using Godot;
using System.Collections.Generic;
using System.Linq;
using GameHub.Core.Networking;

namespace GameHub.Games.PokerRoguelike;

/// <summary>
/// Core game loop for the Poker Roguelike.
/// Balatro-inspired: deal 8 cards, player selects up to 5 to play or discard.
/// Scoring uses Chips × Mult. Beat the round target to advance.
/// </summary>
public partial class PokerGameManager : Node
{
    public enum GamePhase
    {
        Dealing,
        PlayerTurn,
        Scoring,
        RoundEnd,
        GameOver,
        GameWon
    }

    // ===== SIGNALS =====

    [Signal]
    public delegate void PhaseChangedEventHandler(int phase);

    [Signal]
    public delegate void HandDealtEventHandler();

    [Signal]
    public delegate void ScoreUpdatedEventHandler(int roundScore, int roundTarget);

    [Signal]
    public delegate void HandScoredEventHandler(string handName, int score, string breakdown);

    [Signal]
    public delegate void RoundEndedEventHandler(int round, bool passed);

    [Signal]
    public delegate void GameEndedEventHandler(bool won, int totalScore);

    // ===== STATE =====

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Dealing;
    public int CurrentRound { get; private set; } = 1;
    public int HandsRemaining { get; private set; }
    public int DiscardsRemaining { get; private set; }
    public int RoundScore { get; private set; } = 0;
    public int RoundTarget { get; private set; }
    public int TotalScore { get; private set; } = 0;

    // ===== CONSTANTS =====

    public const int HandsPerRound = 4;
    public const int DiscardsPerRound = 3;
    public const int HandSize = 8;
    public const int MaxPlayCards = 5;
    public const int MaxRounds = 8;

    // Escalating round targets
    private static readonly int[] RoundTargets = { 300, 450, 600, 800, 1100, 1400, 1800, 2200 };

    // ===== INTERNALS =====

    private List<CardData> _deck = new();
    private List<CardData> _playerHand = new();
    private HashSet<int> _selectedIndices = new();
    private SyncRng _rng;

    private PokerBossAI _boss;
    private Sprite3D _playerSprite;
    private float _frameTimer = 0f;
    private int _currentFrame = 0;

    // ===== LIFECYCLE =====

    public override void _Ready()
    {
        _rng = new SyncRng(GD.Randi());
        _boss = GetNodeOrNull<PokerBossAI>("../Environment/BossAI");

        if (Core.Registry.GameRegistry.IsTutorialMode)
        {
            var tutorialController = new Core.AI.TutorialController();
            tutorialController.Name = "TutorialController";
            GetParent().CallDeferred("add_child", tutorialController);
        }

        // Load Player Avatar
        _playerSprite = GetNodeOrNull<Sprite3D>("../Environment/PlayerSprite");
        if (_playerSprite != null)
        {
            string avatar = Core.Systems.SettingsManager.Instance?.AvatarId ?? "default";
            string texPath = avatar switch
            {
                "spider" => "res://assets/sprites/characters/spider/spider_spritesheet.jpg",
                _ => "res://assets/sprites/characters/turtle/turtle_spritesheet.jpg" // Default to turtle
            };
            if (ResourceLoader.Exists(texPath))
            {
                _playerSprite.Texture = ResourceLoader.Load<Texture2D>(texPath);

                var shader = ResourceLoader.Load<Shader>("res://assets/shaders/SpatialChromaKey.gdshader");
                if (shader != null)
                {
                    var mat = new ShaderMaterial();
                    mat.Shader = shader;
                    mat.SetShaderParameter("chroma_color", new Color(0.0f, 1.0f, 0.0f)); 
                    mat.SetShaderParameter("chroma_threshold", 0.35f);
                    mat.SetShaderParameter("chroma_smoothing", 0.1f);
                    mat.SetShaderParameter("hframes", 2);
                    mat.SetShaderParameter("vframes", 2);
                    mat.SetShaderParameter("sprite_texture", _playerSprite.Texture);
                    _playerSprite.MaterialOverride = mat;
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_playerSprite != null && _playerSprite.Texture != null && _playerSprite.Visible)
        {
            _frameTimer += (float)delta;
            if (_frameTimer >= 0.2f)
            {
                _frameTimer = 0f;
                _currentFrame = (_currentFrame + 1) % 4;
                _playerSprite.Frame = _currentFrame;

                if (_playerSprite.MaterialOverride is ShaderMaterial smat)
                {
                    smat.SetShaderParameter("frame", _currentFrame);
                }
            }
        }
    }

    // ===== PUBLIC API =====

    public void StartNewGame()
    {
        CurrentRound = 0;
        TotalScore = 0;
        StartNextRound();
    }

    public void StartNextRound()
    {
        CurrentRound++;
        if (CurrentRound > MaxRounds)
        {
            CurrentPhase = GamePhase.GameWon;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            EmitSignal(SignalName.GameEnded, true, TotalScore);
            GD.Print($"[Poker] Game Won! Total score: {TotalScore}");
            return;
        }

        if (_boss != null)
        {
            _boss.SetupForRound(CurrentRound);
        }

        RoundScore = 0;
        HandsRemaining = HandsPerRound;
        DiscardsRemaining = DiscardsPerRound;
        RoundTarget = CurrentRound <= RoundTargets.Length
            ? RoundTargets[CurrentRound - 1]
            : RoundTargets[RoundTargets.Length - 1] + (CurrentRound - RoundTargets.Length) * 500;

        // Create and shuffle a fresh deck
        _deck = CardData.CreateDeck();
        _rng.ShuffleList(_deck);

        // Deal initial hand
        _playerHand.Clear();
        _selectedIndices.Clear();
        DrawCardsToFillHand();

        CurrentPhase = GamePhase.PlayerTurn;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.HandDealt);
        EmitSignal(SignalName.ScoreUpdated, RoundScore, RoundTarget);

        GD.Print($"[Poker] Round {CurrentRound} started. Target: {RoundTarget}. Hand: {string.Join(", ", _playerHand)}");
    }

    public List<CardData> GetPlayerHand() => _playerHand;
    public HashSet<int> GetSelectedIndices() => _selectedIndices;

    public void ToggleCard(int index)
    {
        if (CurrentPhase != GamePhase.PlayerTurn) return;
        if (index < 0 || index >= _playerHand.Count) return;

        if (_selectedIndices.Contains(index))
        {
            _selectedIndices.Remove(index);
        }
        else if (_selectedIndices.Count < MaxPlayCards)
        {
            _selectedIndices.Add(index);
        }
    }

    public bool CanPlayHand()
    {
        return _selectedIndices.Count > 0
            && _selectedIndices.Count <= MaxPlayCards
            && CurrentPhase == GamePhase.PlayerTurn
            && HandsRemaining > 0;
    }

    public bool CanDiscard()
    {
        return _selectedIndices.Count > 0
            && _selectedIndices.Count <= MaxPlayCards
            && CurrentPhase == GamePhase.PlayerTurn
            && DiscardsRemaining > 0;
    }

    public void PlayHand()
    {
        if (!CanPlayHand()) return;

        // Gather selected cards
        var playedCards = _selectedIndices.OrderBy(i => i).Select(i => _playerHand[i]).ToList();

        // Evaluate
        var result = HandEvaluator.Evaluate(playedCards);
        GD.Print($"[Poker] Played: {result.HandName} = {result.GetScoreBreakdown()}");

        // Update score
        RoundScore += result.TotalScore;
        TotalScore += result.TotalScore;
        HandsRemaining--;

        // Remove played cards (descending order to preserve indices)
        foreach (int idx in _selectedIndices.OrderByDescending(i => i))
        {
            _playerHand.RemoveAt(idx);
        }
        _selectedIndices.Clear();

        // Draw new cards to refill hand
        DrawCardsToFillHand();

        // Emit scoring signals
        EmitSignal(SignalName.HandScored, result.HandName, result.TotalScore, result.GetScoreBreakdown());
        EmitSignal(SignalName.ScoreUpdated, RoundScore, RoundTarget);

        _boss?.ReactToPlayerHand(RoundScore, RoundTarget);

        // Check win condition
        if (RoundScore >= RoundTarget)
        {
            CurrentPhase = GamePhase.RoundEnd;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            EmitSignal(SignalName.RoundEnded, CurrentRound, true);
            GD.Print($"[Poker] Round {CurrentRound} passed! Score: {RoundScore}/{RoundTarget}");
            return;
        }

        // Check if out of hands
        if (HandsRemaining <= 0)
        {
            CurrentPhase = GamePhase.GameOver;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            EmitSignal(SignalName.GameEnded, false, TotalScore);
            GD.Print($"[Poker] Game Over. Score: {RoundScore}/{RoundTarget}");
            return;
        }

        // Continue playing
        EmitSignal(SignalName.HandDealt);
    }

    public void DiscardCards()
    {
        if (!CanDiscard()) return;

        GD.Print($"[Poker] Discarding {_selectedIndices.Count} cards. Discards remaining: {DiscardsRemaining - 1}");

        // Remove selected cards
        foreach (int idx in _selectedIndices.OrderByDescending(i => i))
        {
            _playerHand.RemoveAt(idx);
        }
        _selectedIndices.Clear();
        DiscardsRemaining--;

        // Draw replacements
        DrawCardsToFillHand();

        EmitSignal(SignalName.HandDealt);
        EmitSignal(SignalName.ScoreUpdated, RoundScore, RoundTarget);
    }

    // ===== INTERNAL =====

    private void DrawCardsToFillHand()
    {
        while (_playerHand.Count < HandSize && _deck.Count > 0)
        {
            _playerHand.Add(_deck[0]);
            _deck.RemoveAt(0);
        }
    }
}
