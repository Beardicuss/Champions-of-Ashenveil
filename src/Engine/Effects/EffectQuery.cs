using ChampionsOfAshenveil.Engine.Data;

namespace ChampionsOfAshenveil.Engine.Effects;

/// <summary>
/// Queries active effects on a unit for status flags and immunity checks.
/// </summary>
public static class EffectQuery
{
    public static bool HasStatus(UnitState unit, string statusFlag, DataRegistry data) =>
        unit.Effects.Any(ae =>
            data.Effects.GetValueOrDefault(ae.EffectId)
                ?.StatusFlags.Contains(statusFlag, StringComparer.OrdinalIgnoreCase) == true);

    /// <summary>
    /// Phase 1: immunity check not yet loading extension fields from JSON.
    /// Phase 2: read immune_flags array from effect JSON and match against incoming flags.
    /// </summary>
    public static bool IsImmune(UnitState unit, EffectDefinition incoming) => false;
}
