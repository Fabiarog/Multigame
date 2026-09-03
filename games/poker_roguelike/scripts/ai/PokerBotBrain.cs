using Godot;
using System.Collections.Generic;
using GameHub.Core.AI;

namespace GameHub.Games.PokerRoguelike.AI;

/// <summary>
/// Poker-specific Bot AI. Evaluates the hand, considers risk, and plays cards.
/// This is used both for the Tutorial mode (teaching the player) and for
/// replacing disconnected players mid-match.
/// </summary>
public partial class PokerBotBrain : BotController
{
    // Simplified representation of the bot's current hand
    private List<int> _hand = new();
    private int _currentScore = 0;
    private int _targetScore = 300;

    protected override void ExecuteDecision()
    {
        // --- DECISION LOGIC (Finite State Machine) ---
        // 1. Evaluate hand strength
        // 2. Consider risk vs. reward
        // 3. Select cards to play
        // 4. Submit the action via RPC (as if this bot were a real player)

        float handStrength = EvaluateHand();

        if (handStrength > 0.7f)
        {
            // Strong hand: play aggressively
            PlayBestHand();
        }
        else if (handStrength > 0.3f)
        {
            // Medium hand: play safe
            PlaySafeHand();
        }
        else
        {
            // Weak hand: fold or discard
            PlayWeakStrategy();
        }
    }

    public override void UpdateGameState(Dictionary<string, Variant> stateData)
    {
        // Parse the generic state dictionary into our specific fields
        if (stateData.TryGetValue("hand", out var handVar))
        {
            // Parse hand data
            GD.Print($"[PokerBot:{BotName}] Updated hand state.");
        }
        if (stateData.TryGetValue("score", out var scoreVar))
        {
            _currentScore = scoreVar.AsInt32();
        }
        if (stateData.TryGetValue("target_score", out var targetVar))
        {
            _targetScore = targetVar.AsInt32();
        }
    }

    private float EvaluateHand()
    {
        // Placeholder: returns a random strength value.
        // Real implementation would evaluate poker hand combinations.
        return Difficulty switch
        {
            BotDifficulty.Easy => GD.Randf() * 0.6f,    // Easy bots undervalue their hands
            BotDifficulty.Hard => GD.Randf() * 0.3f + 0.5f, // Hard bots are more accurate
            _ => GD.Randf()
        };
    }

    private void PlayBestHand()
    {
        GD.Print($"[PokerBot:{BotName}] Playing aggressively! (Strong hand)");
        // In a real implementation:
        // 1. Select the best combination of cards
        // 2. Call the game's RPC to submit cards
        // RpcId(1, "SubmitCards", ControlledPeerId, selectedCards);
    }

    private void PlaySafeHand()
    {
        GD.Print($"[PokerBot:{BotName}] Playing conservatively. (Medium hand)");
    }

    private void PlayWeakStrategy()
    {
        GD.Print($"[PokerBot:{BotName}] Folding or discarding. (Weak hand)");
    }
}
