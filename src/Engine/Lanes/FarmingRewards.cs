namespace ChampionsOfAshenveil.Engine.Lanes;
using ChampionsOfAshenveil.Engine.Progression;

/// <summary>Credits last hits by Heroes; creep-on-creep kills grant no gold.</summary>
public static class FarmingRewards
{
    public const int CreepGold = 75;
    public const int HeroGold = 200;

    public static GameState Apply(GameState before, GameState after, List<GameEvent> events)
    {
        foreach (var death in events.OfType<UnitDiedEvent>().ToList())
        {
            var victim = before.FindUnit(death.UnitId);
            var hit = events.OfType<DamageEvent>().LastOrDefault(e => e.TargetId == death.UnitId);
            var attacker = hit is null ? null : before.FindUnit(hit.AttackerId);
            if (victim is null || attacker is null || !attacker.IsHero || attacker.PlayerId == victim.PlayerId) continue;
            int gold = victim.IsHero ? HeroGold : CreepGold;
            after = after with { Players = after.Players.Select(p => p.PlayerId == attacker.PlayerId
                ? p with { Gold = p.Gold + gold, GoldEarned = p.GoldEarned + gold,
                    CreepKills = p.CreepKills + (victim.IsHero ? 0 : 1),
                    GoldFromFarming = p.GoldFromFarming + (victim.IsHero ? 0 : gold) }
                : p).ToList() };
            events.Add(new GoldEvent(after.TurnNumber,
                $"Player {attacker.PlayerId} earns {gold} gold for {death.UnitId}.", attacker.PlayerId, gold, false));
            after = HeroProgression.GrantTeamXp(after, attacker.PlayerId,
                victim.IsHero ? HeroProgression.HeroKillTeamXp : HeroProgression.CreepKillTeamXp, events);
        }
        return after;
    }
}
