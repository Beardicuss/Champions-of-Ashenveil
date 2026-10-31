using Godot;
using ChampionsOfAshenveil.Engine;
using ChampionsOfAshenveil.Sim;

namespace ChampionsOfAshenveil.UI;

/// <summary>
/// Scene router and global state keeper.
/// Autoload singleton registered in project.godot.
/// Scenes call SceneManager.GoTo("res://...") to navigate.
/// MatchSetup is handed off from HeroPick to Battle.
/// </summary>
public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; } = null!;

    public MatchSetup? PendingSetup { get; set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void GoTo(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }

    public void StartBotTestMatch()
    {
        PendingSetup = new MatchSetup
        {
            Player0HeroId = "deadwood",
            Player1HeroId = "accursed",
            Player0Name   = "Deadwood Bot",
            Player1Name   = "Accursed Bot",
            Seed          = (ulong)Time.GetTicksMsec()
        };
        GoTo("res://src/UI/Scenes/Battle.tscn");
    }

    public void StartCustomMatch(MatchSetup setup)
    {
        PendingSetup = setup;
        GoTo("res://src/UI/Scenes/Battle.tscn");
    }
}
