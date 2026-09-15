using System;
using System.Collections.Generic;
using System.Linq;
using GameHub.Games.Truco;

namespace GameHub.Games.Fodinha;

/// <summary>Four independent seats. No scene, timers or hidden-hand bot access.</summary>
public sealed class FodinhaMatch
{
    public enum Phase { Bidding, Playing, TrickResult, RoundResult, Finished, Cutting }
    public static readonly int[] RoundSizes = { 1, 2, 3, 4, 5, 4, 3, 2, 1 };
    private readonly Random _random;
    private readonly List<TrucoCardData>[] _hands = Enumerable.Range(0, 4).Select(_ => new List<TrucoCardData>()).ToArray();
    private readonly int[] _lives = { 5, 5, 5, 5 }, _bids = new int[4], _wins = new int[4], _losses = new int[4];
    private readonly List<(int Seat, TrucoCardData Card)> _trick = new();
    private List<TrucoCardData> _deck;
    public int DealerSeat { get; private set; }
    public int CutterSeat { get; private set; }
    public Phase State { get; private set; }
    public int RoundIndex { get; private set; }
    public int CardsPerHand => RoundSizes[RoundIndex];
    public int CurrentSeat { get; private set; }
    public int TricksCompleted { get; private set; }
    public int LastWinner { get; private set; } = -1;
    public TrucoCardData Vira { get; private set; }
    public TrucoRank Manilha => TrucoCardData.GetManilhaRank(Vira.Rank);
    public IReadOnlyList<int> Lives => _lives;
    public IReadOnlyList<int> Bids => _bids;
    public IReadOnlyList<int> Wins => _wins;
    public IReadOnlyList<int> Losses => _losses;
    public IReadOnlyList<(int Seat, TrucoCardData Card)> Trick => _trick.AsReadOnly();
    public IReadOnlyList<TrucoCardData> Hand(int seat) => _hands[seat].AsReadOnly();
    public int[] ActiveSeats => Enumerable.Range(0, 4).Where(s => _lives[s] > 0).ToArray();
    public int[] Winners => State != Phase.Finished || _lives.Max() == 0 ? Array.Empty<int>() :
        Enumerable.Range(0, 4).Where(s => _lives[s] == _lives.Max()).ToArray();

    public FodinhaMatch(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        PrepareRound();
    }

    private int NextActive(int seat)
    {
        do { seat = (seat + 1) % 4; } while (_lives[seat] <= 0);
        return seat;
    }

    private void PrepareRound()
    {
        _deck = TrucoCardData.CreateDeck();
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
        foreach (var hand in _hands) hand.Clear();
        Array.Fill(_bids, -1); Array.Clear(_wins); Array.Clear(_losses);
        _trick.Clear(); TricksCompleted = 0; LastWinner = -1;
        Vira = null;
        CutterSeat = DealerSeat;
        do { CutterSeat = (CutterSeat + 3) % 4; } while (_lives[CutterSeat] <= 0);
        CurrentSeat = CutterSeat;
        State = Phase.Cutting;
    }

    public bool Cut(int seat)
    {
        if (State != Phase.Cutting || seat != CutterSeat) return false;
        int cut = _random.Next(4, _deck.Count - 4);
        var top = _deck.GetRange(0, cut);
        _deck.RemoveRange(0, cut); _deck.AddRange(top);
        int cursor = 0;
        for (int card = 0; card < CardsPerHand; card++)
            for (int offset = 1; offset <= 4; offset++)
            {
                int recipient = (DealerSeat + offset) % 4;
                if (_lives[recipient] > 0) _hands[recipient].Add(_deck[cursor++]);
            }
        Vira = _deck[cursor];
        CurrentSeat = NextActive(DealerSeat);
        State = Phase.Bidding;
        return true;
    }

    public bool Bid(int seat, int amount)
    {
        if (State != Phase.Bidding || seat != CurrentSeat || amount < 0 || amount > CardsPerHand) return false;
        _bids[seat] = amount;
        CurrentSeat = NextActive(seat);
        if (ActiveSeats.All(s => _bids[s] >= 0)) State = Phase.Playing;
        return true;
    }

    public bool Play(int seat, int cardIndex)
    {
        if (State != Phase.Playing || seat != CurrentSeat || cardIndex < 0 || cardIndex >= _hands[seat].Count) return false;
        var card = _hands[seat][cardIndex];
        _hands[seat].RemoveAt(cardIndex); _trick.Add((seat, card));
        if (_trick.Count == ActiveSeats.Length)
        {
            LastWinner = ResolveTrick(_trick, Manilha);
            _wins[LastWinner]++; TricksCompleted++;
            CurrentSeat = LastWinner; State = Phase.TrickResult;
        }
        else CurrentSeat = NextActive(seat);
        return true;
    }

    // Equal ordinary ranks go to the first card played; manilhas use suit order.
    public static int ResolveTrick(IReadOnlyList<(int Seat, TrucoCardData Card)> trick, TrucoRank manilha)
    {
        if (trick.Count == 0) throw new ArgumentException("A trick needs at least one card.");
        var best = trick[0];
        foreach (var play in trick.Skip(1))
            if (TrucoCardData.Compare(play.Card, best.Card, manilha) > 0) best = play;
        return best.Seat;
    }

    public static int LifePenalty(int bid, int wins) => Math.Abs(bid - wins);

    public bool AdvanceTrick()
    {
        if (State != Phase.TrickResult) return false;
        _trick.Clear();
        if (TricksCompleted < CardsPerHand) { State = Phase.Playing; return true; }
        foreach (int seat in ActiveSeats)
        {
            _losses[seat] = LifePenalty(_bids[seat], _wins[seat]);
            _lives[seat] = Math.Max(0, _lives[seat] - _losses[seat]);
        }
        State = RoundIndex == RoundSizes.Length - 1 || ActiveSeats.Length <= 1 ? Phase.Finished : Phase.RoundResult;
        return true;
    }

    public bool AdvanceRound()
    {
        if (State != Phase.RoundResult) return false;
        RoundIndex++; DealerSeat = NextActive(DealerSeat); PrepareRound(); return true;
    }
}

public static class FodinhaBot
{
    // Estimate strength using only this seat's hand, the public vira and player count.
    public static int Predict(IReadOnlyList<TrucoCardData> hand, TrucoCardData vira, int players)
    {
        var manilha = TrucoCardData.GetManilhaRank(vira.Rank);
        var known = hand.Select(c => c.ToString()).Append(vira.ToString()).ToHashSet();
        var unseen = TrucoCardData.CreateDeck().Where(c => !known.Contains(c.ToString())).ToArray();
        double expected = hand.Sum(card => Math.Pow((double)unseen.Count(c => TrucoCardData.Compare(card, c, manilha) > 0) / unseen.Length, players - 1));
        return Math.Clamp((int)Math.Round(expected), 0, hand.Count);
    }

    public static int Choose(IReadOnlyList<TrucoCardData> hand, IReadOnlyList<(int Seat, TrucoCardData Card)> trick,
        TrucoRank manilha, int bid, int wins)
    {
        var ordered = Enumerable.Range(0, hand.Count).OrderBy(i => hand[i].GetStrength(manilha)).ToArray();
        int best = trick.Count == 0 ? -1 : trick.Max(p => p.Card.GetStrength(manilha));
        if (wins < bid)
        {
            if (trick.Count == 0) return ordered[^1];
            foreach (int i in ordered) if (hand[i].GetStrength(manilha) > best) return i;
            return ordered[0];
        }
        foreach (int i in ordered.Reverse()) if (hand[i].GetStrength(manilha) <= best) return i;
        return ordered[0];
    }
}
