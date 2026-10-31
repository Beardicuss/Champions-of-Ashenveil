using ChampionsOfAshenveil.Engine.Data;
using ChampionsOfAshenveil.Engine.Effects;

namespace ChampionsOfAshenveil.Engine.Abilities;

/// <summary>
/// Handles each targeting type for ability execution.
/// Reads AbilityDefinition.Adapted.Targeting and routes to the right logic.
/// </summary>
public static class AbilityTargeting
{
    public static GameState ExecuteSelf(
        GameState state, UseAbilityAction action, AbilityDefinition def,
        UnitState caster, DataRegistry data, List<GameEvent> events) =>
        AbilityEffectApply.ApplyEffectsToUnit(state, caster.Id, caster.Id, def, data, state.TurnNumber, events);

    public static GameState ExecuteSelfArea(
        GameState state, UseAbilityAction action, AbilityDefinition def,
        UnitState caster, DataRegistry data, SeededRng rng, List<GameEvent> events)
    {
        state = AbilityEffectApply.ApplyEffectsToUnit(state, caster.Id, caster.Id, def, data, state.TurnNumber, events);

        if (def.Adapted.Damage > 0)
            foreach (var enemy in EnemyUnits(state, caster.PlayerId))
                state = AbilityDamage.DealMagic(state, caster.Id, enemy.Id, (int)def.Adapted.Damage, data, state.TurnNumber, events);

        return state;
    }

    public static GameState ExecuteSingle(
        GameState state, UseAbilityAction action, AbilityDefinition def,
        UnitState caster, UnitState? target, DataRegistry data, SeededRng rng, List<GameEvent> events)
    {
        if (target is null || target.IsDead) return state;

        if (def.Adapted.Damage > 0)
            state = AbilityDamage.DealPhysical(state, caster.Id, target.Id, (int)def.Adapted.Damage, data, state.TurnNumber, events);

        return AbilityEffectApply.ApplyEffectsToUnit(state, caster.Id, target.Id, def, data, state.TurnNumber, events);
    }

    public static GameState ExecuteSingleAlly(
        GameState state, UseAbilityAction action, AbilityDefinition def,
        UnitState caster, UnitState? target, DataRegistry data, List<GameEvent> events)
    {
        var allyId = target?.Id ?? caster.Id;
        var ally   = state.FindUnit(allyId);
        if (ally is null || ally.IsDead) return state;

        if (def.Adapted.Damage > 0)
            state = AbilityHeal.Heal(state, caster.Id, allyId, (int)def.Adapted.Damage, state.TurnNumber, events);

        // Dispel before applying shield/buff
        bool hasShield = def.EffectIds.Any(eid =>
            data.Effects.GetValueOrDefault(eid)?.StatusFlags
                .Contains("Shield", StringComparer.OrdinalIgnoreCase) == true);
        if (hasShield)
        {
            var refreshedAlly = state.FindUnit(allyId)!;
            var (dispelled, dispelEvents) = EffectApplicator.DispelAll(refreshedAlly, data, state.TurnNumber);
            state = StateUtils.UpdateUnit(state, dispelled);
            events.AddRange(dispelEvents);
        }

        return AbilityEffectApply.ApplyEffectsToUnit(state, caster.Id, allyId, def, data, state.TurnNumber, events);
    }

    public static GameState ExecuteArea(
        GameState state, UseAbilityAction action, AbilityDefinition def,
        UnitState caster, DataRegistry data, SeededRng rng, List<GameEvent> events)
    {
        foreach (var enemy in EnemyUnits(state, caster.PlayerId))
        {
            if (def.Adapted.Damage > 0)
                state = AbilityDamage.DealPhysical(state, caster.Id, enemy.Id, (int)def.Adapted.Damage, data, state.TurnNumber, events);
            state = AbilityEffectApply.ApplyEffectsToUnit(state, caster.Id, enemy.Id, def, data, state.TurnNumber, events);
        }
        return state;
    }

    private static IEnumerable<UnitState> EnemyUnits(GameState state, int playerId) =>
        state.Players.Where(p => p.PlayerId != playerId).SelectMany(p => p.AliveUnits);
}
