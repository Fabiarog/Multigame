using Godot;
using System.Collections.Generic;

namespace GameHub.Core.Networking;

public class PlayerInfo
{
    public long Id { get; set; }
    public string Name { get; set; }
    public bool IsReady { get; set; }
    public int Score { get; set; }
    // More fields like Avatar, Team, etc.
}

/// <summary>
/// Singleton handling the ENet multiplayer connection, Lobby state, and Host/Join logic.
/// </summary>
public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }

    private ENetMultiplayerPeer _multiplayerPeer;
    private const int DEFAULT_PORT = 7070;
    
    public Dictionary<long, PlayerInfo> ConnectedPlayers { get; private set; } = new();
    public PlayerInfo LocalPlayer { get; private set; } = new PlayerInfo();

    [Signal]
    public delegate void PlayerConnectedEventHandler(long peerId);
    
    [Signal]
    public delegate void PlayerDisconnectedEventHandler(long peerId);

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;
            Multiplayer.ServerDisconnected += OnServerDisconnected;
            
            // Set up a default local player name
            LocalPlayer.Name = $"Player_{GD.Randi() % 1000}";
        }
        else
        {
            QueueFree();
        }
    }

    public void HostGame(int maxPlayers)
    {
        _multiplayerPeer = new ENetMultiplayerPeer();
        var error = _multiplayerPeer.CreateServer(DEFAULT_PORT, maxPlayers);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Network] Failed to host: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = _multiplayerPeer;
        LocalPlayer.Id = 1; // Server is always ID 1
        ConnectedPlayers[1] = LocalPlayer;
        
        GD.Print($"[Network] Hosted game on port {DEFAULT_PORT}. I am Host.");
    }

    public void JoinGame(string ipAddress)
    {
        _multiplayerPeer = new ENetMultiplayerPeer();
        var error = _multiplayerPeer.CreateClient(ipAddress, DEFAULT_PORT);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Network] Failed to join: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = _multiplayerPeer;
        GD.Print($"[Network] Joining {ipAddress}...");
    }

    private void OnPeerConnected(long id)
    {
        GD.Print($"[Network] Peer {id} connected.");
        // We will exchange player info via RPCs here
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"[Network] Peer {id} disconnected.");
        if (ConnectedPlayers.ContainsKey(id))
        {
            ConnectedPlayers.Remove(id);
            EmitSignal(SignalName.PlayerDisconnected, id);
        }
    }

    private void OnConnectedToServer()
    {
        LocalPlayer.Id = Multiplayer.GetUniqueId();
        GD.Print($"[Network] Successfully connected to server. My ID is {LocalPlayer.Id}");
        
        // Send our info to the server
        RpcId(1, MethodName.RegisterPlayer, LocalPlayer.Name);
    }
    
    private void OnConnectionFailed() => GD.PrintErr("[Network] Connection failed.");
    private void OnServerDisconnected() => GD.Print("[Network] Server disconnected.");

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterPlayer(string playerName)
    {
        long senderId = Multiplayer.GetRemoteSenderId();
        
        var newPlayer = new PlayerInfo { Id = senderId, Name = playerName };
        ConnectedPlayers[senderId] = newPlayer;
        
        GD.Print($"[Network] Registered Player: {playerName} (ID: {senderId})");
        EmitSignal(SignalName.PlayerConnected, senderId);

        // If I am the server, I need to broadcast the entire player list to the new guy
        if (Multiplayer.IsServer())
        {
            // Send new player to everyone else
            foreach (var p in ConnectedPlayers)
            {
                if (p.Key != senderId && p.Key != 1)
                {
                    RpcId(p.Key, MethodName.AddPlayerToLobby, senderId, playerName);
                }
                
                // Send existing players to the new guy
                RpcId(senderId, MethodName.AddPlayerToLobby, p.Key, p.Value.Name);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void AddPlayerToLobby(long id, string playerName)
    {
        if (!ConnectedPlayers.ContainsKey(id))
        {
            ConnectedPlayers[id] = new PlayerInfo { Id = id, Name = playerName };
            EmitSignal(SignalName.PlayerConnected, id);
        }
    }
}
