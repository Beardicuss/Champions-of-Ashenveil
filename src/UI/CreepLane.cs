using Godot;
using ChampionsOfAshenveil.Engine;

namespace ChampionsOfAshenveil.UI;

/// <summary>Displays one side's automatic creep wave, separate from Hero controls.</summary>
public partial class CreepLane : VBoxContainer
{
    private Label? _summary;

    public override void _Ready() => _summary = GetNode<Label>("Summary");

    public void Render(PlayerState player)
    {
        _summary ??= GetNode<Label>("Summary");
        var creeps = player.Units.Where(u => !u.IsHero && !u.IsDead).ToList();
        _summary.Text = creeps.Count == 0
            ? "Wave: none"
            : "Wave: " + string.Join("  |  ", creeps.Select(u =>
                $"{u.DefinitionId.Replace('_', ' ')} {u.CurrentHp}/{u.MaxHp}"));
    }
}
