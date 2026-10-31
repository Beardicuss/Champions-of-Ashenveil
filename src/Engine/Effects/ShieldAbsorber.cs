using ChampionsOfAshenveil.Engine.Data;

namespace ChampionsOfAshenveil.Engine.Effects;

/// <summary>
/// Handles Shield effect damage absorption.
/// Shield HP is stored in ActiveEffect.StackCount at apply time.
/// </summary>
public static class ShieldAbsorber
{
    public static (int remainingDamage, UnitState unit, List<GameEvent> events) Absorb(
        int incomingDamage, UnitState target, DataRegistry data, int turn)
    {
        var events = new List<GameEvent>();

        var shieldEffect = target.Effects.FirstOrDefault(ae =>
            data.Effects.GetValueOrDefault(ae.EffectId)?.StatusFlags
                .Contains("Shield", StringComparer.OrdinalIgnoreCase) == true);

        if (shieldEffect is null) return (incomingDamage, target, events);

        var shieldDef = data.Effects.GetValueOrDefault(shieldEffect.EffectId)!;
        int shieldHp  = shieldEffect.StackCount;
        int absorbed  = Math.Min(incomingDamage, shieldHp);
        shieldHp     -= absorbed;

        events.Add(new DamageEvent(turn,
            $"{shieldDef.Name} on {target.Id} absorbed {absorbed} damage ({shieldHp} HP left).",
            "", target.Id, absorbed, DamageType.True, false));

        List<ActiveEffect> newEffects;
        if (shieldHp <= 0)
        {
            newEffects = target.Effects.Where(e => e.EffectId != shieldEffect.EffectId).ToList();
            events.Add(new EffectExpiredEvent(turn,
                $"{shieldDef.Name} on {target.Id} was destroyed!", target.Id, shieldEffect.EffectId));
            // Phase 2: trigger explosion event
        }
        else
        {
            newEffects = target.Effects
                .Select(e => e.EffectId == shieldEffect.EffectId ? e with { StackCount = shieldHp } : e)
                .ToList();
        }

        return (incomingDamage - absorbed, target with { Effects = newEffects }, events);
    }
}
