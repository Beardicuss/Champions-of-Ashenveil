namespace ChampionsOfAshenveil.Engine;

/// <summary>
/// Shared state mutation helpers used across Engine sub-systems.
/// Avoids duplicating the player→unit traversal pattern everywhere.
/// </summary>
public static class StateUtils
{
    public static GameState UpdateUnit(GameState state, UnitState updated)
    {
        var newPlayers = state.Players
            .Select(p => p with
            {
                Units = p.Units.Select(u => u.Id == updated.Id ? updated : u).ToList()
            })
            .ToList();
        return state with { Players = newPlayers };
    }
}
