using Godot;
using System.Linq;
using GameHub.Core.Networking;
using GameHub.Core.Systems;

namespace GameHub.Core.Networking;

/// <summary>
/// Manages the lobby lifecycle: hosting, joining, player readying, and starting a match.
/// Uses the NetworkManager for raw connection and LanDiscovery for server discovery.
/// </summary>
public partial class LobbyManager : Node
{
    public static LobbyManager Instance { get; private set; }

    public LobbyState CurrentLobby { get; private set; }
    public bool IsHost => Multiplayer.IsServer();

    [Signal]
    public delegate void LobbyUpdatedEventHandler();

    [Signal]
    public delegate void MatchStartingEventHandler(string gameScenePath);

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            CurrentLobby = new LobbyState();
        }
        else
        {
            QueueFree();
        }
    }

    public override void _Ready()
    {
        // Listen to NetworkManager signals
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
    }

    // ===== HOST =====

    /// <summary>
    /// Host a new lobby and start broadcasting on LAN.
    /// </summary>
    public void HostLobby(string gameId, int maxPlayers, LobbyState.TurnMode turnMode, int targetScore, string scenarioId)
    {
        CurrentLobby = new LobbyState
        {
            SelectedGameId = gameId,
            MaxPlayers = maxPlayers,
            SelectedTurnMode = turnMode,
            TargetScore = targetScore,
            SelectedScenarioId = scenarioId
        };

        NetworkManager.Instance.HostGame(maxPlayers);
        string nickname = SettingsManager.Instance?.PlayerNickname ?? "Host";

        CurrentLobby.AddPlayer(1, nickname);

        if (gameId == "truco")
            AssignTeamsRandomly();

        LanDiscovery.Instance?.StartBroadcasting(nickname, gameId, 1, maxPlayers);

        EmitSignal(SignalName.LobbyUpdated);
        GD.Print($"[LobbyManager] Hosted lobby: {gameId}, TurnMode: {turnMode}, Score: {targetScore}, Scenario: {scenarioId}");
    }

    // ===== CLIENT =====

    /// <summary>
    /// Join a discovered lobby by IP.
    /// </summary>
    public void JoinLobby(string ipAddress)
    {
        NetworkManager.Instance.JoinGame(ipAddress);
        GD.Print($"[LobbyManager] Attempting to join {ipAddress}...");
    }

    // ===== READY =====

    public void ToggleReady()
    {
        long myId = Multiplayer.GetUniqueId();
        if (CurrentLobby.PlayerSlots.TryGetValue(myId, out var slot))
        {
            bool newReady = !slot.IsReady;
            RpcId(1, MethodName.ServerSetPlayerReady, myId, newReady);
        }
    }

    public void SetTeamAssignment(LobbyState.TeamAssignmentMode mode)
    {
        if (!IsHost) return;
        CurrentLobby.TeamAssignment = mode;
        if (mode == LobbyState.TeamAssignmentMode.Random)
            AssignTeamsRandomly();
        EmitSignal(SignalName.LobbyUpdated);
    }

    public void SetPlayerTeam(long playerId, int team)
    {
        if (!IsHost || team < 1 || team > 2) return;
        if (CurrentLobby.PlayerSlots.TryGetValue(playerId, out var slot))
        {
            slot.Team = team;
            Rpc(MethodName.ClientUpdatePlayerTeam, playerId, team);
            EmitSignal(SignalName.LobbyUpdated);
        }
    }

    private void AssignTeamsRandomly()
    {
        int team = 1;
        foreach (var slot in CurrentLobby.PlayerSlots.Values.OrderBy(_ => GD.Randf()))
        {
            slot.Team = team;
            Rpc(MethodName.ClientUpdatePlayerTeam, slot.PeerId, team);
            team = team == 1 ? 2 : 1;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientUpdatePlayerTeam(long peerId, int team)
    {
        if (CurrentLobby.PlayerSlots.TryGetValue(peerId, out var slot))
            slot.Team = team;
        EmitSignal(SignalName.LobbyUpdated);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ServerSetPlayerReady(long peerId, bool ready)
    {
        if (!IsHost) return;

        CurrentLobby.SetPlayerReady(peerId, ready);
        // Broadcast updated state to all
        Rpc(MethodName.ClientUpdatePlayerReady, peerId, ready);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientUpdatePlayerReady(long peerId, bool ready)
    {
        CurrentLobby.SetPlayerReady(peerId, ready);
        EmitSignal(SignalName.LobbyUpdated);
    }

    // ===== START MATCH =====

    /// <summary>
    /// Host-only: Start the match if all players are ready.
    /// </summary>
    public void TryStartMatch()
    {
        if (!IsHost) return;

        if (!CurrentLobby.AreAllPlayersReady())
        {
            GD.Print("[LobbyManager] Cannot start: not all players are ready.");
            return;
        }

        string gameId = CurrentLobby.SelectedGameId;
        GameHub.Core.Registry.GameDefinition gameDef = null;
        if (Registry.GameRegistry.Instance != null && Registry.GameRegistry.Instance.AvailableGames.ContainsKey(gameId))
        {
            gameDef = Registry.GameRegistry.Instance.AvailableGames[gameId];
        }

        if (gameDef == null)
        {
            GD.PrintErr($"[LobbyManager] Game '{gameId}' not found in registry!");
            return;
        }

        // Stop broadcasting now that the match is starting
        LanDiscovery.Instance?.StopBroadcasting();

        // Generate match seed for synchronized RNG
        uint matchSeed = GD.Randi();

        // Tell all clients to load the game scene
        Rpc(MethodName.ClientStartMatch, gameDef.MainScenePath, matchSeed, (int)CurrentLobby.SelectedTurnMode, CurrentLobby.TargetScore, CurrentLobby.SelectedScenarioId);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientStartMatch(string scenePath, uint matchSeed, int turnMode, int targetScore, string scenarioId)
    {
        GD.Print($"[LobbyManager] Starting match! Scene: {scenePath}, Seed: {matchSeed}, Turn: {(LobbyState.TurnMode)turnMode}, Scenario: {scenarioId}");

        // Store the seed for the match's SyncRng system
        SetMeta("match_seed", matchSeed);
        SetMeta("match_turn_mode", turnMode);
        SetMeta("match_target_score", targetScore);
        SetMeta("match_scenario", scenarioId);

        EmitSignal(SignalName.MatchStarting, scenePath);
        // The HubMain or a SceneManager would handle the actual scene change:
        // GetTree().ChangeSceneToFile(scenePath);
    }

    // ===== CONNECTION EVENTS =====

    private void OnPeerConnected(long id)
    {
        if (!IsHost) return;

        // A new peer connected. We send them the full lobby state.
        // For now, assign a temporary name until they register.
        CurrentLobby.AddPlayer(id, $"Player_{id}");

        // Update our broadcast player count
        LanDiscovery.Instance?.StopBroadcasting();
        string nickname = SettingsManager.Instance?.PlayerNickname ?? "Host";
        LanDiscovery.Instance?.StartBroadcasting(nickname, CurrentLobby.SelectedGameId, CurrentLobby.PlayerSlots.Count, CurrentLobby.MaxPlayers);

        // Sync full lobby state to the new player
        foreach (var kvp in CurrentLobby.PlayerSlots)
        {
            RpcId(id, MethodName.ClientSyncPlayer, kvp.Key, kvp.Value.PlayerName, kvp.Value.IsReady, kvp.Value.Team);
        }
        // Also tell everyone about the new player
        Rpc(MethodName.ClientSyncPlayer, id, $"Player_{id}", false, 0);

        EmitSignal(SignalName.LobbyUpdated);
    }

    private void OnPeerDisconnected(long id)
    {
        CurrentLobby.RemovePlayer(id);
        Rpc(MethodName.ClientRemovePlayer, id);
        EmitSignal(SignalName.LobbyUpdated);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientSyncPlayer(long peerId, string name, bool ready, int team)
    {
        if (!CurrentLobby.PlayerSlots.ContainsKey(peerId))
        {
            CurrentLobby.AddPlayer(peerId, name);
        }
        CurrentLobby.SetPlayerReady(peerId, ready);
        EmitSignal(SignalName.LobbyUpdated);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientRemovePlayer(long peerId)
    {
        CurrentLobby.RemovePlayer(peerId);
        EmitSignal(SignalName.LobbyUpdated);
    }

    // ===== CLEANUP =====

    public void LeaveLobby()
    {
        LanDiscovery.Instance?.StopBroadcasting();
        LanDiscovery.Instance?.StopListening();
        Multiplayer.MultiplayerPeer?.Close();
        Multiplayer.MultiplayerPeer = null;
        CurrentLobby = new LobbyState();
        EmitSignal(SignalName.LobbyUpdated);
        GD.Print("[LobbyManager] Left lobby.");
    }
}
