using Godot;
using System.Collections.Generic;

namespace GameHub.Core.Networking;

/// <summary>
/// High-level session orchestrator that abstracts the underlying
/// transport (LAN vs. Online) from the gameplay layer.
///
/// The gameplay code talks only to this class; it never touches
/// <see cref="NetworkManager"/>, <see cref="LanDiscovery"/>, or
/// <see cref="OnlineMultiplayerManager"/> directly.
///
/// Registered as an autoload singleton.
/// </summary>
public partial class NetworkSessionManager : Node
{
    public static NetworkSessionManager Instance { get; private set; }

    // ───────────────────────────── Enums ─────────────────────────────

    /// <summary>
    /// Available network modes.
    /// </summary>
    public enum NetworkMode
    {
        /// <summary>No networking — purely local / solo play.</summary>
        Offline,
        /// <summary>LAN via UDP broadcast discovery + ENet.</summary>
        Lan,
        /// <summary>Online via direct IP/port connection.</summary>
        OnlineDirect,
        /// <summary>Online via a Relay / Matchmaking service.</summary>
        OnlineRelay,
    }

    /// <summary>
    /// Current phase of the session lifecycle.
    /// </summary>
    public enum SessionState
    {
        Idle,
        Hosting,
        Searching,
        Connecting,
        Connected,
        InGame,
        Disconnected,
    }

    // ───────────────────────────── State ─────────────────────────────

    /// <summary>The mode chosen for the current (or last) session.</summary>
    public NetworkMode CurrentMode { get; private set; } = NetworkMode.Offline;

    /// <summary>Lifecycle phase of the current session.</summary>
    public SessionState State { get; private set; } = SessionState.Idle;

    /// <summary>
    /// Online sub-manager.  Created lazily the first time an Online
    /// session is requested.
    /// </summary>
    public OnlineMultiplayerManager OnlineManager { get; private set; }

    /// <summary>
    /// Reconnection service shared across all modes.
    /// </summary>
    public ReconnectionService Reconnection { get; private set; }

    // ───────────────────────────── Signals ───────────────────────────

    [Signal]
    public delegate void SessionCreatedEventHandler(string mode);

    [Signal]
    public delegate void SessionJoinedEventHandler(string ip);

    [Signal]
    public delegate void SessionErrorEventHandler(string errorMessage);

    [Signal]
    public delegate void SessionStateChangedEventHandler(int newState);

    [Signal]
    public delegate void PlayerSyncUpdatedEventHandler(long peerId, string data);

    // ───────────────────────────── Lifecycle ─────────────────────────

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            QueueFree();
        }
    }

    public override void _Ready()
    {
        // Create the reconnection service as a child so it gets _Process.
        Reconnection = new ReconnectionService();
        Reconnection.Name = "ReconnectionService";
        AddChild(Reconnection);

        // Wire into core network events.
        Multiplayer.ConnectedToServer  += OnConnectedToServer;
        Multiplayer.ConnectionFailed   += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
        Multiplayer.PeerConnected      += OnPeerConnected;
        Multiplayer.PeerDisconnected   += OnPeerDisconnected;
    }

    // ═════════════════════════════════════════════════════════════════
    //  LAN API
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Host a new LAN game.  Uses the existing <see cref="NetworkManager"/>
    /// and <see cref="LanDiscovery"/> autoloads.
    /// </summary>
    public void HostLanGame(string hostName, string gameId, int maxPlayers)
    {
        if (State != SessionState.Idle && State != SessionState.Disconnected)
        {
            GD.PrintErr("[Session] Cannot host — session already active.");
            EmitSignal(SignalName.SessionError, "Sessão já ativa.");
            return;
        }

        CurrentMode = NetworkMode.Lan;
        SetState(SessionState.Hosting);

        NetworkManager.Instance.HostGame(maxPlayers);
        LanDiscovery.Instance?.StartBroadcasting(hostName, gameId, 1, maxPlayers);

        EmitSignal(SignalName.SessionCreated, "LAN");
        GD.Print($"[Session] LAN game hosted: {hostName} ({gameId}), max {maxPlayers} players.");
    }

    /// <summary>
    /// Start searching for LAN games on the local network.
    /// Listen to <see cref="LanDiscovery.ServerDiscovered"/> for results.
    /// </summary>
    public void SearchLanGames()
    {
        if (State == SessionState.Hosting || State == SessionState.InGame)
        {
            GD.PrintErr("[Session] Cannot search while hosting or in-game.");
            return;
        }

        CurrentMode = NetworkMode.Lan;
        SetState(SessionState.Searching);
        LanDiscovery.Instance?.StartListening();
        GD.Print("[Session] Searching for LAN games…");
    }

    /// <summary>
    /// Stop an active LAN search.
    /// </summary>
    public void StopSearchingLan()
    {
        LanDiscovery.Instance?.StopListening();
        if (State == SessionState.Searching)
            SetState(SessionState.Idle);
    }

    /// <summary>
    /// Connect to a discovered LAN game by IP address.
    /// </summary>
    public void ConnectToLanGame(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            EmitSignal(SignalName.SessionError, "Endereço IP inválido.");
            return;
        }

        CurrentMode = NetworkMode.Lan;
        SetState(SessionState.Connecting);
        LanDiscovery.Instance?.StopListening();
        NetworkManager.Instance.JoinGame(ipAddress);
        GD.Print($"[Session] Connecting to LAN game at {ipAddress}…");
    }

    /// <summary>
    /// Returns a snapshot of currently discovered LAN servers.
    /// </summary>
    public Dictionary<string, LanDiscovery.ServerInfo> GetDiscoveredLanServers()
    {
        return LanDiscovery.Instance?.GetDiscoveredServers()
            ?? new Dictionary<string, LanDiscovery.ServerInfo>();
    }

    // ═════════════════════════════════════════════════════════════════
    //  ONLINE API
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Host an online game (direct IP mode).
    /// Clients will connect via <see cref="JoinOnlineGame"/>.
    /// </summary>
    public void HostOnlineGame(int port, int maxPlayers)
    {
        if (State != SessionState.Idle && State != SessionState.Disconnected)
        {
            EmitSignal(SignalName.SessionError, "Sessão já ativa.");
            return;
        }

        EnsureOnlineManager();
        CurrentMode = NetworkMode.OnlineDirect;
        SetState(SessionState.Hosting);

        OnlineManager.HostDirect(port, maxPlayers);

        EmitSignal(SignalName.SessionCreated, "Online-Direct");
        GD.Print($"[Session] Online game hosted on port {port}.");
    }

    /// <summary>
    /// Join an online game by direct IP and port.
    /// </summary>
    public void JoinOnlineGame(string ip, int port)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            EmitSignal(SignalName.SessionError, "Endereço IP inválido.");
            return;
        }

        EnsureOnlineManager();
        CurrentMode = NetworkMode.OnlineDirect;
        SetState(SessionState.Connecting);

        OnlineManager.ConnectDirect(ip, port);
        GD.Print($"[Session] Joining online game at {ip}:{port}…");
    }

    /// <summary>
    /// Request matchmaking through a relay service.
    /// Requires <see cref="OnlineMultiplayerManager.InitializeRelay"/>
    /// to have been called first.
    /// </summary>
    public void FindOnlineMatch()
    {
        EnsureOnlineManager();
        CurrentMode = NetworkMode.OnlineRelay;
        SetState(SessionState.Searching);

        OnlineManager.FindMatch();
        GD.Print("[Session] Requesting relay matchmaking…");
    }

    // ═════════════════════════════════════════════════════════════════
    //  SHARED
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cleanly tears down the current session regardless of mode.
    /// </summary>
    public void Disconnect()
    {
        LanDiscovery.Instance?.StopBroadcasting();
        LanDiscovery.Instance?.StopListening();
        OnlineManager?.Shutdown();
        Reconnection?.Reset();

        Multiplayer.MultiplayerPeer?.Close();
        Multiplayer.MultiplayerPeer = null;

        SetState(SessionState.Idle);
        CurrentMode = NetworkMode.Offline;
        GD.Print("[Session] Disconnected.");
    }

    // ───────────────────────────── Internals ─────────────────────────

    private void EnsureOnlineManager()
    {
        if (OnlineManager != null) return;

        OnlineManager = new OnlineMultiplayerManager();
        OnlineManager.Name = "OnlineMultiplayerManager";
        AddChild(OnlineManager);

        // Forward online signals.
        OnlineManager.OnlineConnectionEstablished += () =>
        {
            SetState(SessionState.Connected);
            EmitSignal(SignalName.SessionJoined, "online");
        };
        OnlineManager.OnlineConnectionFailed += (reason) =>
        {
            SetState(SessionState.Disconnected);
            EmitSignal(SignalName.SessionError, reason);
        };
    }

    private void SetState(SessionState newState)
    {
        if (State == newState) return;
        State = newState;
        EmitSignal(SignalName.SessionStateChanged, (int)newState);
        GD.Print($"[Session] State → {newState}");
    }

    // ───────────────────────────── Callbacks ─────────────────────────

    private void OnConnectedToServer()
    {
        SetState(SessionState.Connected);
        string ip = CurrentMode == NetworkMode.Lan ? "LAN" : "Online";
        EmitSignal(SignalName.SessionJoined, ip);
    }

    private void OnConnectionFailed()
    {
        SetState(SessionState.Disconnected);
        EmitSignal(SignalName.SessionError, "Falha na conexão.");
    }

    private void OnServerDisconnected()
    {
        GD.Print("[Session] Server disconnected — attempting reconnection…");
        SetState(SessionState.Disconnected);

        // The ReconnectionService will handle automated retry if enabled.
        Reconnection?.OnLocalDisconnected();
    }

    private void OnPeerConnected(long peerId)
    {
        Reconnection?.OnPeerConnected(peerId);
        EmitSignal(SignalName.PlayerSyncUpdated, peerId, "connected");
    }

    private void OnPeerDisconnected(long peerId)
    {
        Reconnection?.OnPeerDisconnected(peerId);
        EmitSignal(SignalName.PlayerSyncUpdated, peerId, "disconnected");
    }
}
