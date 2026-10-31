using ChampionsOfAshenveil.Engine.Data;

namespace ChampionsOfAshenveil.Engine.Effects;

/// <summary>
/// Ticks all active effects on a unit at turn start.
/// Decrements duration and applies DoT damage using the HoN armor formulas.
/// </summary>
public static class EffectTicker
{
    public static (UnitState unit, List<GameEvent> events) Tick(
        UnitState target, DataRegistry data, int turn, SeededRng rng)
    {
        var events    = new List<GameEvent>();
        var remaining = new List<ActiveEffect>();
        int newHp     = target.CurrentHp;

        foreach (var ae in target.Effects)
        {
            var def = data.Effects.GetValueOrDefault(ae.EffectId);

            if (def is not null && def.DamagePerTurn > 0 && !target.IsDead)
            {
                int dot = (int)MathF.Round(def.DamagePerTurn);
                int mitigated = def.DamageType.Equals("Physical", StringComparison.OrdinalIgnoreCase)
                    ? GameEngine.ApplyPhysicalArmor(dot, target.Armor)
                    : def.DamageType.Equals("Magic", StringComparison.OrdinalIgnoreCase)
                        ? GameEngine.ApplyMagicArmor(dot, target.MagicArmor)
                        : dot;

                newHp -= mitigated;
                bool lethal = newHp <= 0;
                events.Add(new DamageEvent(turn,
                    $"{target.Id} takes {mitigated} {def.DamageType} DoT from {def.Name}.",
                    ae.SourceUnitId, target.Id, mitigated,
                    Enum.TryParse<DamageType>(def.DamageType, true, out var dt) ? dt : DamageType.True,
                    lethal));

                if (lethal)
                    events.Add(new UnitDiedEvent(turn, $"{target.Id} died from {def.Name}.", target.Id, target.PlayerId));
            }

            int turnsLeft = ae.TurnsRemaining - 1;
            if (turnsLeft > 0)
                remaining.Add(ae with { TurnsRemaining = turnsLeft });
            else
                events.Add(new EffectExpiredEvent(turn, $"{ae.EffectId} expired on {target.Id}.", target.Id, ae.EffectId));
        }

        return (target with { CurrentHp = Math.Max(0, newHp), Effects = remaining }, events);
    }
}
