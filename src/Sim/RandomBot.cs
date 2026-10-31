using ChampionsOfAshenveil.Engine;
using ChampionsOfAshenveil.Engine.Data;

namespace ChampionsOfAshenveil.Sim;

/// <summary>
/// Random-move bot. Generates a legal action for the active player by:
///   1. Collecting all candidate actions (attacks, abilities, pass).
///   2. Filtering out obviously invalid ones (dead caster, cooldown, energy).
///   3. Picking uniformly at random.
///
/// This is Phase 1 simulation baseline. Used to find crashes, infinite loops
/// and sanity-check the engine — not for balance evaluation (use greedy bot later).
/// </summary>
public static class RandomBot
{
    public static GameAction ChooseAction(GameState state, SeededRng rng, DataRegistry data)
    {
        var actions = BuildCandidateActions(state, data);
        if (actions.Count == 0) return new PassAction(state.ActivePlayer);
        return actions[rng.RollRange(0, actions.Count - 1)];
    }

    private static List<GameAction> BuildCandidateActions(GameState state, DataRegistry data)
    {
        var result = new List<GameAction>();
        var player  = state.GetPlayer(state.ActivePlayer);
        var enemies = state.Players
            .Where(p => p.PlayerId != state.ActivePlayer)
            .SelectMany(p => p.AliveUnits)
            .ToList();
        var allies = player.AliveUnits.ToList();

        if (enemies.Count == 0) return result; // nothing to do — game should be over

        foreach (var unit in allies)
        {
            if (unit.ActionsTakenThisTurn > 0 || unit.IsStunned) continue;
            // ── Attacks ─────────────────────────────────────────
            if (!unit.IsDisarmed)
            {
                foreach (var target in enemies)
                    result.Add(new AttackAction(state.ActivePlayer, unit.Id, target.Id));
            }

            // ── Abilities ────────────────────────────────────────
            foreach (var abilityId in GetUnitAbilityIds(unit, data))
            {
                if (unit.AbilityRanks.GetValueOrDefault(abilityId) <= 0) continue;
                if (unit.GetAbilityCooldown(abilityId) > 0) continue;

                var abilityDef = data.Abilities.GetValueOrDefault(abilityId);
                if (abilityDef is null) continue;
                if (unit.CurrentEnergy < abilityDef.Adapted.EnergyCost) continue;
                if (unit.IsSilenced) continue;

                // Add one candidate action per valid target
                switch (abilityDef.Adapted.Targeting)
                {
                    case "self":
                    case "self_area":
                        result.Add(new UseAbilityAction(state.ActivePlayer, unit.Id, abilityId, null));
                        break;

                    case "single":
                        foreach (var target in enemies)
                            result.Add(new UseAbilityAction(state.ActivePlayer, unit.Id, abilityId, target.Id));
                        break;

                    case "single_ally":
                        foreach (var ally in allies)
                            result.Add(new UseAbilityAction(state.ActivePlayer, unit.Id, abilityId, ally.Id));
                        break;

                    case "area":
                        // Area abilities target a row — just add one action (engine handles AoE)
                        result.Add(new UseAbilityAction(state.ActivePlayer, unit.Id, abilityId, enemies[0].Id));
                        break;

                    default:
                        result.Add(new UseAbilityAction(state.ActivePlayer, unit.Id, abilityId, null));
                        break;
                }
            }
        }

        // Always allow pass
        result.Add(new PassAction(state.ActivePlayer));
        return result;
    }

    private static IEnumerable<string> GetUnitAbilityIds(UnitState unit, DataRegistry data)
    {
        if (!data.Heroes.TryGetValue(unit.DefinitionId, out var heroDef))
            return Enumerable.Empty<string>();
        return heroDef.AbilityIds;
    }
}
