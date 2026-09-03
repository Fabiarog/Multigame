using Godot;
using System.Collections.Generic;

namespace GameHub.Core.AI;

/// <summary>
/// Base class for all Bot AI controllers.
/// Each game (Poker, Truco) implements its own BotBrain.
/// The BotController handles the lifecycle (activation, turn timing, decision-making).
/// </summary>
public abstract partial class BotController : Node
{
    public enum BotDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    [Export] public string BotName { get; set; } = "Bot";
    [Export] public BotDifficulty Difficulty { get; set; } = BotDifficulty.Medium;

    /// <summary>
    /// The peer ID this bot is impersonating (either a disconnected player or a tutorial slot).
    /// </summary>
    public long ControlledPeerId { get; set; } = -1;

    /// <summary>
    /// Whether this bot is currently active and making decisions.
    /// </summary>
    public bool IsActive { get; set; } = false;

    // Simulated "thinking" time to make the bot feel human
    private float _thinkTimer = 0f;
    private float _thinkDuration = 0f;
    private bool _isThinking = false;

    public override void _Process(double delta)
    {
        if (!IsActive || !_isThinking) return;

        _thinkTimer += (float)delta;
        if (_thinkTimer >= _thinkDuration)
        {
            _isThinking = false;
            ExecuteDecision();
        }
    }

    /// <summary>
    /// Called by the game when it's this bot's turn (or when a simultaneous-turn phase begins).
    /// </summary>
    public void RequestDecision()
    {
        if (!IsActive) return;

        // Simulate thinking time based on difficulty
        _thinkDuration = Difficulty switch
        {
            BotDifficulty.Easy => (float)GD.RandRange(1.5, 3.0),
            BotDifficulty.Medium => (float)GD.RandRange(0.8, 2.0),
            BotDifficulty.Hard => (float)GD.RandRange(0.3, 1.0),
            _ => 1.0f
        };

        _thinkTimer = 0f;
        _isThinking = true;

        GD.Print($"[Bot:{BotName}] Thinking for {_thinkDuration:F1}s...");
    }

    /// <summary>
    /// Subclasses implement this to evaluate the game state and make a move.
    /// </summary>
    protected abstract void ExecuteDecision();

    /// <summary>
    /// Called to provide the bot with the current game state snapshot.
    /// Subclasses parse this into their specific format.
    /// </summary>
    public abstract void UpdateGameState(Dictionary<string, Variant> stateData);
}
