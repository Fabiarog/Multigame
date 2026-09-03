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

    // AI fields
    private SyncRng _rng;
    private float _aiThinkTimer = 0f;
    private bool _aiThinking = false;

    public const int WinScore = 12;

    public override void _Ready()
    {
        _rng = new SyncRng(GD.Randi());
        GD.Print("[Truco] Game manager initialized.");
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
        EmitSignal(SignalName.ScoreUpdated, PlayerScore, OpponentScore);
        StartNewHand();
    }

    public void StartNewHand()
    {
        CurrentStakes = 1;
        CurrentRound = 0;
        _waitingTrucoResponse = false;
        for (int i = 0; i < 3; i++)
        {
            PlayerPlayed[i] = null;
            OpponentPlayed[i] = null;
            RoundWinners[i] = -1;
        }

        // Create and shuffle deck
        var deck = TrucoCardData.CreateDeck();
        _rng.ShuffleList(deck);

        // Deal 3 cards to each
        PlayerHand = deck.GetRange(0, 3);
        OpponentHand = deck.GetRange(3, 3);
        ViraCard = deck[6];
        ManilhaRank = TrucoCardData.GetManilhaRank(ViraCard.Rank);

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
    public void PlayerPlayCard(int handIndex)
    {
        if (CurrentPhase != TrucoPhase.PlayerTurn) return;
        if (handIndex < 0 || handIndex >= PlayerHand.Count) return;

        var card = PlayerHand[handIndex];
        PlayerHand.RemoveAt(handIndex);
        PlayerPlayed[CurrentRound] = card;

        EmitSignal(SignalName.CardPlayed, 0, card.ToString(), CurrentRound);
        GD.Print($"[Truco] Player plays: {card}");

        // If opponent already played this round, resolve
        if (OpponentPlayed[CurrentRound] != null)
        {
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

        CurrentPhase = TrucoPhase.TrucoRequested;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.TrucoCalled, CurrentStakes, true);

        GD.Print($"[Truco] Player calls! Stakes: {CurrentStakes}");

        // AI decides whether to accept (simple: accept if has manilha or 3)
        CallDeferred(nameof(AIRespondToTruco));
    }

    public void RespondToTruco(bool accept, bool raise)
    {
        if (!_waitingTrucoResponse) return;
        _waitingTrucoResponse = false;

        if (raise && CurrentStakes < 12)
        {
            // Counter-raise
            CurrentStakes = CurrentStakes switch
            {
                3 => 6,
                6 => 9,
                9 => 12,
                _ => 12
            };
            _trucoPendingByPlayer = false;
            _waitingTrucoResponse = true;
            EmitSignal(SignalName.TrucoCalled, CurrentStakes, false);
            GD.Print($"[Truco] Opponent raises! Stakes: {CurrentStakes}");
            // Now player needs to respond — show overlay handled by UI
            CurrentPhase = TrucoPhase.TrucoRequested;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        }
        else if (accept)
        {
            GD.Print("[Truco] Truco accepted!");
            // Return to whoever's turn
            CurrentPhase = _trucoPendingByPlayer ? TrucoPhase.OpponentTurn : TrucoPhase.PlayerTurn;
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

    // ===== AI =====

    private void StartAIThinking()
    {
        _aiThinking = true;
        _aiThinkTimer = (float)GD.RandRange(0.8, 2.0);
    }

    private void ExecuteAITurn()
    {
        if (OpponentHand.Count == 0) return;

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
        OpponentPlayed[CurrentRound] = chosen;

        EmitSignal(SignalName.CardPlayed, 1, chosen.ToString(), CurrentRound);
        GD.Print($"[Truco] Opponent plays: {chosen}");

        // If player already played, resolve
        if (PlayerPlayed[CurrentRound] != null)
        {
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

        CurrentPhase = TrucoPhase.TrucoRequested;
        EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
        EmitSignal(SignalName.TrucoCalled, CurrentStakes, false);
        GD.Print($"[Truco] Opponent calls! Stakes: {CurrentStakes}");
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
            CurrentPhase = TrucoPhase.OpponentTurn;
            EmitSignal(SignalName.PhaseChanged, (int)CurrentPhase);
            StartAIThinking();
        }
        else
        {
            // Decline
            RespondToTruco(false, false);
        }
    }
}
