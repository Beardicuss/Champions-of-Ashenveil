namespace ChampionsOfAshenveil.Engine.Effects;

/// <summary>HoN potion states expire on positive damage from non-creep units.</summary>
public static class InterruptedEffects
{
    public static GameState AfterDamage(GameState before, GameState after, DataRegistry data, List<GameEvent> events)
    {
        foreach (var hit in events.OfType<DamageEvent>().Where(e => e.Amount > 0).ToList())
        {
            var source = before.FindUnit(hit.AttackerId);
            if (source is null || !source.IsHero) continue;
            var target = after.FindUnit(hit.TargetId);
            if (target is null) continue;
            var removed = target.Effects.Where(ae =>
                data.Effects.TryGetValue(ae.EffectId, out var definition) && definition.InterruptOnHeroDamage).ToList();
            if (removed.Count == 0) continue;
            after = StateUtils.UpdateUnit(after, target with
            {
                Effects = target.Effects.Except(removed).ToList()
            });
            foreach (var effect in removed)
                events.Add(new EffectExpiredEvent(after.TurnNumber,
                    $"{effect.EffectId} on {target.Id} was interrupted by damage.", target.Id, effect.EffectId));
        }
        return after;
    }
}
