using Godot;

namespace GameHub.Core.Networking;

/// <summary>
/// Manages online (WAN) multiplayer connections:
///   • Direct IP/port via ENet
///   • NAT traversal helpers (UPnP + STUN)
///   • Stub interface for Relay / Matchmaking integration
///   • Periodic heartbeat to detect connection loss
///
/// Created lazily by <see cref="NetworkSessionManager"/> — not an autoload.
/// </summary>
public partial class OnlineMultiplayerManager : Node
{
    // ───────────────────────────── Defaults ──────────────────────────

    private const int DEFAULT_ONLINE_PORT = 7070;
    private const float HEARTBEAT_INTERVAL = 5.0f;
    private const float HEARTBEAT_TIMEOUT  = 15.0f;

    // ───────────────────────────── State ─────────────────────────────

    private ENetMultiplayerPeer _peer;
    private float _heartbeatTimer = 0f;
    private float _lastPongTime   = 0f;
    private bool  _isActive       = false;

    /// <summary>
    /// Public IP discovered via STUN / UPnP.  Empty if not yet resolved.
    /// </summary>
    public string PublicIp { get; private set; } = "";

    /// <summary>Port currently bound for online play.</summary>
    public int BoundPort { get; private set; } = DEFAULT_ONLINE_PORT;

    // ───────────────────────────── Signals ───────────────────────────

    [Signal]
    public delegate void OnlineConnectionEstablishedEventHandler();

    [Signal]
    public delegate void OnlineConnectionFailedEventHandler(string reason);

    [Signal]
    public delegate void RelayMatchFoundEventHandler(string matchId);

    [Signal]
    public delegate void NatTraversalResultEventHandler(bool success, string publicIp, int publicPort);

    [Signal]
    public delegate void HeartbeatTimeoutEventHandler();

    // ═════════════════════════════════════════════════════════════════
    //  DIRECT CONNECTION
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hosts a game listening on the given port.
    /// Optionally attempts UPnP port mapping so external clients can connect.
    /// </summary>
    public void HostDirect(int port, int maxPlayers)
    {
        _peer = new ENetMultiplayerPeer();

        // ENet tuning for WAN.
        var error = _peer.CreateServer(port, maxPlayers);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Online] Failed to host on port {port}: {error}");
            EmitSignal(SignalName.OnlineConnectionFailed, $"Erro ao hospedar: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        BoundPort = port;
        _isActive = true;
        _lastPongTime = (float)Time.GetTicksMsec() / 1000f;

        GD.Print($"[Online] Hosting on port {port} (max {maxPlayers}).");

        // Fire-and-forget UPnP mapping.
        TryUpnpPortMapping(port);
    }

    /// <summary>
    /// Connects to a remote host by direct IP and port.
    /// </summary>
    public void ConnectDirect(string ip, int port)
    {
        _peer = new ENetMultiplayerPeer();

        var error = _peer.CreateClient(ip, port);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Online] Failed to connect to {ip}:{port}: {error}");
            EmitSignal(SignalName.OnlineConnectionFailed, $"Erro ao conectar: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;
        BoundPort = port;
        _isActive = true;
        _lastPongTime = (float)Time.GetTicksMsec() / 1000f;

        GD.Print($"[Online] Connecting to {ip}:{port}…");
    }

    // ═════════════════════════════════════════════════════════════════
    //  NAT TRAVERSAL
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempts to open a UPnP port mapping on the local router
    /// so that external peers can reach this host.
    /// </summary>
    private void TryUpnpPortMapping(int port)
    {
        var upnp = new Upnp();
        int discoverResult = upnp.Discover();

        if (discoverResult != (int)Upnp.UpnpResult.Success)
        {
            GD.Print($"[Online] UPnP discovery failed (code {discoverResult}). NAT traversal unavailable.");
            EmitSignal(SignalName.NatTraversalResult, false, "", 0);
            return;
        }

        // Try to get the external IP.
        string externalIp = upnp.QueryExternalAddress();

        if (string.IsNullOrEmpty(externalIp))
        {
            GD.Print("[Online] UPnP: could not determine external IP.");
            EmitSignal(SignalName.NatTraversalResult, false, "", 0);
            return;
        }

        // Add port mapping (UDP for ENet).
        int mapResult = upnp.AddPortMapping(port, port, "MultiGame", "UDP");

        if (mapResult != (int)Upnp.UpnpResult.Success)
        {
            GD.Print($"[Online] UPnP port mapping failed (code {mapResult}).");
            EmitSignal(SignalName.NatTraversalResult, false, externalIp, port);
            return;
        }

        PublicIp = externalIp;
        GD.Print($"[Online] UPnP mapped {externalIp}:{port} → local:{port}.");
        EmitSignal(SignalName.NatTraversalResult, true, externalIp, port);
    }

    // ═════════════════════════════════════════════════════════════════
    //  RELAY / MATCHMAKING (INTERFACE STUB)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes the relay SDK (e.g. Epic Online Services, Steam, etc.).
    /// Override or extend this method when integrating a specific service.
    ///
    /// TODO: Integrate with a concrete relay SDK.
    /// </summary>
    public virtual void InitializeRelay()
    {
        GD.Print("[Online] Relay initialization stub — no SDK integrated yet.");
    }

    /// <summary>
    /// Authenticates with the relay backend.
    ///
    /// TODO: Implement OAuth / platform-specific auth flow.
    /// </summary>
    public virtual void AuthenticateRelay()
    {
        GD.Print("[Online] Relay authentication stub — no SDK integrated yet.");
    }

    /// <summary>
    /// Requests a match from the matchmaking service.
    ///
    /// TODO: Send matchmaking request to the relay backend.
    /// </summary>
    public virtual void FindMatch()
    {
        GD.Print("[Online] FindMatch stub — no SDK integrated yet.");
        // When a real implementation matches, emit:
        // EmitSignal(SignalName.RelayMatchFound, matchId);
    }

    // ═════════════════════════════════════════════════════════════════
    //  HEARTBEAT
    // ═════════════════════════════════════════════════════════════════

    public override void _Process(double delta)
    {
        if (!_isActive) return;

        _heartbeatTimer += (float)delta;

        if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
        {
            _heartbeatTimer = 0f;
            SendHeartbeat();
        }

        // Check for timeout.
        float now = (float)Time.GetTicksMsec() / 1000f;
        if (now - _lastPongTime > HEARTBEAT_TIMEOUT)
        {
            GD.PrintErr("[Online] Heartbeat timeout — connection may be lost.");
            EmitSignal(SignalName.HeartbeatTimeout);
            _lastPongTime = now; // Reset to avoid spamming.
        }
    }

    /// <summary>
    /// Sends a lightweight ping to all connected peers.
    /// </summary>
    private void SendHeartbeat()
    {
        if (Multiplayer.MultiplayerPeer == null) return;
        if (Multiplayer.MultiplayerPeer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected)
            return;

        Rpc(MethodName.OnHeartbeatReceived);
    }

    /// <summary>
    /// Receives a heartbeat from a peer and updates the last-seen timestamp.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void OnHeartbeatReceived()
    {
        _lastPongTime = (float)Time.GetTicksMsec() / 1000f;
    }

    // ═════════════════════════════════════════════════════════════════
    //  CLEANUP
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Shuts down the online manager and releases all resources.
    /// </summary>
    public void Shutdown()
    {
        _isActive = false;
        _peer = null;
        PublicIp = "";
        GD.Print("[Online] Shut down.");
    }
}
