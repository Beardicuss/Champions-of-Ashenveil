namespace ChampionsOfAshenveil.Engine;

/// <summary>
/// Converts real-time HoN values to card-game turn-based equivalents.
/// Time quantum T is the number of HoN seconds that map to one game turn.
/// This is the central adaptation rule from plan section 6.
/// </summary>
public static class TimeQuantum
{
    /// <summary>
    /// T = seconds per turn. Default provisional value: 5 seconds.
    /// All conversion formulas below depend on this value.
    /// Set before any match begins; do not change mid-match.
    /// </summary>
    public static float T { get; private set; } = 5.0f;

    public static void SetT(float secondsPerTurn)
    {
        if (secondsPerTurn <= 0) throw new ArgumentOutOfRangeException(nameof(secondsPerTurn));
        T = secondsPerTurn;
    }

    /// <summary>Converts a HoN cooldown in seconds to turns. Rounds up.</summary>
    public static int CooldownToTurns(float cooldownSeconds)
        => (int)MathF.Ceiling(cooldownSeconds / T);

    /// <summary>Converts a HoN cooldown in milliseconds to turns. Rounds up.</summary>
    public static int CooldownMsToTurns(int cooldownMs)
        => CooldownToTurns(cooldownMs / 1000f);

    /// <summary>Converts a HoN effect duration in seconds to turns. Rounds up.</summary>
    public static int DurationToTurns(float durationSeconds)
        => (int)MathF.Ceiling(durationSeconds / T);

    /// <summary>Converts a HoN effect duration in milliseconds to turns. Rounds up.</summary>
    public static int DurationMsToTurns(int durationMs)
        => DurationToTurns(durationMs / 1000f);

    /// <summary>
    /// Converts per-second regeneration (HP or mana) to per-turn regeneration.
    /// regen_per_turn = regen_per_second * T
    /// </summary>
    public static float RegenPerTurn(float regenPerSecond)
        => regenPerSecond * T;
}
