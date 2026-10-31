using ChampionsOfAshenveil.Engine;
using ChampionsOfAshenveil.Engine.Data;
using ChampionsOfAshenveil.Engine.Items;
using ChampionsOfAshenveil.Engine.Lanes;

namespace ChampionsOfAshenveil;

/// <summary>Configuration for starting a match.</summary>
public class MatchConfig
{
    public List<PlayerConfig> Players { get; set; } = [];
}

public class PlayerConfig
{
    public int    PlayerId { get; set; }
    public string Name     { get; set; } = "";
    public List<string> HeroIds  { get; set; } = []; // keys into DataRegistry.Heroes
    /// <summary>Items given to all heroes of this player at match start.</summary>
    public List<string> StartItemIds { get; set; } = [];
    public List<string> DeckItemIds { get; set; } = [];
}

/// <summary>
/// Builds the initial GameState from a MatchConfig and DataRegistry.
/// Converts hero definitions into runtime UnitState objects and applies starting items.
/// </summary>
public static class MatchBuilder
{
    public static GameState BuildInitialState(MatchConfig config, DataRegistry data, ulong seed)
    {
        var players = new List<PlayerState>();

        foreach (var playerCfg in config.Players)
        {
            var units = new List<UnitState>();
            foreach (var heroId in playerCfg.HeroIds)
            {
                if (!data.Heroes.TryGetValue(heroId, out var def))
                    continue;

                var unit = HeroToUnitState(def, playerCfg.PlayerId, heroId);

                // Apply starting items (stat bonuses take effect immediately)
                if (playerCfg.StartItemIds.Count > 0)
                    unit = ItemApplicator.ApplyAll(unit, playerCfg.StartItemIds, data);

                units.Add(unit);
            }

            players.Add(new PlayerState
            {
                PlayerId = playerCfg.PlayerId,
                Name     = playerCfg.Name,
                Units    = units,
                Hand     = playerCfg.DeckItemIds.Take(4).ToList(),
                DrawPile = playerCfg.DeckItemIds.Skip(4).ToList()
            });
        }

        var initial = new GameState
        {
            TurnNumber   = 1,
            ActivePlayer = 0,
            Players      = players,
            RngSeed      = seed,
            RngState     = seed
        };
        return LaneWaves.Spawn(initial, data, []);
    }

    private static UnitState HeroToUnitState(HeroDefinition def, int playerId, string heroId)
    {
        var adapted = def.Adapted;
        var orig    = def.Original;

        return new UnitState
        {
            Id            = $"hero_{playerId}_{heroId}",
            DefinitionId  = heroId,
            PlayerId      = playerId,
            IsHero        = true,
            Row           = orig.AttackType == "melee" ? RowPosition.Front : RowPosition.Back,
            CurrentHp     = adapted.MaxHp,
            MaxHp         = adapted.MaxHp,
            CurrentEnergy = adapted.MaxEnergy,
            MaxEnergy     = adapted.MaxEnergy,
            DamageMin     = adapted.BaseDamageMin,
            DamageMax     = adapted.BaseDamageMax,
            Armor         = adapted.Armor,
            MagicArmor    = adapted.MagicArmor,
            Level         = 1,
            Experience    = 0,
            AbilityPoints = 0,
            AbilityRanks  = def.AbilityIds.Count > 0
                ? new Dictionary<string, int> { [def.AbilityIds[0]] = 1 }
                : new Dictionary<string, int>(),
            Effects       = [],
            Cooldowns     = [],
            ItemIds       = []
        };
    }
}
