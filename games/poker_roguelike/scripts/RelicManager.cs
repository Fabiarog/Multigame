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
        ExtraDiscard,
        JokerClassic,
        FlushBoost,
        PairMaster,
        GoldenTicket,
        ChaosDice
    }

    public const int MaxJokers = 5;
    private readonly List<RelicId> _relics = new();

    public IReadOnlyList<RelicId> ActiveRelics => _relics;
    public int Count => _relics.Count;
    public bool CanAddRelic => _relics.Count < MaxJokers;

    public void StartRun()
    {
        _relics.Clear();
        // A visible starter relic makes the system immediately understandable.
        AddRelic(RelicId.AceMultiplier);
    }

    public bool AddRelic(RelicId relic)
    {
        if (_relics.Count >= MaxJokers || _relics.Contains(relic)) return false;
        _relics.Add(relic);
        return true;
    }

    public void RemoveRelic(RelicId relic)
    {
        _relics.Remove(relic);
    }

    public void ApplyHandEffects(HandResult result, IEnumerable<CardData> cards)
    {
        if (_relics.Contains(RelicId.AceMultiplier) && cards.Any(card => card.Rank == Rank.Ace))
            result.Mult += 2;

        if (_relics.Contains(RelicId.JokerClassic))
            result.Mult += 4;

        if (_relics.Contains(RelicId.FlushBoost) && result.Type == HandType.Flush)
            result.CardChips += 30;

        if (_relics.Contains(RelicId.PairMaster) && (result.Type == HandType.Pair || result.Type == HandType.TwoPair))
        {
            result.CardChips += 20;
            result.Mult += 1;
        }

        if (_relics.Contains(RelicId.ChaosDice))
        {
            int dice = (int)(GD.Randi() % 6) + 1;
            result.Mult += dice;
        }
    }

    public int GetExtraHands() => _relics.Contains(RelicId.ExtraHand) ? 1 : 0;

    public int GetExtraDiscards() => _relics.Contains(RelicId.ExtraDiscard) ? 1 : 0;

    public int GetEndRoundBonusGold() => _relics.Contains(RelicId.GoldenTicket) ? 3 : 0;

    public static string GetJokerName(RelicId id) => id switch
    {
        RelicId.AceMultiplier => "Ás Dourado",
        RelicId.ExtraHand     => "Mão Adicional",
        RelicId.ExtraDiscard  => "Descarte Extra",
        RelicId.JokerClassic  => "Coringa Clássico",
        RelicId.FlushBoost    => "Mancha d'Água",
        RelicId.PairMaster    => "Par Perfeito",
        RelicId.GoldenTicket  => "Bilhete de Ouro",
        RelicId.ChaosDice     => "Dados do Caos",
        _ => "Relíquia"
    };

    public static string GetJokerDesc(RelicId id) => id switch
    {
        RelicId.AceMultiplier => "Cada Ás jogado na mão concede +2 Mult.",
        RelicId.ExtraHand     => "+1 jogada de mão em cada rodada.",
        RelicId.ExtraDiscard  => "+1 descarte extra em cada rodada.",
        RelicId.JokerClassic  => "+4 Mult para toda combinação jogada.",
        RelicId.FlushBoost    => "+30 Fichas quando jogar Flush (Cor).",
        RelicId.PairMaster    => "+20 Fichas e +1 Mult para Pares.",
        RelicId.GoldenTicket  => "+3 Fichas de bônus no fim de cada rodada.",
        RelicId.ChaosDice     => "Concede entre +1 e +6 Mult aleatório por mão.",
        _ => "Bônus especial"
    };

    public static string GetJokerIcon(RelicId id) => id switch
    {
        RelicId.AceMultiplier => "🂡",
        RelicId.ExtraHand     => "✋",
        RelicId.ExtraDiscard  => "↻",
        RelicId.JokerClassic  => "🃏",
        RelicId.FlushBoost    => "🌊",
        RelicId.PairMaster    => "✌",
        RelicId.GoldenTicket  => "🎫",
        RelicId.ChaosDice     => "🎲",
        _ => "★"
    };

    public static int GetJokerPrice(RelicId id) => id switch
    {
        RelicId.JokerClassic  => 4,
        RelicId.AceMultiplier => 5,
        RelicId.FlushBoost    => 6,
        RelicId.PairMaster    => 5,
        RelicId.ExtraDiscard  => 6,
        RelicId.ExtraHand     => 8,
        RelicId.GoldenTicket  => 7,
        RelicId.ChaosDice     => 6,
        _ => 5
    };

    public string GetHudText()
    {
        if (_relics.Count == 0) return "Coringas: 0 / 5";
        return $"Coringas ({_relics.Count}/5): " + string.Join("  ", _relics.Select(r => $"{GetJokerIcon(r)} {GetJokerName(r)}"));
    }
}
