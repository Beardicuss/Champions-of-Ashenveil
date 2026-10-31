namespace ChampionsOfAshenveil.Engine;

/// <summary>
/// Deterministic seeded RNG.
/// Uses xorshift64 — fast, simple, fully reproducible.
/// Matches are replayable from (initialState, seed, actionList). (Plan section 7.)
/// </summary>
public class SeededRng
{
    private ulong _state;

    public ulong State => _state;

    public SeededRng(ulong seed)
    {
        _state = seed == 0 ? 1UL : seed; // xorshift requires non-zero state
    }

    /// <summary>Advance the RNG and return the next raw value.</summary>
    private ulong Next()
    {
        _state ^= _state << 13;
        _state ^= _state >> 7;
        _state ^= _state << 17;
        return _state;
    }

    /// <summary>Returns a value in [min, max] inclusive.</summary>
    public int RollRange(int min, int max)
    {
        if (min == max) return min;
        if (min > max) (min, max) = (max, min);
        return (int)(Next() % (ulong)(max - min + 1)) + min;
    }

    /// <summary>Returns true with the given probability (0.0–1.0).</summary>
    public bool RollChance(float probability)
    {
        return (Next() % 10000UL) < (ulong)(probability * 10000f);
    }
}
