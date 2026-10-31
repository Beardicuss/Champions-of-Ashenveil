using ChampionsOfAshenveil.Engine.Effects;

namespace ChampionsOfAshenveil.Engine.Abilities;

/// <summary>
/// Physical and magic damage delivery for abilities.
/// Applies shield absorption then armor mitigation before updating HP.
/// </summary>
public static class AbilityDamage
{
    public static GameState DealPhysical(
        GameState state, string sourceId, string targetId,
        int rawDamage, DataRegistry data, int turn, List<GameEvent> events)
    {
        var target = state.FindUnit(targetId);
        if (target is null || target.IsDead) return state;

        var (afterShield, shielded, shieldEvents) = ShieldAbsorber.Absorb(rawDamage, target, data, turn);
        state = StateUtils.UpdateUnit(state, shielded);
        events.AddRange(shieldEvents);
        if (afterShield <= 0) return state;

        int mitigated = GameEngine.ApplyPhysicalArmor(afterShield, shielded.Armor);
        return Apply(state, sourceId, targetId, mitigated, DamageType.Physical, turn, events);
    }

    public static GameState DealMagic(
        GameState state, string sourceId, string targetId,
        int rawDamage, DataRegistry data, int turn, List<GameEvent> events)
    {
        var target = state.FindUnit(targetId);
        if (target is null || target.IsDead) return state;

        var (afterShield, shielded, shieldEvents) = ShieldAbsorber.Absorb(rawDamage, target, data, turn);
        state = StateUtils.UpdateUnit(state, shielded);
        events.AddRange(shieldEvents);
        if (afterShield <= 0) return state;

        int mitigated = GameEngine.ApplyMagicArmor(afterShield, shielded.MagicArmor);
        return Apply(state, sourceId, targetId, mitigated, DamageType.Magic, turn, events);
    }

    private static GameState Apply(
        GameState state, string sourceId, string targetId,
        int finalDamage, DamageType type, int turn, List<GameEvent> events)
    {
        var target  = state.FindUnit(targetId)!;
        int newHp   = Math.Max(0, target.CurrentHp - finalDamage);
        bool lethal = newHp == 0;

        events.Add(new DamageEvent(turn,
            $"{targetId} takes {finalDamage} {type} ability damage (→ {newHp} HP).",
            sourceId, targetId, finalDamage, type, lethal));

        if (lethal)
            events.Add(new UnitDiedEvent(turn, $"{targetId} has died.", targetId, target.PlayerId));

        return StateUtils.UpdateUnit(state, target with { CurrentHp = newHp });
    }
}
