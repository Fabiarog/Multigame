using Godot;
using System.Collections.Generic;
using System.Linq;
using GameHub.Core.Networking;

namespace GameHub.Games.Truco;

/// <summary>
/// Full Truco game manager with proper Brazilian Truco rules:
/// - 40-card deck, 3 cards per player
/// - Vira determines manilhas
/// - Best of 3 rounds (tombos) per hand
/// - Truco/Seis/Nove/Doze raise system
/// - Game to 12 points
/// </summary>
public partial class TrucoGameManager : Node
{
    public enum TrucoPhase
    {
        Dealing,
        Cutting,
        PenaDecision,
        PlayerTurn,
        OpponentTurn,
        TrucoRequested,
        RoundEnd,
        HandEnd,
        GameOver
    }

    // ===== SIGNALS =====
    [Signal] public delegate void PhaseChangedEventHandler(int phase);
    [Signal] public delegate void HandDealtEventHandler();
    [Signal] public delegate void DeckShuffledEventHandler();
    [Signal] public delegate void DeckCutEventHandler(int cutPosition);
    [Signal] public delegate void PenaAvailableEventHandler(string recipient);
    [Signal] public delegate void PenaDeliveredEventHandler(string cardDisplay, string recipient);
    [Signal] public delegate void PenaResolvedEventHandler(bool kept, string cardDisplay);
    [Signal] public delegate void DistributionStartedEventHandler(int cardCount, int penaRecipientSeat, bool penaKept);
    [Signal] public delegate void HandCleanupStartedEventHandler(int nextDealerSeat, string nextDealerName);
    [Signal] public delegate void ViraRevealedEventHandler(string viraDisplay, string manilhaDisplay);
    [Signal] public delegate void ScoreUpdatedEventHandler(int team1, int team2);
    [Signal] public delegate void TrucoCalledEventHandler(int currentStakes, bool byPlayer);
    [Signal] public delegate void CardPlayedEventHandler(int who, string cardDisplay, int roundIdx);
    [Signal] public delegate void RoundResolvedEventHandler(int roundIdx, int winner); // 0=player, 1=opponent, 2=tie
    [Signal] public delegate void HandEndedEventHandler(bool playerWon, int pointsGained);
    [Signal] public delegate void GameEndedEventHandler(bool playerWon, int team1Score, int team2Score);

    // ===== STATE =====
    public TrucoPhase CurrentPhase { get; private set; } = TrucoPhase.Dealing;
    public int PlayerScore { get; private set; } = 0;
    public int OpponentScore { get; private set; } = 0;
    public int CurrentStakes { get; private set; } = 1;
    public int TeamSize { get; private set; } = 1;
    public int BotCount => TeamSize * 2 - 1;
    public TrucoCardData PenaCard { get; private set; }
    public bool PenaWasKept { get; private set; }
    public List<TrucoCardData> AllyPenaCards { get; } = new();
    public List<List<TrucoCardData>> TeamOneHands { get; } = new();
    public List<List<TrucoCardData>> TeamTwoHands { get; } = new();
    public int DealerSeatIndex { get; private set; }
    public int PenaRecipientSeatIndex { get; private set; } = -1;

    // Hand state
    public List<TrucoCardData> PlayerHand { get; private set; } = new();
    public List<TrucoCardData> OpponentHand { get; private set; } = new();
    public TrucoCardData ViraCard { get; private set; }
    public TrucoRank ManilhaRank { get; private set; }

    // Round (tombo) tracking: best of 3
    public TrucoCardData[] PlayerPlayed { get; private set; } = new TrucoCardData[3];
    public TrucoCardData[] OpponentPlayed { get; private set; } = new TrucoCardData[3];
    public int[] RoundWinners { get; private set; } = new int[3]; // 0=player, 1=opponent, 2=tie, -1=not played
    public int CurrentRound { get; private set; } = 0;

    private bool _playerStartsNext = true;
    private bool _waitingTrucoResponse = false;
    private bool _trucoPendingByPlayer = false;
    private TrucoPhase _phaseAfterTrucoResponse = TrucoPhase.PlayerTurn;
    private List<TrucoCardData> _deck = new();
    private bool _penaDelivered;
    private Core.Visuals.AvatarComposite _playerAvatar;
    private Core.Visuals.AvatarComposite _opponentAvatar;
    private readonly List<Sprite3D> _teamSeatSprites = new();

    // AI fields
    private SyncRng _rng;
    private float _aiThinkTimer = 0f;
    private bool _aiThinking = false;

    public const int WinScore = 12;

    public override void _Ready()
    {
        _rng = new SyncRng(GD.Randi());
        _playerAvatar = GetNodeOrNull<Core.Visuals.AvatarComposite>("../Environment/PlayerSprite");
        _opponentAvatar = GetNodeOrNull<Core.Visuals.AvatarComposite>("../Environment/OpponentSprite");
        TeamSize = Mathf.Clamp(Core.Registry.GameRegistry.TrucoTeamSize, 1, 3);
        RefreshTeamSeatVisuals();
        GD.Print("[Truco] Game manager initialized.");
    }

    private void RefreshTeamSeatVisuals()
    {
        foreach (var sprite in _teamSeatSprites) sprite.QueueFree();
        _teamSeatSprites.Clear();
        if (TeamSize == 1) return;

        var environment = GetNodeOrNull<Node3D>("../Environment");
        if (environment == null) return;

        Vector3[] seats =
        {
            new(-3.3f, 1.1f, 0.5f),  // ally, anti-clockwise from player
            new(3.3f, 1.1f, 0.5f),   // ally for 3v3
            new(-3.3f, 1.1f, -3.0f),
            new(3.3f, 1.1f, -3.0f)
        };
        string[] textures =
        {
            "res://assets/sprites/characters/spider/spider_spritesheet.jpg",
            "res://assets/sprites/characters/turtle/turtle_spritesheet.jpg",
            "res://assets/sprites/characters/spider/spider_spritesheet.jpg",
            "res://assets/sprites/characters/turtle/turtle_spritesheet.jpg"
        };
        var shader = ResourceLoader.Load<Shader>("res://assets/shaders/SpatialChromaKey.gdshader");
        for (int i = 0; i < BotCount - 1; i++)
        {
            var sprite = new Sprite3D
            {
                Name = $"TrucoBotSeat{i + 1}", Position = seats[i], PixelSize = 0.004f,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, Transparent = true,
                Hframes = 2, Vframes = 2, Texture = ResourceLoader.Load<Texture2D>(textures[i])
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
            _teamSeatSprites.Add(sprite);
        }
    }

    public override void _Process(double delta)
    {
        if (_aiThinking)
        {
            _aiThinkTimer -= (float)delta;
            if (_aiThinkTimer <= 0)
            {
                _aiThinking = false;
                ExecuteAITurn();
            }
        }
    }

    // ===== PUBLIC API =====

    public void StartMatch()
    {
        PlayerScore = 0;
        OpponentScore = 0;
        _playerStartsNext = true;
        DealerSeatIndex = 0;

        var bgSprite = GetNodeOrNull<Sprite3D>("../Environment/Background");
        if (bgSprite != null)
        {
            string[] maps = {
                "res://assets/sprites/backgrounds/cyber_casino.jpg",
                "res://assets/sprites/backgrounds/neon_lounge.jpg",
                "res://assets/sprites/backgrounds/retro_arcade.jpg"
            };
            string chosenMap = maps[_rng.RandiRange(0, maps.Length - 1)];
            bgSprite.Texture = ResourceLoader.Load<Texture2D>(chosenMap);
            GD.Print($"[Truco] Chosen map: {chosenMap}");
        }

        EmitSignal(SignalName.ScoreUpdated, PlayerScore, OpponentScore);
        StartNewHand();
    }

    public void StartNewHand()
    {
        CurrentStakes = 1;
        CurrentRound = 0;
        _waitingTrucoResponse = false;
        ViraCard = null;
        PenaCard = null;
        AllyPenaCards.Clear();
        for (int i = 0; i < 3; i++)
        {
            PlayerPlayed[i] = null;
            OpponentPlayed[i] = null;
            RoundWinners[i] = -1;
        }

        // The deck stays available until the player cuts it. This makes the
        // shuffle/cut a real game action rather than a cosmetic message.
        _deck = TrucoCardData.CreateDeck();
        _rng.ShuffleList(_deck);
        CurrentPhase = TrucoPhase.Cutting;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.DeckShuffled);
    }

    public async void CutDeck()
    {
        if (CurrentPhase != TrucoPhase.Cutting || _deck.Count == 0) return;

        int cutPosition = _rng.RandiRange(4, _deck.Count - 4);
        var top = _deck.GetRange(0, cutPosition);
        _deck.RemoveRange(0, cutPosition);
        _deck.AddRange(top);
        EmitSignal(SignalName.DeckCut, cutPosition);
        await WaitForAnimation(0.45f);

        if (TeamSize > 1)
        {
            // Seats alternate between teams. The closest team-mate in the
            // anti-clockwise direction is therefore two seats from the dealer.
            PenaRecipientSeatIndex = (DealerSeatIndex + 2) % (TeamSize * 2);
            PenaCard = _deck[0];
            _deck.RemoveAt(0);
            PenaWasKept = false;
            _penaDelivered = false;
            CurrentPhase = TrucoPhase.PenaDecision;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            EmitSignal(SignalName.PenaAvailable, GetSeatName(PenaRecipientSeatIndex));
            return;
        }

        PenaRecipientSeatIndex = -1;
        DealAfterCut();
    }

    public void GivePena()
    {
        if (CurrentPhase != TrucoPhase.PenaDecision || _penaDelivered || PenaCard == null) return;
        _penaDelivered = true;
        EmitSignal(SignalName.PenaDelivered, PenaCard.ToString(), GetSeatName(PenaRecipientSeatIndex));
    }

    public async void ResolvePena(bool keep)
    {
        if (CurrentPhase != TrucoPhase.PenaDecision) return;
        if (!_penaDelivered)
        {
            keep = false;
            if (PenaCard != null)
                _deck.Insert(0, PenaCard);
        }
        PenaWasKept = keep;
        if (keep && PenaCard != null)
            AllyPenaCards.Add(PenaCard);
        if (!keep && _penaDelivered)
            ViraCard = PenaCard;
        EmitSignal(SignalName.PenaResolved, PenaWasKept, PenaCard?.ToString() ?? "");
        await WaitForAnimation(0.45f);
        DealAfterCut();
    }

    private async void DealAfterCut()
    {
        TeamOneHands.Clear();
        TeamTwoHands.Clear();
        for (int i = 0; i < TeamSize; i++)
        {
            TeamOneHands.Add(new List<TrucoCardData>());
            TeamTwoHands.Add(new List<TrucoCardData>());
        }

        if (PenaWasKept && PenaCard != null && PenaRecipientSeatIndex >= 0)
            GetHandForSeat(PenaRecipientSeatIndex).Add(PenaCard);

        int totalSeats = TeamSize * 2;
        int distributedCards = 0;
        for (int pass = 0; pass < 3; pass++)
        {
            for (int offset = 1; offset <= totalSeats; offset++)
            {
                int seat = (DealerSeatIndex + offset) % totalSeats;
                var hand = GetHandForSeat(seat);
                if (hand.Count >= 3) continue;
                if (_deck.Count == 0) break;
                hand.Add(_deck[0]);
                _deck.RemoveAt(0);
                distributedCards++;
            }
        }

        PlayerHand = TeamOneHands[0];
        OpponentHand = TeamTwoHands[0];
        ViraCard ??= _deck[0];
        ManilhaRank = TrucoCardData.GetManilhaRank(ViraCard.Rank);
        ValidateDistributedHands();

        EmitSignal(SignalName.DistributionStarted, distributedCards, PenaRecipientSeatIndex, PenaWasKept);
        await WaitForAnimation(0.45f + distributedCards * 0.045f);

        CurrentPhase = TrucoPhase.PlayerTurn;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.HandDealt);
        EmitSignal(SignalName.ViraRevealed, ViraCard.ToString(), $"Manilha: {ManilhaRank}");

        GD.Print($"[Truco] New hand. Vira: {ViraCard}, Manilha rank: {ManilhaRank}");
        GD.Print($"[Truco] Player hand: {string.Join(", ", PlayerHand)}");

        if (!_playerStartsNext)
        {
            CurrentPhase = TrucoPhase.OpponentTurn;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            StartAIThinking();
        }

        _playerStartsNext = !_playerStartsNext;
    }

    /// <summary>
    /// Player plays a card at the given index from their hand.
    /// </summary>
    public async void PlayerPlayCard(int handIndex)
    {
        if (CurrentPhase != TrucoPhase.PlayerTurn) return;
        if (PlayerPlayed[CurrentRound] != null) return;
        if (handIndex < 0 || handIndex >= PlayerHand.Count) return;

        var card = PlayerHand[handIndex];
        _playerAvatar?.SetState(Core.Visuals.AvatarComposite.AnimState.Action);
        PlayerHand.RemoveAt(handIndex);
        PlayerPlayed[CurrentRound] = card;

        EmitSignal(SignalName.CardPlayed, 0, card.ToString(), CurrentRound);
        GD.Print($"[Truco] Player plays: {card}");

        // If opponent already played this round, resolve
        if (OpponentPlayed[CurrentRound] != null)
        {
            await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
            ResolveCurrentRound();
        }
        else
        {
            // Opponent's turn
            CurrentPhase = TrucoPhase.OpponentTurn;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            StartAIThinking();
        }
    }

    /// <summary>
    /// Player requests Truco (or raise).
    /// </summary>
    public void RequestTruco()
    {
        if (CurrentPhase != TrucoPhase.PlayerTurn && CurrentPhase != TrucoPhase.OpponentTurn) return;
        if (CurrentStakes >= 12) return;

        int newStakes = CurrentStakes switch
        {
            1 => 3,
            3 => 6,
            6 => 9,
            9 => 12,
            _ => 12
        };
        CurrentStakes = newStakes;
        _trucoPendingByPlayer = true;
        _waitingTrucoResponse = true;
        _phaseAfterTrucoResponse = CurrentPhase;
        _playerAvatar?.SetState(Core.Visuals.AvatarComposite.AnimState.Truco);

        CurrentPhase = TrucoPhase.TrucoRequested;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.TrucoCalled, CurrentStakes, true);

        GD.Print($"[Truco] Player calls! Stakes: {CurrentStakes}");

        // AI decides whether to accept (simple: accept if has manilha or 3)
        RespondAIToTrucoDelayed();
    }

    public void RespondToTruco(bool accept, bool raise)
    {
        if (!_waitingTrucoResponse) return;
        _waitingTrucoResponse = false;

        if (raise && CurrentStakes < 12)
        {
            // A counter-raise changes who is waiting for an answer.
            CurrentStakes = CurrentStakes switch
            {
                3 => 6,
                6 => 9,
                9 => 12,
                _ => 12
            };
            _trucoPendingByPlayer = true;
            _waitingTrucoResponse = true;
            EmitSignal(SignalName.TrucoCalled, CurrentStakes, true);
            GD.Print($"[Truco] Player raises! Stakes: {CurrentStakes}");
            CurrentPhase = TrucoPhase.TrucoRequested;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            RespondAIToTrucoDelayed();
        }
        else if (accept)
        {
            GD.Print("[Truco] Truco accepted!");
            // Resume the exact action that was paused by the call. In
            // particular, an AI call made after the player placed a card must
            // return to the AI, otherwise the player can overwrite that card.
            CurrentPhase = _phaseAfterTrucoResponse;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            if (CurrentPhase == TrucoPhase.OpponentTurn)
            {
                StartAIThinking();
            }
        }
        else
        {
            // Declined — opponent/player who called gets points of previous stakes
            int points = CurrentStakes switch
            {
                3 => 1,
                6 => 3,
                9 => 6,
                12 => 9,
                _ => 1
            };

            if (_trucoPendingByPlayer)
            {
                // Player called, opponent ran
                PlayerScore += points;
            }
            else
            {
                // Opponent called, player ran
                OpponentScore += points;
            }

            GD.Print($"[Truco] Truco declined! {points} points awarded.");
            EmitSignal(SignalName.ScoreUpdated, PlayerScore, OpponentScore);
            EndHand(_trucoPendingByPlayer);
        }
    }

    // ===== ROUND RESOLUTION =====

    private void ResolveCurrentRound()
    {
        var pCard = PlayerPlayed[CurrentRound];
        var oCard = OpponentPlayed[CurrentRound];
        int cmp = TrucoCardData.Compare(pCard, oCard, ManilhaRank);

        int winner;
        if (cmp > 0) winner = 0; // Player wins
        else if (cmp < 0) winner = 1; // Opponent wins
        else winner = 2; // Tie

        RoundWinners[CurrentRound] = winner;
        GD.Print($"[Truco] Round {CurrentRound + 1}: {pCard} vs {oCard} → {(winner == 0 ? "Player" : winner == 1 ? "Opponent" : "Tie")}");
        EmitSignal(SignalName.RoundResolved, CurrentRound, winner);

        // Check if hand is decided (best of 3)
        int playerWins = RoundWinners.Count(w => w == 0);
        int opponentWins = RoundWinners.Count(w => w == 1);
        int ties = RoundWinners.Count(w => w == 2);

        bool handDecided = false;
        bool playerWonHand = false;

        if (playerWins >= 2) { handDecided = true; playerWonHand = true; }
        else if (opponentWins >= 2) { handDecided = true; playerWonHand = false; }
        else if (CurrentRound >= 2)
        {
            // All 3 rounds played
            handDecided = true;
            if (playerWins > opponentWins) playerWonHand = true;
            else if (opponentWins > playerWins) playerWonHand = false;
            else
            {
                // All ties or equal wins — first round winner takes it, or player who went first
                if (RoundWinners[0] == 0) playerWonHand = true;
                else if (RoundWinners[0] == 1) playerWonHand = false;
                else playerWonHand = _playerStartsNext; // whoever started
            }
        }

        if (handDecided)
        {
            if (playerWonHand) PlayerScore += CurrentStakes;
            else OpponentScore += CurrentStakes;

            EmitSignal(SignalName.ScoreUpdated, PlayerScore, OpponentScore);
            EndHand(playerWonHand);
        }
        else
        {
            // Next round
            CurrentRound++;
            CurrentPhase = TrucoPhase.PlayerTurn;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            // In truco, winner of round plays first next round
            if (winner == 1)
            {
                CurrentPhase = TrucoPhase.OpponentTurn;
                EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
                StartAIThinking();
            }
        }
    }

    private void EndHand(bool playerWon)
    {
        FinishHandSequence(playerWon);
    }

    private async void FinishHandSequence(bool playerWon)
    {
        int totalSeats = TeamSize * 2;
        DealerSeatIndex = (DealerSeatIndex + 1) % totalSeats;
        EmitSignal(SignalName.HandCleanupStarted, DealerSeatIndex, GetSeatName(DealerSeatIndex));
        await WaitForAnimation(0.9f);
        EmitSignal(SignalName.HandEnded, playerWon, CurrentStakes);

        if (PlayerScore >= WinScore || OpponentScore >= WinScore)
        {
            CurrentPhase = TrucoPhase.GameOver;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            EmitSignal(SignalName.GameEnded, PlayerScore >= WinScore, PlayerScore, OpponentScore);
            GD.Print($"[Truco] Game Over! Player: {PlayerScore}, Opponent: {OpponentScore}");
        }
        else
        {
            CurrentPhase = TrucoPhase.HandEnd;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        }
    }

    private List<TrucoCardData> GetHandForSeat(int seat)
    {
        return seat % 2 == 0 ? TeamOneHands[seat / 2] : TeamTwoHands[seat / 2];
    }

    private void ValidateDistributedHands()
    {
        for (int i = 0; i < TeamSize; i++)
        {
            if (TeamOneHands[i].Count != 3 || TeamTwoHands[i].Count != 3)
                GD.PushError($"[Truco] Invalid distribution at pair {i}: {TeamOneHands[i].Count}/{TeamTwoHands[i].Count} cards.");
        }

        if (PenaWasKept && PenaRecipientSeatIndex >= 0 && !GetHandForSeat(PenaRecipientSeatIndex).Contains(PenaCard))
            GD.PushError("[Truco] The kept pena was not included in its recipient's three-card hand.");
    }

    public string GetSeatName(int seat)
    {
        if (seat == 0) return "Você";
        string team = seat % 2 == 0 ? "Aliado" : "Adversário";
        return $"{team} {(seat / 2) + 1}";
    }

    private async System.Threading.Tasks.Task WaitForAnimation(float seconds)
    {
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion == true) return;
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    // ===== AI =====

    private void StartAIThinking()
    {
        _aiThinking = true;
        _aiThinkTimer = (float)GD.RandRange(0.8, 2.0);
    }

    private async void ExecuteAITurn()
    {
        if (OpponentHand.Count == 0) return;
        if (OpponentPlayed[CurrentRound] != null) return;

        // Simple AI: play strongest card if losing, weakest if winning
        int playerWins = RoundWinners.Count(w => w == 0);
        int opponentWins = RoundWinners.Count(w => w == 1);

        // Sort by strength
        var sorted = OpponentHand.OrderBy(c => c.GetStrength(ManilhaRank)).ToList();

        TrucoCardData chosen;
        if (opponentWins > playerWins)
        {
            // Winning: play weakest
            chosen = sorted.First();
        }
        else
        {
            // Losing or tied: play strongest
            chosen = sorted.Last();

            // Chance to call truco if has strong cards
            if (CurrentStakes == 1 && sorted.Last().GetStrength(ManilhaRank) >= 100 && GD.Randf() > 0.4f)
            {
                AICallTruco();
                return;
            }
        }

        OpponentHand.Remove(chosen);
        _opponentAvatar?.SetState(Core.Visuals.AvatarComposite.AnimState.Action);
        OpponentPlayed[CurrentRound] = chosen;

        EmitSignal(SignalName.CardPlayed, 1, chosen.ToString(), CurrentRound);
        GD.Print($"[Truco] Opponent plays: {chosen}");

        // If player already played, resolve
        if (PlayerPlayed[CurrentRound] != null)
        {
            await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
            ResolveCurrentRound();
        }
        else
        {
            CurrentPhase = TrucoPhase.PlayerTurn;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        }
    }

    private void AICallTruco()
    {
        int newStakes = CurrentStakes switch
        {
            1 => 3, 3 => 6, 6 => 9, 9 => 12, _ => 12
        };
        if (newStakes > 12) { ExecuteAITurn(); return; }

        CurrentStakes = newStakes;
        _trucoPendingByPlayer = false;
        _waitingTrucoResponse = true;
        // The AI calls before it places its pending card, so it must keep the
        // turn after the player accepts regardless of who started the tombo.
        _phaseAfterTrucoResponse = CurrentPhase;
        _opponentAvatar?.SetState(Core.Visuals.AvatarComposite.AnimState.Truco);

        CurrentPhase = TrucoPhase.TrucoRequested;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.TrucoCalled, CurrentStakes, false);
        GD.Print($"[Truco] Opponent calls! Stakes: {CurrentStakes}");
    }

    private async void RespondAIToTrucoDelayed()
    {
        await ToSignal(GetTree().CreateTimer(0.8f), SceneTreeTimer.SignalName.Timeout);
        if (_waitingTrucoResponse && _trucoPendingByPlayer)
            AIRespondToTruco();
    }

    private void AIRespondToTruco()
    {
        // Simple: accept if has any manilha or 3
        bool hasStrong = OpponentHand.Any(c => c.GetStrength(ManilhaRank) >= 13 || c.GetStrength(ManilhaRank) >= 100);

        if (hasStrong || GD.Randf() > 0.5f)
        {
            // Accept
            _waitingTrucoResponse = false;
            GD.Print("[Truco] AI accepts truco!");
            CurrentPhase = _phaseAfterTrucoResponse;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            if (CurrentPhase == TrucoPhase.OpponentTurn)
                StartAIThinking();
        }
        else
        {
            // Decline
            RespondToTruco(false, false);
        }
    }
}
