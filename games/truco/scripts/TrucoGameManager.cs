using Godot;
using System.Collections.Generic;
using System.Linq;
using GameHub.Core.Networking;
using GameHub.Core.Visuals;

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
        GameOver,
        Shuffling
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
    [Signal] public delegate void TrucoRespondedEventHandler(bool accepted, bool byPlayer);
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
    public int CutterSeatIndex => (DealerSeatIndex - 1 + TeamSize * 2) % (TeamSize * 2);
    public bool CutterIsPlayer => CutterSeatIndex == 0;
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
    private int _handId = 0;

    private bool _waitingTrucoResponse = false;
    private bool _trucoPendingByPlayer = false;
    private TrucoPhase _phaseAfterTrucoResponse = TrucoPhase.PlayerTurn;
    private List<TrucoCardData> _deck = new();
    private bool _penaDelivered;
    private bool _resolvingPena;
    private readonly Dictionary<int, TrucoCardData> _playedSeats = new();
    private static readonly Dictionary<string, Texture2D> _cachedMaps = new();
    public int ActiveSeatIndex { get; private set; }
    public int LastPlayedSeatIndex { get; private set; }
    public bool PenaDecisionIsLocal => PenaRecipientSeatIndex == 0;
    public bool PenaDecisionIsBot => PenaRecipientSeatIndex != 0;
    public int GetSeatCardCount(int seat) => TeamOneHands.Count == 0 ? 0 : GetHandForSeat(seat).Count;
    private int _roundLeaderSeat;
    private Core.Visuals.AvatarComposite _playerAvatar;
    private Core.Visuals.AvatarComposite _opponentAvatar;
    private readonly List<Sprite3D> _teamSeatSprites = new();

    // AI fields
    private SyncRng _rng;
    private float _aiThinkTimer = 0f;
    private bool _aiThinking = false;
    private float _aiCutTimer = 0f;
    private bool _aiCutting = false;
    private float _aiPenaTimer = 0f;
    private bool _aiPenaThinking = false;
    private float _shuffleTimer;
    public bool CanOfferPena => CurrentPhase == TrucoPhase.PenaDecision && CutterIsPlayer && !_penaDelivered && !_resolvingPena;
    public bool CanResolvePena => CurrentPhase == TrucoPhase.PenaDecision && PenaDecisionIsLocal && _penaDelivered && !_resolvingPena;

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
        if (CurrentPhase == TrucoPhase.Shuffling)
        {
            _shuffleTimer -= (float)delta;
            if (_shuffleTimer <= 0)
            {
                CurrentPhase = TrucoPhase.Cutting;
                _aiCutting = !CutterIsPlayer;
                _aiCutTimer = Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? .35f : 1.25f;
                EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            }
            return;
        }
        if (_aiThinking)
        {
            _aiThinkTimer -= (float)delta;
            if (_aiThinkTimer <= 0)
            {
                _aiThinking = false;
                ExecuteAITurn();
            }
        }
        if (_aiCutting)
        {
            _aiCutTimer -= (float)delta;
            if (_aiCutTimer <= 0)
            {
                _aiCutting = false;
                if (CurrentPhase == TrucoPhase.Cutting)
                    CutDeckForSeat(CutterSeatIndex);
            }
        }
        if (_aiPenaThinking)
        {
            _aiPenaTimer -= (float)delta;
            if (_aiPenaTimer <= 0)
            {
                _aiPenaThinking = false;
                if (CurrentPhase == TrucoPhase.PenaDecision && !_penaDelivered)
                    DeliverPena();
            }
        }
    }

    // ===== PUBLIC API =====

    public void StartMatch()
    {
        PlayerScore = 0;
        OpponentScore = 0;
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
            if (!_cachedMaps.TryGetValue(chosenMap, out var tex) || !IsInstanceValid(tex))
            {
                tex = GD.Load<Texture2D>(chosenMap);
                if (tex != null) _cachedMaps[chosenMap] = tex;
            }
            if (tex != null) bgSprite.Texture = tex;
            GD.Print($"[Truco] Chosen map: {chosenMap}");
        }

        EmitSignal(SignalName.ScoreUpdated, PlayerScore, OpponentScore);
        StartNewHand();
    }

    public void StartNewHand()
    {
        _handId++;
        CurrentStakes = 1;
        CurrentRound = 0;
        _resolvingPena = false;
        _aiThinking = false;
        _playedSeats.Clear();
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

        // Dealer and cutter rotate with the seat order. Only seat zero is local.
        _aiCutting = false;
        _aiPenaThinking = false;
        _deck = TrucoCardData.CreateDeck();
        _rng.ShuffleList(_deck);
        CurrentPhase = TrucoPhase.Shuffling;
        _shuffleTimer = Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? .25f : .95f;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.DeckShuffled);

    }

    public void CutDeck()
    {
        if (CutterIsPlayer) CutDeckForSeat(0);
    }

    private async void CutDeckForSeat(int seat)
    {
        if (seat != CutterSeatIndex || CurrentPhase != TrucoPhase.Cutting || _deck.Count == 0) return;
        _aiCutting = false;
        int thisHand = _handId;

        CurrentPhase = TrucoPhase.Dealing;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        int cutPosition = _rng.RandiRange(4, _deck.Count - 4);
        var top = _deck.GetRange(0, cutPosition);
        _deck.RemoveRange(0, cutPosition);
        _deck.AddRange(top);
        EmitSignal(SignalName.DeckCut, cutPosition);
        await WaitForAnimation(0.90f);
        if (!IsInsideTree() || _handId != thisHand) return;

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
            if (!CutterIsPlayer)
            {
                _aiPenaThinking = true;
                _aiPenaTimer = Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? 0.45f : 1.90f;
            }
            return;
        }

        PenaRecipientSeatIndex = -1;
        DealAfterCut();
    }

    public void GivePena()
    {
        if (CanOfferPena) DeliverPena();
    }

    private async void DeliverPena()
    {
        if (CurrentPhase != TrucoPhase.PenaDecision || _penaDelivered || _resolvingPena || PenaCard == null) return;
        _aiPenaThinking = false;
        _penaDelivered = true;
        int thisHand = _handId;
        EmitSignal(SignalName.PenaDelivered, PenaCard.ToString(), GetSeatName(PenaRecipientSeatIndex));
        if (PenaDecisionIsBot)
        {
            await WaitForAnimation(1.80f);
            if (!IsInsideTree() || _handId != thisHand || CurrentPhase != TrucoPhase.PenaDecision) return;
            // The vira is not known yet: keep naturally strong cards, occasionally bluff.
            bool keep = (int)PenaCard.Rank >= (int)TrucoRank.Ace || _rng.RandiRange(0, 99) < 24;
            ResolvePenaInternal(keep);
        }
    }

    public void ResolvePena(bool keep)
    {
        if (CanResolvePena || (!keep && CanOfferPena)) ResolvePenaInternal(keep);
    }

    private async void ResolvePenaInternal(bool keep)
    {
        if (CurrentPhase != TrucoPhase.PenaDecision || _resolvingPena) return;
        _resolvingPena = true;
        _aiPenaThinking = false;
        int thisHand = _handId;
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
        await WaitForAnimation(1.30f);
        if (!IsInsideTree() || _handId != thisHand) return;
        DealAfterCut();
    }

    private async void DealAfterCut()
    {
        CurrentPhase = TrucoPhase.Dealing;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
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

        int thisHand = _handId;
        EmitSignal(SignalName.DistributionStarted, distributedCards, PenaRecipientSeatIndex, PenaWasKept);
        // Do not reveal the vira or start the first seat while the final
        // physical card is still travelling across the table.
        await WaitForAnimation(TableStage.DealPresentationDuration(distributedCards));

        if (!IsInsideTree() || _handId != thisHand) return;
        _playedSeats.Clear();
        EmitSignal(SignalName.HandDealt);
        EmitSignal(SignalName.ViraRevealed, ViraCard.ToString(), $"Manilha: {ManilhaRank}");
        await WaitForAnimation(1.85f);
        if (!IsInsideTree() || _handId != thisHand) return;
        _roundLeaderSeat = (DealerSeatIndex + 1) % (TeamSize * 2);
        BeginSeatTurn(_roundLeaderSeat);
    }

    private void BeginSeatTurn(int seat)
    {
        ActiveSeatIndex = seat;
        CurrentPhase = seat == 0 ? TrucoPhase.PlayerTurn : TrucoPhase.OpponentTurn;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        if (seat != 0) StartAIThinking();
    }

    /// <summary>
    /// Player plays a card at the given index from their hand.
    /// </summary>
    public void PlayerPlayCard(int handIndex)
    {
        if (CurrentPhase != TrucoPhase.PlayerTurn || ActiveSeatIndex != 0) return;
        PlaySeatCard(0, handIndex);
    }

    private async void PlaySeatCard(int seat, int handIndex)
    {
        var hand = GetHandForSeat(seat);
        if (_playedSeats.ContainsKey(seat) || handIndex < 0 || handIndex >= hand.Count) return;
        _aiThinking = false;
        int thisHand = _handId;
        CurrentPhase = TrucoPhase.RoundEnd;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        var card = hand[handIndex];
        hand.RemoveAt(handIndex);
        _playedSeats[seat] = card;
        LastPlayedSeatIndex = seat;
        var bestCards = seat % 2 == 0 ? PlayerPlayed : OpponentPlayed;
        if (bestCards[CurrentRound] == null || TrucoCardData.Compare(card, bestCards[CurrentRound], ManilhaRank) > 0)
            bestCards[CurrentRound] = card;
        EmitSignal(SignalName.CardPlayed, seat, card.ToString(), CurrentRound);
        GD.Print($"[Truco] Seat {seat} ({GetSeatName(seat)}) plays: {card}");
        await WaitForAnimation(1.50f);
        if (!IsInsideTree() || _handId != thisHand) return;
        if (_playedSeats.Count == TeamSize * 2) ResolveCurrentRound();
        else BeginSeatTurn((seat + 1) % (TeamSize * 2));
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

    public async void RespondToTruco(bool accept, bool raise)
    {
        if (!_waitingTrucoResponse) return;
        int thisHand = _handId;
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
            EmitSignal(SignalName.TrucoResponded, true, !_trucoPendingByPlayer);
            GD.Print("[Truco] Truco accepted!");
            await WaitForAnimation(0.65f);
            if (!IsInsideTree() || _handId != thisHand) return;
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
            EmitSignal(SignalName.TrucoResponded, false, !_trucoPendingByPlayer);
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

    private async void ResolveCurrentRound()
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

        // Check if hand is decided (best of 3 according to Brazilian Truco rules)
        int playerWins = RoundWinners.Count(w => w == 0);
        int opponentWins = RoundWinners.Count(w => w == 1);
        int ties = RoundWinners.Count(w => w == 2);

        bool handDecided = false;
        bool playerWonHand = false;

        int totalSeats = TeamSize * 2;
        int handInitialLeader = (DealerSeatIndex + 1) % totalSeats;
        bool initialLeaderIsPlayerTeam = (handInitialLeader % 2 == 0);

        if (CurrentRound == 1)
        {
            // After round 2:
            int r0 = RoundWinners[0];
            int r1 = RoundWinners[1];

            if (r0 == 2)
            {
                // Primeira empatou: quem fizer a segunda ganha a mão imediatamente!
                if (r1 == 0) { handDecided = true; playerWonHand = true; }
                else if (r1 == 1) { handDecided = true; playerWonHand = false; }
                // Se r1 também empatou, vai para a terceira rodada.
            }
            else if (r0 == 0)
            {
                // Jogador venceu a primeira
                if (r1 == 0) { handDecided = true; playerWonHand = true; } // 2 a 0
                else if (r1 == 2) { handDecided = true; playerWonHand = true; } // Primeira do jogador, segunda empatada -> jogador leva imediatamente!
                // Se r1 == 1, 1 a 1 -> vai para a terceira rodada.
            }
            else if (r0 == 1)
            {
                // Oponente venceu a primeira
                if (r1 == 1) { handDecided = true; playerWonHand = false; } // 0 a 2
                else if (r1 == 2) { handDecided = true; playerWonHand = false; } // Primeira do oponente, segunda empatada -> oponente leva imediatamente!
                // Se r1 == 0, 1 a 1 -> vai para a terceira rodada.
            }
        }
        else if (CurrentRound >= 2)
        {
            // All 3 rounds played
            handDecided = true;
            if (playerWins > opponentWins) playerWonHand = true;
            else if (opponentWins > playerWins) playerWonHand = false;
            else
            {
                // Ties in 3rd round or all rounds:
                int r0 = RoundWinners[0];
                int r1 = RoundWinners[1];
                int r2 = RoundWinners[2];

                if (r0 != 2)
                {
                    // 1 a 1 e a terceira empatou: quem ganhou a primeira leva!
                    playerWonHand = (r0 == 0);
                }
                else if (r1 != 2)
                {
                    // Primeira empatou, 1 a 1 na segunda e terceira: quem ganhou a segunda leva!
                    playerWonHand = (r1 == 0);
                }
                else if (r2 != 2)
                {
                    // Primeira e segunda empataram: quem ganhou a terceira leva!
                    playerWonHand = (r2 == 0);
                }
                else
                {
                    // Todas as três rodadas empataram: a "mão" (quem começou a primeira vaza) leva!
                    playerWonHand = initialLeaderIsPlayerTeam;
                }
            }
        }

        int thisHand = _handId;
        if (handDecided)
        {
            await WaitForAnimation(1.60f);
            if (!IsInsideTree() || _handId != thisHand) return;
            if (playerWonHand) PlayerScore += CurrentStakes;
            else OpponentScore += CurrentStakes;

            EmitSignal(SignalName.ScoreUpdated, PlayerScore, OpponentScore);
            EndHand(playerWonHand);
        }
        else
        {
            await WaitForAnimation(2.10f);
            if (!IsInsideTree() || _handId != thisHand) return;
            // All seats participate; the seat with the winning card leads next.
            if (winner != 2)
                _roundLeaderSeat = _playedSeats.Where(entry => entry.Key % 2 == winner)
                    .OrderByDescending(entry => entry.Value.GetStrength(ManilhaRank)).First().Key;
            CurrentRound++;
            _playedSeats.Clear();
            BeginSeatTurn(_roundLeaderSeat);
        }
    }

    private void EndHand(bool playerWon)
    {
        FinishHandSequence(playerWon);
    }

    private async void FinishHandSequence(bool playerWon)
    {
        _aiThinking = false;
        CurrentPhase = TrucoPhase.HandEnd;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        int totalSeats = TeamSize * 2;
        DealerSeatIndex = (DealerSeatIndex + 1) % totalSeats;
        EmitSignal(SignalName.HandCleanupStarted, DealerSeatIndex, GetSeatName(DealerSeatIndex));
        int thisHand = _handId;
        await WaitForAnimation(0.9f);
        if (!IsInsideTree() || _handId != thisHand) return;
        EmitSignal(SignalName.HandEnded, playerWon, CurrentStakes);

        if (PlayerScore >= WinScore || OpponentScore >= WinScore)
        {
            CurrentPhase = TrucoPhase.GameOver;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            EmitSignal(SignalName.GameEnded, PlayerScore >= WinScore, PlayerScore, OpponentScore);
            GD.Print($"[Truco] Game Over! Player: {PlayerScore}, Opponent: {OpponentScore}");
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
        _aiThinkTimer = Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? .04f : _rng.RandiRange(20, 32) / 10f;
    }

    private void ExecuteAITurn()
    {
        if (CurrentPhase != TrucoPhase.OpponentTurn || ActiveSeatIndex == 0) return;
        var hand = GetHandForSeat(ActiveSeatIndex);
        if (hand.Count == 0 || _playedSeats.ContainsKey(ActiveSeatIndex)) return;
        int team = ActiveSeatIndex % 2;
        var sorted = hand.OrderBy(card => card.GetStrength(ManilhaRank)).ToList();
        var enemyBest = team == 0 ? OpponentPlayed[CurrentRound] : PlayerPlayed[CurrentRound];
        var allyBest = team == 0 ? PlayerPlayed[CurrentRound] : OpponentPlayed[CurrentRound];
        bool allyWinning = allyBest != null && (enemyBest == null || TrucoCardData.Compare(allyBest, enemyBest, ManilhaRank) > 0);
        var chosen = allyWinning ? sorted.First() : enemyBest != null
            ? sorted.FirstOrDefault(card => TrucoCardData.Compare(card, enemyBest, ManilhaRank) > 0) ?? sorted.First()
            : sorted.Last();

        // In authentic Brazilian Truco, never shout Truco blindly on turn 1 of round 0 before any card is played!
        // The bot only calls Truco when responding to an opponent card on the table, or in Round 1 or 2.
        bool tableHasCards = enemyBest != null || _playedSeats.Count > 0;
        bool canCallTruco = (CurrentRound >= 1 || tableHasCards) && CurrentStakes == 1 && team == 1;
        if (canCallTruco && sorted.Last().GetStrength(ManilhaRank) >= 100 && _rng.RandiRange(0, 99) > 60)
        {
            AICallTruco();
            return;
        }
        PlaySeatCard(ActiveSeatIndex, hand.IndexOf(chosen));
    }

    private async void AICallTruco()
    {
        int newStakes = CurrentStakes switch
        {
            1 => 3, 3 => 6, 6 => 9, 9 => 12, _ => 12
        };
        if (newStakes > 12) { ExecuteAITurn(); return; }

        _opponentAvatar?.SetState(Core.Visuals.AvatarComposite.AnimState.Truco);
        if (Core.Systems.SettingsManager.Instance?.ReduceMotion != true)
        {
            await ToSignal(GetTree().CreateTimer(1.35f), SceneTreeTimer.SignalName.Timeout);
            if (!IsInsideTree()) return;
        }

        CurrentStakes = newStakes;
        _trucoPendingByPlayer = false;
        _waitingTrucoResponse = true;
        // The AI calls before it places its pending card, so it must keep the
        // turn after the player accepts regardless of who started the tombo.
        _phaseAfterTrucoResponse = CurrentPhase;

        CurrentPhase = TrucoPhase.TrucoRequested;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.TrucoCalled, CurrentStakes, false);
        GD.Print($"[Truco] Opponent calls! Stakes: {CurrentStakes}");
    }

    private async void RespondAIToTrucoDelayed()
    {
        int thisHand = _handId;
        float delay = Core.Systems.SettingsManager.Instance?.ReduceMotion == true ? 0.05f : 2.4f;
        await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        if (!IsInsideTree() || _handId != thisHand) return;
        if (_waitingTrucoResponse && _trucoPendingByPlayer)
            AIRespondToTruco();
    }

    private async void AIRespondToTruco()
    {
        int thisHand = _handId;
        // Simple: accept if has any manilha or 3
        bool hasStrong = TeamTwoHands.SelectMany(hand => hand).Any(c => c.GetStrength(ManilhaRank) >= 13);

        if (hasStrong || GD.Randf() > 0.5f)
        {
            // Accept
            _waitingTrucoResponse = false;
            GD.Print("[Truco] AI accepts truco!");
            EmitSignal(SignalName.TrucoResponded, true, false);
            await WaitForAnimation(0.85f);
            if (!IsInsideTree() || _handId != thisHand) return;
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
