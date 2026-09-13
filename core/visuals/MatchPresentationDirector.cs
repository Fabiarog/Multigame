using Godot;
using System;
using System.Threading.Tasks;
using GameHub.Core.Systems;

namespace GameHub.Core.Visuals;

/// <summary>
/// Importance level for table events.
/// Scales camera, lighting, audio, VFX and dramatic pauses without altering game rules or stalling turns.
/// </summary>
public enum PresentationLevel
{
    Level0_Normal = 0,
    Level1_GoodPlay = 1,
    Level2_Manilha = 2,
    Level3_TrucoCall = 3,
    Level4_MatchPoint = 4
}

/// <summary>
/// Dynamic match director that observes game state and orchestrates cinematic emphasis,
/// procedural attention focus, camera framing and lighting shifts while strictly respecting
/// game pace, cooldowns, and accessibility settings (ReduceMotion).
/// </summary>
public partial class MatchPresentationDirector : Node
{
    public static MatchPresentationDirector Instance { get; private set; }

    private TableStage _activeStage;
    private float _cooldownTimer = 0f;
    private const float MinDramaticInterval = 2.4f;

    // Director states
    private bool _isMatchPoint = false;
    private PresentationLevel _currentLevel = PresentationLevel.Level0_Normal;

    public PresentationLevel CurrentLevel => _currentLevel;
    public bool IsMatchPoint => _isMatchPoint;

    public override void _EnterTree()
    {
        if (Instance == null) Instance = this;
    }

    public override void _Ready()
    {
        if (Instance == null) Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Process(double delta)
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= (float)delta;
    }

    public void RegisterStage(TableStage stage)
    {
        _activeStage = stage;
        _isMatchPoint = false;
        _currentLevel = PresentationLevel.Level0_Normal;
        _cooldownTimer = 0f;
    }

    public void UnregisterStage(TableStage stage)
    {
        if (_activeStage == stage)
        {
            _activeStage = null;
            _isMatchPoint = false;
            _currentLevel = PresentationLevel.Level0_Normal;
        }
    }

    /// <summary>
    /// Level 0 & Level 2: Evaluates a card played on the table.
    /// Manilhas trigger Level 2 dramatic accent (camera pull, audio stinger, attention snap).
    /// </summary>
    public void NotifyCardPlayed(int seat, string cardDisplay, bool isSpecial, Vector3 cardWorldPos)
    {
        if (_activeStage == null || !IsInstanceValid(_activeStage)) return;

        bool motion = SettingsManager.Instance?.ReduceMotion != true;

        if (isSpecial)
        {
            _currentLevel = PresentationLevel.Level2_Manilha;
            // Level 2: Manilha played!
            // All seated participants immediately watch the winning/manilha card
            _activeStage.SetAllAttentionToCard(cardWorldPos);

            if (_cooldownTimer <= 0f && motion)
            {
                _cooldownTimer = MinDramaticInterval;
                // Subtle camera FOV contraction (+1.4°) and brief amber accent
                _activeStage.ApplyDramaticFovPulse(-1.4f, 0.45f);
                _activeStage.ApplyPendantHighlight(1.22f, 0.5f);
            }
        }
        else
        {
            _currentLevel = PresentationLevel.Level0_Normal;
            // Level 0: Opponents naturally track the played card in flight
            _activeStage.SetAllAttentionToCard(cardWorldPos);
        }
    }

    /// <summary>
    /// Level 1: Trick won. Micro-pause and gesture coordination.
    /// </summary>
    public void NotifyTrickResolved(int winnerSeat, int loserSeat, bool isTight)
    {
        if (_activeStage == null || !IsInstanceValid(_activeStage)) return;

        _currentLevel = PresentationLevel.Level1_GoodPlay;

        // Seated actors look towards the trick winner
        _activeStage.SetAllAttentionToSeat(winnerSeat);
        _activeStage.SetSeatAttention(winnerSeat, AttentionState.Celebrating, Vector3.Zero);

        if (loserSeat >= 0)
        {
            _activeStage.SetSeatAttention(loserSeat, AttentionState.Defeated, Vector3.Zero);
        }
    }

    /// <summary>
    /// Level 3: Truco / Seis / Nove / Doze called! Critical challenge moment.
    /// Challenges command immediate table focus, camera accent on challenger, and music ducking.
    /// </summary>
    public void NotifyTrucoCall(int challengerSeat, int defenderSeat, int stakes, bool byPlayer)
    {
        if (_activeStage == null || !IsInstanceValid(_activeStage)) return;

        _currentLevel = PresentationLevel.Level3_TrucoCall;
        bool motion = SettingsManager.Instance?.ReduceMotion != true;

        // 1. Audio ducking: duck music by -6 dB to give punch to the Truco call
        AudioManager.Instance?.SetDucking(true);

        // 2. Attention: all participants lock onto the challenger
        _activeStage.SetAllAttentionToSeat(challengerSeat);
        _activeStage.SetSeatAttention(challengerSeat, AttentionState.Challenging, Vector3.Zero);

        // 3. Cinematic framing and lighting
        if (motion)
        {
            _activeStage.ApplyDramaticFovPulse(-2.2f, 0.75f);
            _activeStage.ApplyPendantHighlight(1.35f, 0.8f);
        }

        // 4. Stinger for high stakes (9 or 12)
        if (stakes >= 9)
        {
            AudioManager.Instance?.PlaySound("score");
        }
    }

    /// <summary>
    /// Level 3 Response: Challenger or defender answers the Truco call.
    /// Restores audio ducking and transitions focus to responder.
    /// </summary>
    public void NotifyTrucoResponse(int responderSeat, bool accepted)
    {
        if (_activeStage == null || !IsInstanceValid(_activeStage)) return;

        // Restore ducking smoothly
        AudioManager.Instance?.SetDucking(false);

        if (accepted)
        {
            _activeStage.SetSeatAttention(responderSeat, AttentionState.Challenging, Vector3.Zero);
        }
        else
        {
            _activeStage.SetSeatAttention(responderSeat, AttentionState.Defeated, Vector3.Zero);
        }
    }

    /// <summary>
    /// Level 4: Match Point (e.g. 11x11 in Truco, or final trick of deciding hand).
    /// Heightened atmospheric tension: sconces dim slightly, chandelier warms table, tense rhythm.
    /// </summary>
    public void NotifyMatchPoint(int myScore, int oppScore, int maxScore = 12)
    {
        if (_activeStage == null || !IsInstanceValid(_activeStage)) return;

        bool critical = (myScore >= maxScore - 1 && oppScore >= maxScore - 1);
        if (critical && !_isMatchPoint)
        {
            _isMatchPoint = true;
            _currentLevel = PresentationLevel.Level4_MatchPoint;

            // Transition to tense atmosphere
            _activeStage.SetTensionLighting(true);

            // Switch to tension track if available
            AudioManager.Instance?.PlayMusic("last-manilha");
        }
        else if (!critical && _isMatchPoint)
        {
            _isMatchPoint = false;
            _activeStage.SetTensionLighting(false);
        }
    }

    /// <summary>
    /// Level 4: Boss Encounter critical phase (e.g. Boss near defeat in Poker Roguelike).
    /// </summary>
    public void NotifyBossCritical(string bossId, float healthPercent)
    {
        if (_activeStage == null || !IsInstanceValid(_activeStage)) return;

        if (healthPercent <= 0.35f && !_isMatchPoint)
        {
            _isMatchPoint = true;
            _currentLevel = PresentationLevel.Level4_MatchPoint;
            _activeStage.SetTensionLighting(true);
            AudioManager.Instance?.PlaySound("score");
        }
    }
}
