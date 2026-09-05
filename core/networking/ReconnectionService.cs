using Godot;
using System.Collections.Generic;

namespace GameHub.Core.Networking;

/// <summary>
/// Monitors connected peers and handles reconnection logic:
///   • Detects disconnections via the Multiplayer API
///   • Activates AI takeover for disconnected players (via <see cref="DisconnectHandler"/>)
///   • Client-side automatic reconnection with exponential backoff
///   • Peer state tracking with configurable timeout
///
/// Added as a child of <see cref="NetworkSessionManager"/> so it receives _Process().
/// </summary>
public partial class ReconnectionService : Node
{
    // ───────────────────────────── Configuration ────────────────────

    /// <summary>Maximum time (seconds) before a disconnected peer is permanently dropped.</summary>
    public double ReconnectTimeoutSeconds { get; set; } = 120.0;

    /// <summary>Initial delay before the first reconnection attempt (client-side).</summary>
    public float InitialRetryDelaySec { get; set; } = 1.0f;

    /// <summary>Maximum delay between reconnection attempts (caps the exponential growth).</summary>
    public float MaxRetryDelaySec { get; set; } = 30.0f;

    /// <summary>Maximum number of client-side reconnection attempts before giving up.</summary>
    public int MaxRetryAttempts { get; set; } = 10;

    // ───────────────────────────── State ─────────────────────────────

    /// <summary>
    /// Server-side: tracks every peer that has disconnected during a match,
    /// keyed by their original peer ID.
    /// </summary>
    public Dictionary<long, DisconnectHandler> DisconnectedPeers { get; private set; } = new();

    // Client-side reconnection state.
    private bool   _isReconnecting     = false;
    private string _lastServerIp       = "";
    private int    _lastServerPort     = 7070;
    private int    _retryAttempt       = 0;
    private float  _retryTimer         = 0f;
    private float  _currentRetryDelay  = 1.0f;

    // ───────────────────────────── Signals ───────────────────────────

    /// <summary>Emitted when a peer has been flagged as disconnected and AI takes over.</summary>
    [Signal]
    public delegate void PeerDisconnectedAiTakeoverEventHandler(long peerId, string playerName);

    /// <summary>Emitted when a previously disconnected peer successfully reconnects.</summary>
    [Signal]
    public delegate void PeerReconnectedEventHandler(long peerId, string playerName);

    /// <summary>Emitted when a peer's reconnect timeout expires and they are permanently removed.</summary>
    [Signal]
    public delegate void PeerPermanentlyLostEventHandler(long peerId, string playerName);

    /// <summary>Client-side: emitted on each retry attempt.</summary>
    [Signal]
    public delegate void ReconnectionAttemptEventHandler(int attempt, int maxAttempts, float nextDelaySec);

    /// <summary>Client-side: emitted when all retry attempts are exhausted.</summary>
    [Signal]
    public delegate void ReconnectionGaveUpEventHandler();

    /// <summary>Client-side: emitted when reconnection succeeds.</summary>
    [Signal]
    public delegate void ReconnectionSucceededEventHandler();

    // ═════════════════════════════════════════════════════════════════
    //  SERVER-SIDE — Peer tracking
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by <see cref="NetworkSessionManager"/> when a peer connects.
    /// If this peer was previously disconnected, restore their slot.
    /// </summary>
    public void OnPeerConnected(long peerId)
    {
        if (DisconnectedPeers.TryGetValue(peerId, out var handler))
        {
            handler.OnReconnected();
            DisconnectedPeers.Remove(peerId);
            EmitSignal(SignalName.PeerReconnected, peerId, handler.PlayerName);
            GD.Print($"[Reconnect] Peer {peerId} ({handler.PlayerName}) reconnected.");
        }
    }

    /// <summary>
    /// Called by <see cref="NetworkSessionManager"/> when a peer disconnects.
    /// Starts the reconnection timeout and AI takeover window.
    /// </summary>
    public void OnPeerDisconnected(long peerId)
    {
        // Only the server tracks disconnected peers.
        if (!Multiplayer.IsServer()) return;

        // Resolve player name from NetworkManager.
        string name = $"Player_{peerId}";
        if (NetworkManager.Instance?.ConnectedPlayers.TryGetValue(peerId, out var info) == true)
            name = info.Name;

        if (!DisconnectedPeers.ContainsKey(peerId))
        {
            var handler = new DisconnectHandler(peerId, name);
            handler.OnDisconnected();
            DisconnectedPeers[peerId] = handler;
            EmitSignal(SignalName.PeerDisconnectedAiTakeover, peerId, name);
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  CLIENT-SIDE — Automatic reconnection
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by <see cref="NetworkSessionManager"/> when the local client
    /// loses its connection to the server.
    /// </summary>
    public void OnLocalDisconnected()
    {
        // Don't reconnect if we disconnected deliberately.
        if (NetworkSessionManager.Instance?.State == NetworkSessionManager.SessionState.Idle)
            return;

        // Remember last connection target.
        // In LAN mode we don't have a stored IP — skip auto-reconnect.
        if (NetworkSessionManager.Instance?.CurrentMode == NetworkSessionManager.NetworkMode.Lan)
        {
            GD.Print("[Reconnect] LAN disconnection — auto-reconnect not supported in LAN mode.");
            return;
        }

        _isReconnecting    = true;
        _retryAttempt      = 0;
        _retryTimer        = 0f;
        _currentRetryDelay = InitialRetryDelaySec;

        GD.Print($"[Reconnect] Connection lost. Will attempt up to {MaxRetryAttempts} reconnections.");
    }

    /// <summary>
    /// Called when the client has successfully reconnected.
    /// Cancels any pending retry loop.
    /// </summary>
    public void OnLocalReconnected()
    {
        if (!_isReconnecting) return;

        _isReconnecting = false;
        _retryAttempt = 0;
        EmitSignal(SignalName.ReconnectionSucceeded);
        GD.Print("[Reconnect] Successfully reconnected.");
    }

    // ═════════════════════════════════════════════════════════════════
    //  PROCESS
    // ═════════════════════════════════════════════════════════════════

    public override void _Process(double delta)
    {
        ProcessServerTimeouts();
        ProcessClientReconnection((float)delta);
    }

    /// <summary>
    /// Server-side: check if any disconnected peer has exceeded the timeout.
    /// </summary>
    private void ProcessServerTimeouts()
    {
        if (!Multiplayer.IsServer()) return;

        // Collect expired peers (can't modify dictionary while iterating).
        List<long> expired = null;

        foreach (var kvp in DisconnectedPeers)
        {
            if (kvp.Value.HasTimedOut())
            {
                expired ??= new List<long>();
                expired.Add(kvp.Key);
            }
        }

        if (expired == null) return;

        foreach (long id in expired)
        {
            var handler = DisconnectedPeers[id];
            DisconnectedPeers.Remove(id);
            EmitSignal(SignalName.PeerPermanentlyLost, id, handler.PlayerName);
            GD.Print($"[Reconnect] Peer {id} ({handler.PlayerName}) timed out — permanently removed.");
        }
    }

    /// <summary>
    /// Client-side: runs the exponential-backoff retry loop.
    /// </summary>
    private void ProcessClientReconnection(float delta)
    {
        if (!_isReconnecting) return;

        _retryTimer += delta;

        if (_retryTimer < _currentRetryDelay) return;
        _retryTimer = 0f;

        _retryAttempt++;

        if (_retryAttempt > MaxRetryAttempts)
        {
            _isReconnecting = false;
            EmitSignal(SignalName.ReconnectionGaveUp);
            GD.PrintErr($"[Reconnect] Gave up after {MaxRetryAttempts} attempts.");
            return;
        }

        // Exponential backoff: delay doubles each attempt, capped.
        float nextDelay = Mathf.Min(_currentRetryDelay * 2f, MaxRetryDelaySec);

        EmitSignal(SignalName.ReconnectionAttempt, _retryAttempt, MaxRetryAttempts, nextDelay);
        GD.Print($"[Reconnect] Attempt {_retryAttempt}/{MaxRetryAttempts} (next in {nextDelay:F1}s)…");

        // Attempt reconnection via the session manager.
        var session = NetworkSessionManager.Instance;
        if (session?.OnlineManager != null && !string.IsNullOrEmpty(_lastServerIp))
        {
            session.OnlineManager.ConnectDirect(_lastServerIp, _lastServerPort);
        }

        _currentRetryDelay = nextDelay;
    }

    // ═════════════════════════════════════════════════════════════════
    //  UTILITY
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Stores the server address so client-side reconnection knows where to connect.
    /// Should be called right after a successful connection.
    /// </summary>
    public void RememberServer(string ip, int port)
    {
        _lastServerIp   = ip;
        _lastServerPort = port;
    }

    /// <summary>
    /// Returns whether the given peer is currently controlled by AI due to disconnection.
    /// </summary>
    public bool IsPeerAiControlled(long peerId)
    {
        return DisconnectedPeers.TryGetValue(peerId, out var handler) && handler.IsAiControlled;
    }

    /// <summary>
    /// Resets all state (used when leaving a session entirely).
    /// </summary>
    public void Reset()
    {
        _isReconnecting = false;
        _retryAttempt = 0;
        _retryTimer = 0f;
        _currentRetryDelay = InitialRetryDelaySec;
        DisconnectedPeers.Clear();
        _lastServerIp = "";
    }
}
