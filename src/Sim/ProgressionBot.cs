using ChampionsOfAshenveil.Engine;
using ChampionsOfAshenveil.Engine.Progression;

namespace ChampionsOfAshenveil.Sim;

/// <summary>Deterministic baseline policy for spending earned Hero ability points.</summary>
public static class ProgressionBot
{
    public static UpgradeAbilityAction? ChooseUpgrade(GameState state, DataRegistry data)
    {
        foreach (var hero in state.GetPlayer(state.ActivePlayer).Heroes)
        {
            if (hero.AbilityPoints <= 0 || !data.Heroes.TryGetValue(hero.DefinitionId, out var definition)) continue;
            var ultimate = definition.AbilityIds.LastOrDefault();
            if (ultimate is not null && HeroProgression.CanUpgrade(hero, ultimate, data))
                return new UpgradeAbilityAction(state.ActivePlayer, hero.Id, ultimate);
            foreach (var abilityId in definition.AbilityIds.Take(3).OrderBy(id => hero.AbilityRanks.GetValueOrDefault(id)))
                if (HeroProgression.CanUpgrade(hero, abilityId, data))
                    return new UpgradeAbilityAction(state.ActivePlayer, hero.Id, abilityId);
        }
        return null;
    }
}
