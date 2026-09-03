using Godot;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GameHub.Core.Networking;

/// <summary>
/// Handles UDP broadcast-based LAN discovery.
/// The Host broadcasts a beacon; Clients listen for beacons to populate the server list.
/// </summary>
public partial class LanDiscovery : Node
{
    public static LanDiscovery Instance { get; private set; }

    private const int BROADCAST_PORT = 7071;
    private const float BROADCAST_INTERVAL = 2.0f;
    private const string PROTOCOL_HEADER = "GAMEHUB_LAN";

    private UdpClient _broadcaster;
    private UdpClient _listener;
    private Thread _listenThread;
    private bool _isBroadcasting = false;
    private bool _isListening = false;
    private float _broadcastTimer = 0f;

    // Discovered server info
    public struct ServerInfo
    {
        public string HostName;
        public string GameId;
        public int CurrentPlayers;
        public int MaxPlayers;
        public string IpAddress;
        public double LastSeen; // timestamp
    }

    // Thread-safe collection for discovered servers
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, ServerInfo> _discoveredServers = new();

    [Signal]
    public delegate void ServerDiscoveredEventHandler(string ip, string hostName, string gameId, int currentPlayers, int maxPlayers);

    [Signal]
    public delegate void ServerLostEventHandler(string ip);

    public override void _EnterTree()
    {
        if (Instance == null)
            Instance = this;
        else
            QueueFree();
    }

    public override void _ExitTree()
    {
        StopBroadcasting();
        StopListening();
    }

    // ===== HOST SIDE =====

    /// <summary>
    /// Start broadcasting this host's presence on the LAN.
    /// </summary>
    public void StartBroadcasting(string hostName, string gameId, int currentPlayers, int maxPlayers)
    {
        if (_isBroadcasting) return;

        try
        {
            _broadcaster = new UdpClient();
            _broadcaster.EnableBroadcast = true;
            _isBroadcasting = true;
            _broadcastTimer = 0f;

            // Store the data we will broadcast
            SetMeta("broadcast_host", hostName);
            SetMeta("broadcast_game", gameId);
            SetMeta("broadcast_cur", currentPlayers);
            SetMeta("broadcast_max", maxPlayers);

            GD.Print($"[LAN] Broadcasting started: {hostName} ({gameId})");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[LAN] Failed to start broadcasting: {ex.Message}");
        }
    }

    public void StopBroadcasting()
    {
        if (!_isBroadcasting) return;
        _isBroadcasting = false;
        _broadcaster?.Close();
        _broadcaster = null;
        GD.Print("[LAN] Broadcasting stopped.");
    }

    public override void _Process(double delta)
    {
        if (_isBroadcasting)
        {
            _broadcastTimer += (float)delta;
            if (_broadcastTimer >= BROADCAST_INTERVAL)
            {
                _broadcastTimer = 0f;
                SendBroadcast();
            }
        }

        // Clean up stale servers (not seen in 6 seconds)
        double now = Time.GetUnixTimeFromSystem();
        foreach (var kvp in _discoveredServers)
        {
            if (now - kvp.Value.LastSeen > 6.0)
            {
                _discoveredServers.TryRemove(kvp.Key, out _);
                CallDeferred(MethodName.EmitServerLost, kvp.Key);
            }
        }
    }

    private void SendBroadcast()
    {
        try
        {
            string hostName = (string)GetMeta("broadcast_host");
            string gameId = (string)GetMeta("broadcast_game");
            int cur = (int)GetMeta("broadcast_cur");
            int max = (int)GetMeta("broadcast_max");

            string message = $"{PROTOCOL_HEADER}|{hostName}|{gameId}|{cur}|{max}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT);
            _broadcaster.Send(data, data.Length, endPoint);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[LAN] Broadcast send error: {ex.Message}");
        }
    }

    // ===== CLIENT SIDE =====

    /// <summary>
    /// Start listening for LAN server broadcasts.
    /// </summary>
    public void StartListening()
    {
        if (_isListening) return;

        try
        {
            _listener = new UdpClient(BROADCAST_PORT);
            _listener.EnableBroadcast = true;
            _isListening = true;

            _listenThread = new Thread(ListenLoop) { IsBackground = true };
            _listenThread.Start();
            GD.Print("[LAN] Listening for servers...");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[LAN] Failed to start listening: {ex.Message}");
        }
    }

    public void StopListening()
    {
        if (!_isListening) return;
        _isListening = false;
        _listener?.Close();
        _listener = null;
        _discoveredServers.Clear();
        GD.Print("[LAN] Stopped listening.");
    }

    private void ListenLoop()
    {
        while (_isListening)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _listener.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                if (!message.StartsWith(PROTOCOL_HEADER)) continue;

                string[] parts = message.Split('|');
                if (parts.Length < 5) continue;

                var info = new ServerInfo
                {
                    HostName = parts[1],
                    GameId = parts[2],
                    CurrentPlayers = int.Parse(parts[3]),
                    MaxPlayers = int.Parse(parts[4]),
                    IpAddress = remoteEP.Address.ToString(),
                    LastSeen = Time.GetUnixTimeFromSystem()
                };

                bool isNew = !_discoveredServers.ContainsKey(info.IpAddress);
                _discoveredServers[info.IpAddress] = info;

                if (isNew)
                {
                    CallDeferred(MethodName.EmitServerDiscovered, info.IpAddress, info.HostName, info.GameId, info.CurrentPlayers, info.MaxPlayers);
                }
            }
            catch (SocketException)
            {
                // Expected when socket is closed during StopListening
                break;
            }
            catch (System.Exception ex)
            {
                if (_isListening)
                    GD.PrintErr($"[LAN] Listen error: {ex.Message}");
            }
        }
    }

    // Deferred signal emitters (must run on main thread)
    private void EmitServerDiscovered(string ip, string hostName, string gameId, int curPlayers, int maxPlayers)
    {
        EmitSignal(SignalName.ServerDiscovered, ip, hostName, gameId, curPlayers, maxPlayers);
    }

    private void EmitServerLost(string ip)
    {
        EmitSignal(SignalName.ServerLost, ip);
    }

    /// <summary>
    /// Returns all currently visible servers.
    /// </summary>
    public System.Collections.Generic.Dictionary<string, ServerInfo> GetDiscoveredServers()
    {
        return new System.Collections.Generic.Dictionary<string, ServerInfo>(_discoveredServers);
    }
}
