using System.Collections.Generic;
using System.Linq;

namespace GameHub.Games.Truco;

public enum TrucoSuit
{
    Diamonds, // Ouros  (weakest manilha)
    Spades,   // Espadas
    Hearts,   // Copas
    Clubs     // Paus   (strongest manilha - "zap")
}

public enum TrucoRank
{
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Queen = 8,   // Q
    Jack = 9,    // J
    King = 10,   // K
    Ace = 11,
    Two = 12,
    Three = 13
}

/// <summary>
/// Represents a single Truco card with suit, rank, and manilha-aware comparison.
/// The 40-card Truco deck excludes 8, 9, 10.
/// </summary>
public class TrucoCardData
{
    public TrucoSuit Suit { get; set; }
    public TrucoRank Rank { get; set; }
    public bool IsManilha { get; set; } = false;

    /// <summary>
    /// Gets the effective strength of this card considering manilhas.
    /// Manilhas are stronger than any non-manilha, and among manilhas:
    /// Ouros < Espadas < Copas < Paus (Zap).
    /// </summary>
    public int GetStrength(TrucoRank manilhaRank)
    {
        if (Rank == manilhaRank)
        {
            IsManilha = true;
            // Manilhas are 100+ suit order
            return 100 + (int)Suit;
        }
        IsManilha = false;
        return (int)Rank;
    }

    public string GetRankString()
    {
        return Rank switch
        {
            TrucoRank.Ace => "A",
            TrucoRank.Two => "2",
            TrucoRank.Three => "3",
            TrucoRank.Four => "4",
            TrucoRank.Five => "5",
            TrucoRank.Six => "6",
            TrucoRank.Seven => "7",
            TrucoRank.Queen => "Q",
            TrucoRank.Jack => "J",
            TrucoRank.King => "K",
            _ => "?"
        };
    }

    public string GetSuitSymbol()
    {
        return Suit switch
        {
            TrucoSuit.Hearts => "♥",
            TrucoSuit.Diamonds => "♦",
            TrucoSuit.Clubs => "♣",
            TrucoSuit.Spades => "♠",
            _ => "?"
        };
    }

    public override string ToString() => $"{GetRankString()}{GetSuitSymbol()}";

    /// <summary>
    /// Given the Vira card's rank, determine which rank is the manilha.
    /// The manilha is the rank immediately above the Vira in the sequence:
    /// 4→5→6→7→Q→J→K→A→2→3→(wraps to 4)
    /// </summary>
    public static TrucoRank GetManilhaRank(TrucoRank viraRank)
    {
        return viraRank switch
        {
            TrucoRank.Four => TrucoRank.Five,
            TrucoRank.Five => TrucoRank.Six,
            TrucoRank.Six => TrucoRank.Seven,
            TrucoRank.Seven => TrucoRank.Queen,
            TrucoRank.Queen => TrucoRank.Jack,
            TrucoRank.Jack => TrucoRank.King,
            TrucoRank.King => TrucoRank.Ace,
            TrucoRank.Ace => TrucoRank.Two,
            TrucoRank.Two => TrucoRank.Three,
            TrucoRank.Three => TrucoRank.Four,
            _ => TrucoRank.Four
        };
    }

    /// <summary>
    /// Creates the full 40-card Truco deck.
    /// </summary>
    public static List<TrucoCardData> CreateDeck()
    {
        var deck = new List<TrucoCardData>();
        foreach (TrucoSuit suit in System.Enum.GetValues(typeof(TrucoSuit)))
        {
            foreach (TrucoRank rank in System.Enum.GetValues(typeof(TrucoRank)))
            {
                deck.Add(new TrucoCardData { Suit = suit, Rank = rank });
            }
        }
        return deck;
    }

    /// <summary>
    /// Compare two cards given the manilha rank. Returns positive if card1 wins, negative if card2 wins, 0 for tie.
    /// </summary>
    public static int Compare(TrucoCardData card1, TrucoCardData card2, TrucoRank manilhaRank)
    {
        int s1 = card1.GetStrength(manilhaRank);
        int s2 = card2.GetStrength(manilhaRank);
        return s1.CompareTo(s2);
    }
}
