using System.Collections.Generic;

namespace GameHub.Games.PokerRoguelike;

public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

public class CardData
{
    public Suit Suit { get; set; }
    public Rank Rank { get; set; }

    public string GetSuitSymbol()
    {
        return Suit switch
        {
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs => "♣",
            Suit.Spades => "♠",
            _ => "?"
        };
    }

    public string GetRankString()
    {
        return Rank switch
        {
            Rank.Ace => "A",
            Rank.King => "K",
            Rank.Queen => "Q",
            Rank.Jack => "J",
            Rank.Ten => "10",
            _ => ((int)Rank).ToString()
        };
    }

    public override string ToString() => $"{GetRankString()}{GetSuitSymbol()}";

    public static List<CardData> CreateDeck()
    {
        var deck = new List<CardData>();
        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
            {
                deck.Add(new CardData { Suit = suit, Rank = rank });
            }
        }
        return deck;
    }
}
