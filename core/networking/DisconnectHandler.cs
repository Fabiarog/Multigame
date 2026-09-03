using Godot;

namespace GameHub.Core.Networking;

/// <summary>
/// Tracks a disconnected player's state and manages AI takeover / reconnection.
/// When a player disconnects mid-match, we:
/// 1. Mark them as disconnected
/// 2. Hand control to a BotController
/// 3. Wait for them to reconnect
/// 4. If they reconnect, hand control back seamlessly
/// </summary>
public class DisconnectHandler
{
    public enum PlayerConnectionState
    {
        Connected,
        Disconnected_AiTakeover,
        Reconnecting
    }

    public long PeerId { get; }
    public string PlayerName { get; }
    public PlayerConnectionState State { get; private set; }
    public double DisconnectedAt { get; private set; }

    // Timeout after which a disconnected player is permanently removed
    private const double RECONNECT_TIMEOUT_SECONDS = 120.0;

    public DisconnectHandler(long peerId, string playerName)
    {
        PeerId = peerId;
        PlayerName = playerName;
        State = PlayerConnectionState.Connected;
    }

    /// <summary>
    /// Called by the host when this player's peer disconnects.
    /// </summary>
    public void OnDisconnected()
    {
        State = PlayerConnectionState.Disconnected_AiTakeover;
        DisconnectedAt = Time.GetUnixTimeFromSystem();
        GD.Print($"[Disconnect] Player '{PlayerName}' (ID: {PeerId}) disconnected. AI taking over.");
    }

    /// <summary>
    /// Called when a peer reconnects and claims this slot.
    /// </summary>
    public void OnReconnected()
    {
        State = PlayerConnectionState.Connected;
        GD.Print($"[Disconnect] Player '{PlayerName}' (ID: {PeerId}) has reconnected!");
    }

    /// <summary>
    /// Returns true if the reconnect timeout has expired and this player should be permanently removed.
    /// </summary>
    public bool HasTimedOut()
    {
        if (State != PlayerConnectionState.Disconnected_AiTakeover) return false;
        double elapsed = Time.GetUnixTimeFromSystem() - DisconnectedAt;
        return elapsed >= RECONNECT_TIMEOUT_SECONDS;
    }

    /// <summary>
    /// Whether the AI should be making decisions for this player.
    /// </summary>
    public bool IsAiControlled => State == PlayerConnectionState.Disconnected_AiTakeover;
}
