using Godot;
using System.Collections.Generic;
using System.Linq;

namespace GameHub.Games.PokerRoguelike;

/// <summary>
/// Owns the relics earned during one Poker run.  Relics are deliberately kept
/// independent from UI so the same effects can later be used by LAN and shop
/// flows.
/// </summary>
public partial class RelicManager : Node
{
    public enum RelicId
    {
        AceMultiplier,
        ExtraHand,
        ExtraDiscard
    }

    private readonly List<RelicId> _relics = new();

    public IReadOnlyList<RelicId> ActiveRelics => _relics;

    public void StartRun()
    {
        _relics.Clear();
        // A visible starter relic makes the system immediately understandable.
        AddRelic(RelicId.AceMultiplier);
    }

    public bool AddRelic(RelicId relic)
    {
        if (_relics.Contains(relic)) return false;
        _relics.Add(relic);
        return true;
    }

    public void ApplyHandEffects(HandResult result, IEnumerable<CardData> cards)
    {
        if (_relics.Contains(RelicId.AceMultiplier) && cards.Any(card => card.Rank == Rank.Ace))
            result.Mult += 1;
    }

    public int GetExtraHands() => _relics.Contains(RelicId.ExtraHand) ? 1 : 0;

    public int GetExtraDiscards() => _relics.Contains(RelicId.ExtraDiscard) ? 1 : 0;

    public string GetHudText()
    {
        if (_relics.Count == 0) return "Relíquias: —";

        string symbols = string.Join(" ", _relics.Select(relic => relic switch
        {
            RelicId.AceMultiplier => "♠ Ás +1 Mult",
            RelicId.ExtraHand => "✋ +1 Mão",
            RelicId.ExtraDiscard => "↻ +1 Descarte",
            _ => "?"
        }));
        return $"Relíquias: {symbols}";
    }
}
