using Godot;
using ChampionsOfAshenveil.Engine;

namespace ChampionsOfAshenveil.UI;

/// <summary>Hand and target selection for equipment and HoN-derived consumables.</summary>
public partial class ItemHandPanel : VBoxContainer
{
    public event Action<string, string>? PlayRequested;

    public void Render(GameState state, int playerId, DataRegistry data, bool enabled)
    {
        var hand = GetNode<HBoxContainer>("HandScroll/Hand");
        var targets = GetNode<HBoxContainer>("TargetScroll/Targets");
        Clear(hand);
        Clear(targets);
        var player = state.GetPlayer(playerId);
        int index = 0;
        foreach (var id in player.Hand)
        {
            if (!data.Items.TryGetValue(id, out var item)) continue;
            var button = new Button
            {
                Name = $"Item_{index++}", Text = $"{item.Name} ({item.Cost}g)",
                TooltipText = item.Notes,
                Disabled = !enabled || player.Gold < item.Cost || player.CardsPlayedThisTurn >= 2
            };
            string selected = id;
            button.Pressed += () => ShowTargets(state, playerId, selected, data, targets);
            hand.AddChild(button);
        }
    }

    private void ShowTargets(GameState state, int playerId, string itemId, DataRegistry data, HBoxContainer targets)
    {
        Clear(targets);
        if (!data.Items.TryGetValue(itemId, out var item)) return;
        var allies = state.GetPlayer(playerId).AliveUnits.Where(unit => item.CardType == "consumable"
            ? item.Id != "mana_potion" || unit.MaxEnergy > 0
            : unit.IsHero && unit.ItemIds.Count < 2);
        foreach (var unit in allies)
        {
            var button = new Button { Text = $"{unit.DefinitionId.Replace('_', ' ')} {unit.CurrentHp}HP" };
            string target = unit.Id;
            button.Pressed += () => PlayRequested?.Invoke(itemId, target);
            targets.AddChild(button);
        }
    }

    private static void Clear(Node parent)
    {
        foreach (Node child in parent.GetChildren()) child.QueueFree();
    }
}
