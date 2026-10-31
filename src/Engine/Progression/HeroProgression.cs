using ChampionsOfAshenveil.Engine.Data;

namespace ChampionsOfAshenveil.Engine.Progression;

/// <summary>Match-only Hero XP, levels, and fixed-kit upgrades.</summary>
public static class HeroProgression
{
    public const int LevelCap = 6;
    public const int PassiveXpPerRound = 10;
    public const int CreepKillTeamXp = 25;
    public const int HeroKillTeamXp = 80;

    public static int XpForNextLevel(int level) => 40 + 20 * (level - 1);

    public static GameState GrantTeamXp(GameState state, int playerId, int amount, List<GameEvent> events)
    {
        foreach (var hero in state.GetPlayer(playerId).Heroes.ToList())
            state = GrantXp(state, hero.Id, amount, events);
        return state;
    }

    public static GameState GrantXp(GameState state, string heroId, int amount, List<GameEvent> events)
    {
        var hero = state.FindUnit(heroId);
        if (hero is null || !hero.IsHero || hero.IsDead || amount <= 0) return state;
        int level = hero.Level;
        int xp = hero.Experience + amount;
        int points = hero.AbilityPoints;
        int hpGain = 0;
        int damageGain = 0;
        while (level < LevelCap && xp >= XpForNextLevel(level))
        {
            xp -= XpForNextLevel(level);
            level++;
            points++;
            hpGain += 20;
            damageGain += 2;
            events.Add(new HeroLeveledEvent(state.TurnNumber,
                $"{hero.Id} reaches level {level} and gains an ability point.", hero.Id, level));
        }
        events.Add(new ExperienceEvent(state.TurnNumber, $"{hero.Id} gains {amount} XP.", hero.Id, amount));
        return StateUtils.UpdateUnit(state, hero with
        {
            Level = level, Experience = xp, AbilityPoints = points,
            MaxHp = hero.MaxHp + hpGain, CurrentHp = hero.CurrentHp + hpGain,
            DamageMin = hero.DamageMin + damageGain, DamageMax = hero.DamageMax + damageGain
        });
    }

    public static bool CanUpgrade(UnitState hero, string abilityId, DataRegistry data)
    {
        if (!hero.IsHero || hero.IsDead || hero.AbilityPoints <= 0 ||
            !data.Heroes.TryGetValue(hero.DefinitionId, out var definition)) return false;
        int slot = definition.AbilityIds.IndexOf(abilityId);
        if (slot < 0 || !data.Abilities.TryGetValue(abilityId, out var ability)) return false;
        bool ultimate = slot == definition.AbilityIds.Count - 1;
        int rank = hero.AbilityRanks.GetValueOrDefault(abilityId);
        return (!ultimate || hero.Level >= 4) && rank < (ultimate ? 1 : Math.Min(3, ability.Original.MaxLevel));
    }

    public static GameState Upgrade(GameState state, string heroId, string abilityId, DataRegistry data,
        List<GameEvent> events)
    {
        var hero = state.FindUnit(heroId);
        if (hero is null || !CanUpgrade(hero, abilityId, data)) return state;
        int rank = hero.AbilityRanks.GetValueOrDefault(abilityId) + 1;
        var ranks = hero.AbilityRanks.ToDictionary(kv => kv.Key, kv => kv.Value);
        ranks[abilityId] = rank;
        events.Add(new AbilityUpgradedEvent(state.TurnNumber,
            $"{hero.Id} upgrades {abilityId} to rank {rank}.", hero.Id, abilityId, rank));
        return StateUtils.UpdateUnit(state, hero with { AbilityPoints = hero.AbilityPoints - 1, AbilityRanks = ranks });
    }

    public static int ScaledDamage(int baseDamage, int rank) =>
        (int)MathF.Round(baseDamage * (1f + 0.25f * Math.Max(0, rank - 1)));
}
