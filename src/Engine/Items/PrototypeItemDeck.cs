namespace ChampionsOfAshenveil.Engine.Items;

/// <summary>Shared slice deck using only item definitions already traced to HoN.</summary>
public static class PrototypeItemDeck
{
    public static List<string> Build(DataRegistry data)
    {
        var equipment = data.Items.Values.Where(i => i.CardType == "equipment" && i.StatBonuses.Count > 0)
            .OrderBy(i => i.Cost).ThenBy(i => i.Id).Take(4).SelectMany(i => new[] { i.Id, i.Id });
        var consumables = data.Items.Values.Where(i => i.CardType == "consumable" && i.UseEffectId.Length > 0)
            .OrderBy(i => i.Id).SelectMany(i => new[] { i.Id, i.Id });
        return equipment.Concat(consumables).ToList();
    }
}
