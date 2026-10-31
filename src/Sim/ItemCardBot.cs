using ChampionsOfAshenveil.Engine;

namespace ChampionsOfAshenveil.Sim;

/// <summary>Baseline card policy for HoN-derived equipment and potion items.</summary>
public static class ItemCardBot
{
    public static PlayCardAction? Choose(GameState state, DataRegistry data)
    {
        var player = state.GetPlayer(state.ActivePlayer);
        if (player.CardsPlayedThisTurn >= 2) return null;
        foreach (var id in player.Hand.OrderBy(id => data.Items.GetValueOrDefault(id)?.Cost ?? int.MaxValue))
        {
            if (!data.Items.TryGetValue(id, out var item) || player.Gold < item.Cost) continue;
            UnitState? target = item.Id switch
            {
                "health_potion" => player.AliveUnits.Where(u => u.MaxHp - u.CurrentHp >= 100)
                    .OrderByDescending(u => u.MaxHp - u.CurrentHp).FirstOrDefault(),
                "mana_potion" => player.Heroes.Where(u => u.MaxEnergy - u.CurrentEnergy >= 50)
                    .OrderByDescending(u => u.MaxEnergy - u.CurrentEnergy).FirstOrDefault(),
                _ when item.CardType == "equipment" => player.Heroes.FirstOrDefault(u => u.ItemIds.Count < 2),
                _ => null
            };
            if (target is not null) return new PlayCardAction(state.ActivePlayer, id, target.Id);
        }
        return null;
    }
}
