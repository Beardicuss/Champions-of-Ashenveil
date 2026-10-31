namespace ChampionsOfAshenveil.Engine.Lanes;

/// <summary>One deterministic creep attack phase after both players have acted.</summary>
public static class CreepCombat
{
    public static GameState Resolve(GameState state, SeededRng rng, List<GameEvent> events)
    {
        var attackers = state.Players.SelectMany(p => p.Units)
            .Where(u => !u.IsHero && !u.IsDead).OrderBy(u => u.PlayerId).ThenBy(u => u.Id).ToList();

        foreach (var original in attackers)
        {
            var attacker = state.FindUnit(original.Id);
            if (attacker is null || attacker.IsDead || attacker.IsStunned || attacker.IsDisarmed) continue;
            var targets = state.Players.Where(p => p.PlayerId != attacker.PlayerId)
                .SelectMany(p => p.AliveUnits).ToList();
            var target = targets.OrderBy(u => u.IsHero).ThenBy(u => u.Row == RowPosition.Back)
                .ThenBy(u => u.Id).FirstOrDefault();
            if (target is null) continue;

            int damage = GameEngine.ApplyPhysicalArmor(rng.RollRange(attacker.DamageMin, attacker.DamageMax), target.Armor);
            int hp = Math.Max(0, target.CurrentHp - damage);
            events.Add(new DamageEvent(state.TurnNumber,
                $"{attacker.Id} attacks {target.Id} for {damage} physical damage.",
                attacker.Id, target.Id, damage, DamageType.Physical, hp == 0));
            state = StateUtils.UpdateUnit(state, target with { CurrentHp = hp });
            if (hp == 0)
                events.Add(new UnitDiedEvent(state.TurnNumber, $"{target.Id} has died.", target.Id, target.PlayerId));
        }
        return state;
    }
}
