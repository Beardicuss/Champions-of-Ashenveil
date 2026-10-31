using ChampionsOfAshenveil.Engine.Data;
using ChampionsOfAshenveil.Engine.Effects;

namespace ChampionsOfAshenveil.Engine.Abilities;

/// <summary>
/// Applies EffectDefinitions to a target unit on behalf of an ability.
/// </summary>
public static class AbilityEffectApply
{
    public static GameState ApplyEffectsToUnit(
        GameState state, string sourceId, string targetId,
        AbilityDefinition abilityDef, DataRegistry data, int turn, List<GameEvent> events)
    {
        var target = state.FindUnit(targetId);
        if (target is null) return state;

        foreach (var effectId in abilityDef.EffectIds)
        {
            if (!data.Effects.TryGetValue(effectId, out var effectDef)) continue;

            // Shield HP passed as stackCount (level-1 value)
            int stackCount = effectDef.StatusFlags.Contains("Shield", StringComparer.OrdinalIgnoreCase) ? 110 : 1;

            var (updated, applyEvents) = EffectApplicator.Apply(target, effectDef, turn, sourceId, stackCount);
            target = updated;
            events.AddRange(applyEvents);
        }

        return StateUtils.UpdateUnit(state, target);
    }
}
