using System.Collections.Generic;
using System.Linq;

namespace GameHub.Games.PokerRoguelike;

/// <summary>
/// The type of poker hand, ordered from weakest to strongest.
/// </summary>
public enum HandType
{
    HighCard,
    Pair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
    RoyalFlush
}

/// <summary>
/// Result of evaluating a poker hand. Uses Balatro-inspired Chips × Mult scoring.
/// </summary>
public class HandResult
{
    public HandType Type { get; set; }
    public string HandName { get; set; } = "";
    public int BaseChips { get; set; }
    public int Mult { get; set; }
    public int CardChips { get; set; }
    public int TotalScore => (BaseChips + CardChips) * Mult;

    public string GetScoreBreakdown()
    {
        return $"({BaseChips} + {CardChips}) × {Mult} = {TotalScore}";
    }
}

/// <summary>
/// Evaluates poker hands (1–5 cards) and returns scored results.
/// Scoring is Balatro-inspired: each hand type provides base chips + multiplier,
/// and each card in the hand adds its rank value as chips.
/// </summary>
public static class HandEvaluator
{
    private static readonly Dictionary<HandType, (string Name, int Chips, int Mult)> HandValues = new()
    {
        { HandType.HighCard,       ("Carta Alta",      5,   1) },
        { HandType.Pair,           ("Par",             10,  2) },
        { HandType.TwoPair,        ("Dois Pares",      20,  2) },
        { HandType.ThreeOfAKind,   ("Trinca",          30,  3) },
        { HandType.Straight,       ("Sequência",       30,  4) },
        { HandType.Flush,          ("Flush",           35,  4) },
        { HandType.FullHouse,      ("Full House",      40,  4) },
        { HandType.FourOfAKind,    ("Quadra",          60,  7) },
        { HandType.StraightFlush,  ("Straight Flush",  100, 8) },
        { HandType.RoyalFlush,     ("Royal Flush",     100, 8) },
    };

    public static HandResult Evaluate(List<CardData> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            return new HandResult
            {
                Type = HandType.HighCard,
                HandName = "Nenhuma",
                BaseChips = 0,
                Mult = 0,
                CardChips = 0
            };
        }

        int cardChips = cards.Sum(c => GetCardChipValue(c));
        HandType type = DetermineHandType(cards);
        var (name, chips, mult) = HandValues[type];

        return new HandResult
        {
            Type = type,
            HandName = name,
            BaseChips = chips,
            Mult = mult,
            CardChips = cardChips
        };
    }

    /// <summary>
    /// Each card contributes its rank as chip value. Face cards = 10, Ace = 11.
    /// </summary>
    private static int GetCardChipValue(CardData card)
    {
        return card.Rank switch
        {
            Rank.Ace => 11,
            Rank.King or Rank.Queen or Rank.Jack => 10,
            _ => (int)card.Rank
        };
    }

    private static HandType DetermineHandType(List<CardData> cards)
    {
        if (cards.Count < 2) return HandType.HighCard;

        var rankGroups = cards.GroupBy(c => c.Rank)
                              .OrderByDescending(g => g.Count())
                              .ThenByDescending(g => g.Key)
                              .ToList();

        bool isFlush = cards.Count >= 5 && cards.All(c => c.Suit == cards[0].Suit);
        bool isStraight = cards.Count >= 5 && IsStraight(cards);

        // Straight Flush / Royal Flush (requires 5 cards)
        if (isFlush && isStraight)
        {
            var maxRank = cards.Max(c => c.Rank);
            return maxRank == Rank.Ace ? HandType.RoyalFlush : HandType.StraightFlush;
        }

        int maxGroupSize = rankGroups[0].Count();
        int pairCount = rankGroups.Count(g => g.Count() >= 2);

        // Four of a Kind
        if (maxGroupSize >= 4) return HandType.FourOfAKind;

        // Full House (three + pair)
        if (maxGroupSize >= 3 && pairCount >= 2) return HandType.FullHouse;

        // Flush (5 cards, same suit)
        if (isFlush) return HandType.Flush;

        // Straight (5 consecutive ranks)
        if (isStraight) return HandType.Straight;

        // Three of a Kind
        if (maxGroupSize >= 3) return HandType.ThreeOfAKind;

        // Two Pair
        if (pairCount >= 2) return HandType.TwoPair;

        // Pair
        if (maxGroupSize >= 2) return HandType.Pair;

        return HandType.HighCard;
    }

    private static bool IsStraight(List<CardData> cards)
    {
        var ranks = cards.Select(c => (int)c.Rank).Distinct().OrderBy(r => r).ToList();
        if (ranks.Count < 5) return false;

        // Normal straight: highest - lowest == 4
        if (ranks[ranks.Count - 1] - ranks[0] == 4) return true;

        // Ace-low straight: A-2-3-4-5
        if (ranks.Contains(14) && ranks.Contains(2) && ranks.Contains(3)
            && ranks.Contains(4) && ranks.Contains(5))
        {
            return true;
        }

        return false;
    }
}
