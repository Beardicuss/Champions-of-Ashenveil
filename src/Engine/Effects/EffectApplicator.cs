using ChampionsOfAshenveil.Engine.Data;

namespace ChampionsOfAshenveil.Engine.Effects;

/// <summary>
/// Applies and dispels ActiveEffects on units.
/// Handles stacking and refresh rules from EffectDefinition.
/// </summary>
public static class EffectApplicator
{
    public static (UnitState unit, List<GameEvent> events) Apply(
        UnitState        target,
        EffectDefinition def,
        int              turn,
        string           sourceUnitId,
        int              stackCount = 1)
    {
        var events = new List<GameEvent>();

        if (EffectQuery.IsImmune(target, def))
        {
            events.Add(new InvalidActionEvent(turn, $"{target.Id} is immune to {def.Name}.", "Effect immunity."));
            return (target, events);
        }

        var existing = target.Effects.FirstOrDefault(e => e.EffectId == def.Id);
        List<ActiveEffect> newEffects;

        if (existing is not null)
        {
            if (!def.CanStack && def.Refreshes)
            {
                newEffects = target.Effects
                    .Select(e => e.EffectId == def.Id ? e with { TurnsRemaining = def.DurationTurns } : e)
                    .ToList();
                events.Add(new EffectAppliedEvent(turn,
                    $"{def.Name} refreshed on {target.Id}.", sourceUnitId, target.Id, def.Id));
            }
            else if (def.CanStack)
            {
                newEffects = [.. target.Effects, new ActiveEffect(def.Id, def.DurationTurns, stackCount, sourceUnitId, def.StatusFlags)];
                events.Add(new EffectAppliedEvent(turn,
                    $"{def.Name} stacked on {target.Id} (×{existing.StackCount + 1}).", sourceUnitId, target.Id, def.Id));
            }
            else
            {
                return (target, events); // no-stack, no-refresh: ignore
            }
        }
        else
        {
            newEffects = [.. target.Effects, new ActiveEffect(def.Id, def.DurationTurns, stackCount, sourceUnitId, def.StatusFlags)];
            events.Add(new EffectAppliedEvent(turn,
                $"{def.Name} applied to {target.Id} for {def.DurationTurns} turn(s).", sourceUnitId, target.Id, def.Id));
        }

        return (target with { Effects = newEffects }, events);
    }

    public static (UnitState unit, List<GameEvent> events) DispelAll(
        UnitState target, DataRegistry data, int turn)
    {
        var events = new List<GameEvent>();
        var kept   = new List<ActiveEffect>();

        foreach (var ae in target.Effects)
        {
            var def = data.Effects.GetValueOrDefault(ae.EffectId);
            if (def is not null && def.Dispellable)
                events.Add(new EffectExpiredEvent(turn, $"{ae.EffectId} dispelled from {target.Id}.", target.Id, ae.EffectId));
            else
                kept.Add(ae);
        }

        return (target with { Effects = kept }, events);
    }
}
