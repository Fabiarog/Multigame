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
        Shop,
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
    public int OpponentCount { get; private set; } = 1;

    // --- Roguelike Shop State ---
    public int Gold { get; private set; } = 0;
    public int BaseMultiplierBonus { get; private set; } = 0;
    public int ExtraHands { get; private set; } = 0;
    public int ExtraDiscards { get; private set; } = 0;
    public int DeckCount => _deck?.Count ?? 52;

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
    private List<CardData> _dealerHand = new();
    private HashSet<int> _selectedIndices = new();
    private SyncRng _rng;

    private PokerBossAI _boss;
    private Core.Visuals.AvatarComposite _playerAvatar;
    private RelicManager _relics;
    private readonly List<Sprite3D> _additionalOpponentVisuals = new();
    private static readonly Dictionary<string, Texture2D> _cachedMaps = new();

    public RelicManager Relics => _relics;

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
        _playerAvatar = GetNodeOrNull<Core.Visuals.AvatarComposite>("../Environment/PlayerSprite");

        _relics = new RelicManager { Name = "RelicManager" };
        AddChild(_relics);
    }

    public override void _Process(double delta)
    {
        // Animation is now handled by AvatarComposite internally.
    }

    // ===== PUBLIC API =====

    public void StartNewGame()
    {
        CurrentRound = 0;
        TotalScore = 0;
        Gold = 0;
        BaseMultiplierBonus = 0;
        ExtraHands = 0;
        ExtraDiscards = 0;
        _relics.StartRun();
        OpponentCount = Mathf.Clamp(Core.Registry.GameRegistry.SoloBotCount, 1, 3);
        RefreshOpponentVisuals();
        // Random Map Selection
        var bgSprite = GetNodeOrNull<Sprite3D>("../Environment/Background");
        if (bgSprite != null)
        {
            string[] maps = {
                "res://assets/sprites/backgrounds/cyber_casino.jpg",
                "res://assets/sprites/backgrounds/neon_lounge.jpg",
                "res://assets/sprites/backgrounds/retro_arcade.jpg"
            };
            string chosenMap = maps[_rng.RandiRange(0, maps.Length - 1)];
            if (!_cachedMaps.TryGetValue(chosenMap, out var tex) || !IsInstanceValid(tex))
            {
                tex = GD.Load<Texture2D>(chosenMap);
                if (tex != null) _cachedMaps[chosenMap] = tex;
            }
            if (tex != null) bgSprite.Texture = tex;
            GD.Print($"[Poker] Chosen map: {chosenMap}");
        }

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
        HandsRemaining = HandsPerRound + ExtraHands + _relics.GetExtraHands();
        DiscardsRemaining = DiscardsPerRound + ExtraDiscards + _relics.GetExtraDiscards();
        int baseTarget = CurrentRound <= RoundTargets.Length
            ? RoundTargets[CurrentRound - 1]
            : RoundTargets[RoundTargets.Length - 1] + (CurrentRound - RoundTargets.Length) * 500;
        float difficultyMultiplier = Core.Registry.GameRegistry.SelectedSoloDifficulty switch
        {
            Core.Registry.GameRegistry.SoloDifficulty.Fácil => 0.8f,
            Core.Registry.GameRegistry.SoloDifficulty.Difícil => 1.25f,
            _ => 1.0f
        };
        RoundTarget = Mathf.RoundToInt(baseTarget * difficultyMultiplier * (1f + (OpponentCount - 1) * 0.15f));

        // Create and shuffle a fresh deck
        _deck = CardData.CreateDeck();
        _rng.ShuffleList(_deck);

        // Deal initial hand
        _playerHand.Clear();
        _selectedIndices.Clear();
        DrawCardsToFillHand();
        DrawDealerCards();

        CurrentPhase = GamePhase.PlayerTurn;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.HandDealt);
        EmitSignal(SignalName.ScoreUpdated, RoundScore, RoundTarget);

        GD.Print($"[Poker] Round {CurrentRound} started. Target: {RoundTarget}. Hand: {string.Join(", ", _playerHand)}");
    }

    public List<CardData> GetPlayerHand() => _playerHand;
    public List<CardData> GetDealerHand() => _dealerHand;
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

    public async void PlayHand()
    {
        if (!CanPlayHand()) return;

        // Gather selected cards
        var playedCards = _selectedIndices.OrderBy(i => i).Select(i => _playerHand[i]).ToList();
        _playerAvatar?.SetState(Core.Visuals.AvatarComposite.AnimState.Action);

        // Evaluate
        var result = HandEvaluator.Evaluate(playedCards);
        result.Mult += BaseMultiplierBonus;
        _relics.ApplyHandEffects(result, playedCards);
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

        // Allow cards to fly and land on the felt before showing score calculations
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion != true)
        {
            await ToSignal(GetTree().CreateTimer(0.85f), SceneTreeTimer.SignalName.Timeout);
            if (!IsInsideTree()) return;
        }

        // Emit scoring signals
        EmitSignal(SignalName.HandScored, result.HandName, result.TotalScore, result.GetScoreBreakdown());
        EmitSignal(SignalName.ScoreUpdated, RoundScore, RoundTarget);

        _boss?.ReactToPlayerHand(RoundScore, RoundTarget);

        // Wait to show the result and score breakdown
        float waitResult = Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? 0.3f : 2.2f;
        await ToSignal(GetTree().CreateTimer(waitResult), SceneTreeTimer.SignalName.Timeout);
        if (!IsInsideTree()) return;

        _playerAvatar?.SetState(
            RoundScore >= RoundTarget
                ? Core.Visuals.AvatarComposite.AnimState.React
                : Core.Visuals.AvatarComposite.AnimState.Idle);

        // Draw new cards to refill hand
        DrawCardsToFillHand();
        EmitSignal(SignalName.HandDealt);

        // Check win condition
        if (RoundScore >= RoundTarget)
        {
            // Award gold based on overscore
            int overscore = RoundScore - RoundTarget;
            int goldEarned = 5 + (overscore / 100);
            Gold += goldEarned;

            CurrentPhase = GamePhase.Shop;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            EmitSignal(SignalName.RoundEnded, CurrentRound, true);
            GD.Print($"[Poker] Round {CurrentRound} passed! Score: {RoundScore}/{RoundTarget}. Earned {goldEarned} Gold.");
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

    public bool BuyUpgrade(string upgradeType, int cost)
    {
        if (Gold < cost) return false;
        
        switch (upgradeType)
        {
            case "multiplier":
                BaseMultiplierBonus++;
                break;
            case "hand":
                ExtraHands++;
                break;
            case "discard":
                ExtraDiscards++;
                break;
            default:
                return false;
        }
        
        Gold -= cost;
        return true;
    }

    public bool BuyRelic(RelicManager.RelicId relic, int cost)
    {
        if (Gold < cost || _relics == null || !_relics.CanAddRelic) return false;
        if (!_relics.AddRelic(relic)) return false;
        Gold -= cost;
        return true;
    }

    public bool RerollShop(int cost)
    {
        if (Gold < cost) return false;
        Gold -= cost;
        return true;
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

    private void DrawDealerCards()
    {
        _dealerHand.Clear();
        const int dealerHandSize = 5;
        while (_dealerHand.Count < dealerHandSize && _deck.Count > 0)
        {
            _dealerHand.Add(_deck[0]);
            _deck.RemoveAt(0);
        }
    }

    private void RefreshOpponentVisuals()
    {
        foreach (var sprite in _additionalOpponentVisuals)
            sprite.QueueFree();
        _additionalOpponentVisuals.Clear();

        var environment = GetNodeOrNull<Node3D>("../Environment");
        if (environment == null) return;

        // BossAI is bot 1 in the centre. Create visible seats for every extra
        // selected bot so a 3-bot table no longer looks like a 1v1 match.
        Vector3[] positions = { new(-2.4f, 1.5f, -2.4f), new(2.4f, 1.5f, -2.4f) };
        string[] textures =
        {
            "res://assets/sprites/characters/spider/spider_spritesheet.jpg",
            "res://assets/sprites/characters/turtle/turtle_spritesheet.jpg"
        };

        var shader = ResourceLoader.Load<Shader>("res://assets/shaders/SpatialChromaKey.gdshader");
        for (int i = 0; i < OpponentCount - 1; i++)
        {
            var sprite = new Sprite3D
            {
                Name = $"OpponentBot{i + 2}",
                Position = positions[i],
                PixelSize = 0.004f,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Transparent = true,
                Hframes = 2,
                Vframes = 2,
                Texture = ResourceLoader.Load<Texture2D>(textures[i])
            };

            if (shader != null && sprite.Texture != null)
            {
                var material = new ShaderMaterial { Shader = shader };
                material.SetShaderParameter("chroma_color", new Color(0, 1, 0));
                material.SetShaderParameter("chroma_threshold", 0.35f);
                material.SetShaderParameter("chroma_smoothing", 0.1f);
                material.SetShaderParameter("sprite_texture", sprite.Texture);
                sprite.MaterialOverride = material;
            }
            environment.AddChild(sprite);
            _additionalOpponentVisuals.Add(sprite);
        }
    }
}
