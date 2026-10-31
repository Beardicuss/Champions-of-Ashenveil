using Godot;
using ChampionsOfAshenveil.UI;
using ChampionsOfAshenveil.Engine;
using ChampionsOfAshenveil.Engine.Items;

namespace ChampionsOfAshenveil;

/// <summary>
/// Main menu scene. Entry point on game launch.
/// Shows game title, data summary, and navigation to Hero Pick or Bot Test.
/// </summary>
public partial class MainScene : Control
{
    private Label? _dataStatus;
    private Label? _botResults;

    public override void _Ready()
    {
        _dataStatus = GetNode<Label>("VBox/DataStatus");
        _botResults = GetNode<Label>("VBox/BotResults");
        GetNode<Button>("VBox/ButtonRow/BtnPlay").Pressed    += () => SceneManager.Instance.GoTo("res://src/UI/Scenes/HeroPick.tscn");
        GetNode<Button>("VBox/ButtonRow/BtnBotTest").Pressed += RunBotBatch;

        var d = GameManager.Instance.Data;
        _dataStatus!.Text = $"{d.Heroes.Count} Heroes  ·  {d.Abilities.Count} Abilities  ·  {d.Effects.Count} Effects  ·  {d.Items.Count} Items  ·  {d.Units.Count} Units";
    }

    private void RunBotBatch()
    {
        var data = GameManager.Instance.Data;
        var ids = data.Heroes.Keys.OrderBy(id => id).ToList();
        var deck = PrototypeItemDeck.Build(data);
        const int count = 100;
        var results = new List<ChampionsOfAshenveil.Sim.MatchResult>();
        for (int i = 0; i < count; i++)
        {
            var first = i % 2 == 0 ? ids.Take(3).ToList() : ids.Skip(3).Take(3).ToList();
            var second = i % 2 == 0 ? ids.Skip(3).Take(3).ToList() : ids.Take(3).ToList();
            var config = new MatchConfig { Players =
            [
                new PlayerConfig { PlayerId = 0, Name = "Bot A", HeroIds = first, DeckItemIds = deck },
                new PlayerConfig { PlayerId = 1, Name = "Bot B", HeroIds = second, DeckItemIds = deck }
            ] };
            results.Add(new ChampionsOfAshenveil.Sim.MatchRunner(data, (ulong)(i + 1)).Run(config, (ulong)(i + 1)));
        }
        string report = $"Bot batch ({count} matches): P0 wins {results.Count(r => r.WinnerId == 0)}, " +
            $"P1 wins {results.Count(r => r.WinnerId == 1)}, draws {results.Count(r => r.WinnerId < 0)}; " +
            $"avg {results.Average(r => r.TurnCount):F1} rounds, {results.Average(r => r.WallTimeSeconds):F3}s simulation; " +
            $"avg cards {results.Average(r => r.PlayerStats.Sum(p => p.CardsPlayed)):F1}, " +
            $"potions {results.Average(r => r.EventLog.OfType<CardPlayedEvent>().Count(e => e.CardId is "health_potion" or "mana_potion")):F1}, " +
            $"gold earned {results.Average(r => r.PlayerStats.Sum(p => p.GoldEarned)):F0}, " +
            $"spent {results.Average(r => r.PlayerStats.Sum(p => p.GoldSpent)):F0}; " +
            $"creep last hits {results.Average(r => r.PlayerStats.Sum(p => p.CreepKills)):F1}, " +
            $"farming gold {results.Average(r => r.PlayerStats.Sum(p => p.GoldFromFarming)):F0}, " +
            $"waves {results.Average(r => r.EventLog.Count(e => e is CreepSpawnedEvent)):F1}, " +
            $"survivors {results.Average(r => r.PlayerStats.Sum(p => p.LivingCreeps)):F1}; " +
            $"level-ups {results.Average(r => r.EventLog.Count(e => e is HeroLeveledEvent)):F1}, " +
            $"upgrades {results.Average(r => r.EventLog.Count(e => e is AbilityUpgradedEvent)):F1}, " +
            $"Ultimates {results.Average(r => r.EventLog.OfType<AbilityUpgradedEvent>().Count(e => data.Heroes.Values.Any(h => h.AbilityIds.LastOrDefault() == e.AbilityId))):F1}.";
        _botResults!.Text = report;
        GD.Print(report);
    }
}
