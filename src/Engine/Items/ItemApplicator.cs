using ChampionsOfAshenveil.Engine.Data;

namespace ChampionsOfAshenveil.Engine.Items;

/// <summary>
/// Applies ItemDefinition.StatBonuses to a UnitState.
/// Called when a unit equips an item (at match build time or via shop).
/// Single responsibility: read stat_bonuses dict → mutate UnitState fields.
/// </summary>
public static class ItemApplicator
{
    /// <summary>
    /// Returns a new UnitState with the item's stat bonuses applied.
    /// Unknown bonus keys are logged to console and skipped.
    /// </summary>
    public static UnitState Apply(UnitState unit, ItemDefinition item)
    {
        int    hp         = unit.MaxHp;
        int    energy     = unit.MaxEnergy;
        int    damMin     = unit.DamageMin;
        int    damMax     = unit.DamageMax;
        float  armor      = unit.Armor;
        float  magArmor   = unit.MagicArmor;

        foreach (var (key, value) in item.StatBonuses)
        {
            switch (key.ToLowerInvariant())
            {
                case "bonus_hp":          hp       += (int)value; break;
                case "bonus_energy":      energy   += (int)value; break;
                case "bonus_damage":      damMin   += (int)value; damMax += (int)value; break;
                case "bonus_armor":       armor    += value;      break;
                case "bonus_magic_armor": magArmor += value;      break;
                default:
                    Console.WriteLine($"[ItemApplicator] Unknown stat bonus key '{key}' on item '{item.Id}' — skipped.");
                    break;
            }
        }

        return unit with
        {
            MaxHp      = hp,
            CurrentHp  = unit.CurrentHp + Math.Max(0, hp - unit.MaxHp), // HP delta goes to current too
            MaxEnergy  = energy,
            DamageMin  = damMin,
            DamageMax  = damMax,
            Armor      = armor,
            MagicArmor = magArmor,
            ItemIds    = [.. unit.ItemIds, item.Id]
        };
    }

    /// <summary>
    /// Applies all items in a list to a unit in sequence.
    /// </summary>
    public static UnitState ApplyAll(UnitState unit, IEnumerable<string> itemIds, DataRegistry data)
    {
        foreach (var id in itemIds)
        {
            if (data.Items.TryGetValue(id, out var item))
                unit = Apply(unit, item);
            else
                Console.WriteLine($"[ItemApplicator] Item not found: '{id}'");
        }
        return unit;
    }
}
