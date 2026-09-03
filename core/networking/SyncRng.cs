using Godot;

namespace GameHub.Core.Networking;

/// <summary>
/// Deterministic synchronized RNG system.
/// The Host generates a MatchSeed and shares it with all clients.
/// All random gameplay events (card draws, shop rolls, etc.) derive from this seed
/// to guarantee identical results across all machines.
/// </summary>
public class SyncRng
{
    private RandomNumberGenerator _rng;
    private uint _baseSeed;

    public uint Seed => _baseSeed;

    public SyncRng(uint seed)
    {
        _baseSeed = seed;
        _rng = new RandomNumberGenerator();
        _rng.Seed = seed;
    }

    /// <summary>
    /// Create a child RNG for a specific round, preserving the parent seed.
    /// This allows per-round determinism.
    /// </summary>
    public SyncRng CreateChildForRound(int roundNumber)
    {
        // Combine the base seed with the round number to get a unique but deterministic child seed
        uint childSeed = (uint)(_baseSeed ^ (uint)(roundNumber * 2654435761));
        return new SyncRng(childSeed);
    }

    /// <summary>
    /// Create a child RNG for a specific player action within a round.
    /// </summary>
    public SyncRng CreateChildForPlayer(long playerId)
    {
        uint childSeed = (uint)(_baseSeed ^ (uint)(playerId * 2246822519));
        return new SyncRng(childSeed);
    }

    public int RandiRange(int min, int max) => _rng.RandiRange(min, max);
    public float RandfRange(float min, float max) => _rng.RandfRange(min, max);
    public uint Randi() => _rng.Randi();
    public float Randf() => _rng.Randf();

    /// <summary>
    /// Shuffle an array in-place deterministically using Fisher-Yates.
    /// </summary>
    public void Shuffle<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = RandiRange(0, i);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    /// <summary>
    /// Shuffle a Godot Array in-place deterministically.
    /// </summary>
    public void ShuffleList<T>(System.Collections.Generic.List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = RandiRange(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
