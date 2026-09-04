using Godot;
using System.Collections.Generic;

namespace GameHub.Core.Networking;

/// <summary>
/// Represents the full state of a game lobby.
/// The Host owns this and replicates it to all clients via RPCs.
/// </summary>
public partial class LobbyState : Node
{
    public enum TurnMode
    {
        Simultaneous,
        Sequential
    }

    public enum TeamAssignmentMode
    {
        Random,
        HostChooses
    }

    // Match configuration (set by host before starting)
    public string SelectedGameId { get; set; } = "poker_roguelike";
    public string SelectedScenarioId { get; set; } = "casino";
    public int TargetScore { get; set; } = 300;
    public TurnMode SelectedTurnMode { get; set; } = TurnMode.Simultaneous;
    public int MaxPlayers { get; set; } = 6;
    public TeamAssignmentMode TeamAssignment { get; set; } = TeamAssignmentMode.Random;

    // Player slots
    public Dictionary<long, LobbyPlayerSlot> PlayerSlots { get; private set; } = new();

    public bool AreAllPlayersReady()
    {
        foreach (var slot in PlayerSlots.Values)
        {
            if (!slot.IsReady) return false;
        }
        return PlayerSlots.Count >= 2; // At least 2 players needed
    }

    public void AddPlayer(long peerId, string name)
    {
        if (!PlayerSlots.ContainsKey(peerId))
        {
            PlayerSlots[peerId] = new LobbyPlayerSlot
            {
                PeerId = peerId,
                PlayerName = name,
                IsReady = false,
                Team = 0,
                AvatarId = "default"
            };
            GD.Print($"[Lobby] Added player: {name} (ID: {peerId})");
        }
    }

    public void RemovePlayer(long peerId)
    {
        if (PlayerSlots.Remove(peerId))
        {
            GD.Print($"[Lobby] Removed player ID: {peerId}");
        }
    }

    public void SetPlayerReady(long peerId, bool ready)
    {
        if (PlayerSlots.TryGetValue(peerId, out var slot))
        {
            slot.IsReady = ready;
            GD.Print($"[Lobby] Player {slot.PlayerName} ready: {ready}");
        }
    }
}

public class LobbyPlayerSlot
{
    public long PeerId { get; set; }
    public string PlayerName { get; set; } = "";
    public bool IsReady { get; set; } = false;
    public int Team { get; set; } = 0; // 0 = unassigned
    public string AvatarId { get; set; } = "default";
}
