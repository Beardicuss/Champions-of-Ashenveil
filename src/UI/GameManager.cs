using Godot;
using ChampionsOfAshenveil.Engine;
using ChampionsOfAshenveil.Sim;

namespace ChampionsOfAshenveil;

/// <summary>
/// Autoload singleton — boots the engine and data registry when the game starts.
/// Access via: GetNode&lt;GameManager&gt;("/root/GameManager")
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; } = null!;

    public DataRegistry Data   { get; private set; } = null!;
    public GameEngine?  Engine { get; private set; }
    public GameState?   State  { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        LoadData();
        GD.Print("[GameManager] Champions of Ashenveil — ready.");
    }

    private void LoadData()
    {
        Data = new DataRegistry();
        // Data path: res://data/ (Godot virtual filesystem)
        var dataPath = ProjectSettings.GlobalizePath("res://data");
        Data.Load(dataPath);
        GD.Print($"[GameManager] Data loaded — Heroes: {Data.Heroes.Count}, Abilities: {Data.Abilities.Count}, Effects: {Data.Effects.Count}");
    }

    /// <summary>
    /// Start a new match with the given seed and player configurations.
    /// </summary>
    public void StartMatch(ulong seed, MatchConfig config)
    {
        Engine = new GameEngine(Data, seed);
        State  = MatchBuilder.BuildInitialState(config, Data, seed);
        GD.Print($"[GameManager] Match started. Seed: {seed}");
        EmitSignal(SignalName.MatchStarted);
    }

    /// <summary>Apply a player action and update state.</summary>
    public IReadOnlyList<GameEvent> ApplyAction(GameAction action)
    {
        if (Engine is null || State is null)
            return [];

        var result = Engine.Apply(State, action);
        State = result.NewState;

        if (State.IsGameOver)
            EmitSignal(SignalName.MatchEnded, State.WinnerId);

        return result.Events;
    }

    /// <summary>Advance to next player's turn.</summary>
    public IReadOnlyList<GameEvent> AdvanceTurn()
    {
        if (Engine is null || State is null) return [];
        var result = Engine.AdvanceTurn(State);
        State = result.NewState;
        return result.Events;
    }

    /// <summary>
    /// Run a single headless bot-vs-bot test match: Deadwood (P0) vs Accursed (P1).
    /// Returns the full text log. Safe to call from any scene.
    /// </summary>
    public MatchResult RunBotTest(ulong seed = 12345UL)
    {
        var config = new MatchConfig
        {
            Players =
            [
                new PlayerConfig { PlayerId = 0, Name = "Deadwood Bot",  HeroIds = ["deadwood"]  },
                new PlayerConfig { PlayerId = 1, Name = "Accursed Bot", HeroIds = ["accursed"] }
            ]
        };

        var runner = new MatchRunner(Data, seed);
        var result = runner.Run(config, seed);

        GD.Print($"[BotTest] {result.Outcome} after {result.TurnCount} turns.");
        EmitSignal(SignalName.BotTestFinished, result.Outcome, result.TurnCount);
        return result;
    }

    [Signal] public delegate void MatchStartedEventHandler();
    [Signal] public delegate void MatchEndedEventHandler(int winnerId);
    [Signal] public delegate void BotTestFinishedEventHandler(string outcome, int turns);
}

