using Godot;
using ChampionsOfAshenveil.Engine;
using ChampionsOfAshenveil.Engine.Progression;

namespace ChampionsOfAshenveil.UI;

/// <summary>Hero level and fixed-kit upgrade controls, separate from the battle scene.</summary>
public partial class HeroProgressionPanel : VBoxContainer
{
    public event Action<string, string>? UpgradeRequested;

    public void Render(PlayerState player, DataRegistry data, bool canUpgrade)
    {
        var rows = GetNode<VBoxContainer>("Scroll/Rows");
        foreach (Node child in rows.GetChildren()) child.QueueFree();

        foreach (var hero in player.Units.Where(u => u.IsHero))
        {
            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = $"{hero.DefinitionId.Capitalize()} Lv {hero.Level}  XP {hero.Experience}/{(hero.Level < HeroProgression.LevelCap ? HeroProgression.XpForNextLevel(hero.Level) : 0)}  Points {hero.AbilityPoints}",
                CustomMinimumSize = new Vector2(255, 0)
            });
            if (canUpgrade && data.Heroes.TryGetValue(hero.DefinitionId, out var definition))
            {
                foreach (var id in definition.AbilityIds)
                {
                    if (!HeroProgression.CanUpgrade(hero, id, data)) continue;
                    var name = data.Abilities.TryGetValue(id, out var ability) ? ability.Name : id;
                    int rank = hero.AbilityRanks.GetValueOrDefault(id) + 1;
                    var button = new Button { Text = $"+ {name} {rank}", TooltipText = "Spend one ability point" };
                    string heroId = hero.Id;
                    string abilityId = id;
                    button.Pressed += () => UpgradeRequested?.Invoke(heroId, abilityId);
                    row.AddChild(button);
                }
            }
            rows.AddChild(row);
        }
    }
}
