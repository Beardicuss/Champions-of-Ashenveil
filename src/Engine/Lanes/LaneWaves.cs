using ChampionsOfAshenveil.Engine.Data;

namespace ChampionsOfAshenveil.Engine.Lanes;

/// <summary>Public, deterministic one-lane spawn schedule.</summary>
public static class LaneWaves
{
    public const int WaveInterval = 3;
    public const int MaxLivingCreepsPerSide = 4;

    public static GameState Spawn(GameState state, DataRegistry data, List<GameEvent> events)
    {
        if (state.TurnNumber != 1 && (state.TurnNumber - 1) % WaveInterval != 0) return state;

        var players = state.Players.Select(player =>
        {
            var units = player.Units.Where(u => u.IsHero || !u.IsDead).ToList();
            int living = units.Count(u => !u.IsHero && !u.IsDead);
            string faction = player.PlayerId == 0 ? "legion" : "hellbourne";
            string[] definitions = faction == "legion"
                ? ["legion_footman", "legion_ranged"]
                : ["hellbourne_demon", "hellbourne_ranged"];

            foreach (var id in definitions)
            {
                if (living >= MaxLivingCreepsPerSide || !data.Units.TryGetValue(id, out UnitDefinition? def)) break;
                string unitId = $"creep_{player.PlayerId}_{state.TurnNumber}_{living}";
                units.Add(new UnitState
                {
                    Id = unitId, DefinitionId = id, PlayerId = player.PlayerId, IsHero = false,
                    Row = def.AttackType == "melee" ? RowPosition.Front : RowPosition.Back,
                    // Provisional turn-based adaptation: waves should be farmable in a short match.
                    CurrentHp = Math.Max(1, def.MaxHp / 2), MaxHp = Math.Max(1, def.MaxHp / 2),
                    DamageMin = def.DamageMin, DamageMax = def.DamageMax,
                    Armor = def.Armor, MagicArmor = def.MagicArmor
                });
                events.Add(new CreepSpawnedEvent(state.TurnNumber,
                    $"{def.Name} joins player {player.PlayerId}'s wave.", player.PlayerId, unitId));
                living++;
            }
            return player with { Units = units };
        }).ToList();

        return state with { Players = players };
    }
}
