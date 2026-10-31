using ChampionsOfAshenveil.Engine.Data;
using ChampionsOfAshenveil.Engine.Progression;

namespace ChampionsOfAshenveil.Engine.Abilities;

/// <summary>
/// Entry point: reads targeting type and routes to AbilityTargeting.
/// Contains no logic itself — pure dispatch.
/// </summary>
public static class AbilityExecutor
{
    public static (GameState state, List<GameEvent> events) Execute(
        GameState state, UseAbilityAction action, DataRegistry data, SeededRng rng)
    {
        var events = new List<GameEvent>();
        if (!data.Abilities.TryGetValue(action.AbilityId, out var def))
            return (state, events);

        var caster = state.FindUnit(action.CasterId)!;
        int rank = caster.AbilityRanks.GetValueOrDefault(action.AbilityId);
        if (rank <= 0) return (state, events);
        def = new AbilityDefinition
        {
            Id = def.Id, Name = def.Name, Description = def.Description,
            Source = def.Source, Original = def.Original, EffectIds = def.EffectIds,
            Adapted = new AbilityAdapted
            {
                CooldownTurns = def.Adapted.CooldownTurns,
                EnergyCost = def.Adapted.EnergyCost,
                Damage = HeroProgression.ScaledDamage((int)def.Adapted.Damage, rank),
                DurationTurns = def.Adapted.DurationTurns,
                Targeting = def.Adapted.Targeting,
                AdaptationReason = def.Adapted.AdaptationReason
            }
        };
        var target = action.TargetId is not null ? state.FindUnit(action.TargetId) : null;

        state = def.Adapted.Targeting switch
        {
            "self"        => AbilityTargeting.ExecuteSelf(state, action, def, caster, data, events),
            "self_area"   => AbilityTargeting.ExecuteSelfArea(state, action, def, caster, data, rng, events),
            "single"      => AbilityTargeting.ExecuteSingle(state, action, def, caster, target, data, rng, events),
            "single_ally" => AbilityTargeting.ExecuteSingleAlly(state, action, def, caster, target, data, events),
            "area"        => AbilityTargeting.ExecuteArea(state, action, def, caster, data, rng, events),
            _             => state
        };

        return (state, events);
    }
}
