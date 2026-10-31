using ChampionsOfAshenveil.Engine;

namespace ChampionsOfAshenveil.Sim;

/// <summary>
/// Runs a complete match between bots and collects the event log.
/// Headless — no Godot dependencies. Can be called from the editor or a test.
/// </summary>
public class MatchRunner
{
    public const int MaxTurns = 200; // safety cap — detect infinite loops

    private readonly DataRegistry  _data;
    private readonly GameEngine    _engine;
    private readonly SeededRng     _rng;

    public List<GameEvent> EventLog   { get; } = [];
    public List<string>    TextLog    { get; } = [];
    public MatchResult?    Result     { get; private set; }

    public MatchRunner(DataRegistry data, ulong seed)
    {
        _data   = data;
        _engine = new GameEngine(data, seed);
        _rng    = new SeededRng(seed ^ 0xDEADBEEF); // separate RNG stream for bot decisions
    }

    /// <summary>
    /// Run a full match to completion (or MaxTurns).
    /// Returns the MatchResult with winner, turn count and full event log.
    /// </summary>
    public MatchResult Run(MatchConfig config, ulong seed)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var state = MatchBuilder.BuildInitialState(config, _data, seed);
        Log($"=== MATCH START  Seed:{seed} ===");
        foreach (var p in state.Players)
        {
            var heroes = string.Join(", ", p.Units.Select(u => $"{u.Id}({u.CurrentHp}HP)"));
            Log($"  Player {p.PlayerId} ({p.Name}): {heroes}");
        }

        int turnsWithoutProgress = 0;
        int lastTotalHp = TotalHp(state);

        while (!state.IsGameOver && state.TurnNumber <= MaxTurns)
        {
            // Each living Hero may act once; item decisions use a separate policy.
            int active = state.ActivePlayer;
            for (int i = 0; i < 18; i++)
            {
                var upgrade = ProgressionBot.ChooseUpgrade(state, _data);
                if (upgrade is null) break;
                var result = _engine.Apply(state, upgrade);
                state = result.NewState;
                CollectEvents(result.Events, state.TurnNumber);
            }
            for (int i = 0; i < 3 && !state.IsGameOver; i++)
            {
                var action = RandomBot.ChooseAction(state, _rng, _data);
                if (action is PassAction) break;
                var result = _engine.Apply(state, action);
                state = result.NewState;
                CollectEvents(result.Events, state.TurnNumber);
            }
            for (int i = 0; i < 2 && !state.IsGameOver; i++)
            {
                var action = ItemCardBot.Choose(state, _data);
                if (action is null) break;
                var result = _engine.Apply(state, action);
                state = result.NewState;
                CollectEvents(result.Events, state.TurnNumber);
            }

            // Advance to next player after action
            if (!state.IsGameOver)
            {
                var advResult = _engine.AdvanceTurn(state);
                state = advResult.NewState;
                CollectEvents(advResult.Events, state.TurnNumber);
            }

            // Infinite-loop / stalemate detection
            int currentHp = TotalHp(state);
            if (currentHp == lastTotalHp)
            {
                turnsWithoutProgress++;
                if (turnsWithoutProgress >= 20)
                {
                    Log($"[WARNING] No HP change in 20 turns — possible stalemate at turn {state.TurnNumber}.");
                    break;
                }
            }
            else
            {
                turnsWithoutProgress = 0;
                lastTotalHp = currentHp;
            }
        }

        // Build result
        string outcome;
        if (state.IsGameOver)
        {
            var winner = state.Players.FirstOrDefault(p => p.PlayerId == state.WinnerId);
            outcome = $"Player {state.WinnerId} ({winner?.Name ?? "?"}) WINS";
        }
        else if (state.TurnNumber > MaxTurns)
            outcome = "DRAW — max turns reached";
        else
            outcome = "DRAW — stalemate detected";

        Log($"=== MATCH END  Turn:{state.TurnNumber}  {outcome} ===");
        LogFinalHp(state);
        clock.Stop();

        Result = new MatchResult(
            WinnerId:   state.IsGameOver ? state.WinnerId : -1,
            TurnCount:  state.TurnNumber,
            Outcome:    outcome,
            EventLog:   [.. EventLog],
            TextLog:    [.. TextLog],
            WallTimeSeconds: clock.Elapsed.TotalSeconds,
            PlayerStats: state.Players.Select(p => new MatchPlayerStats(p.PlayerId, p.CardsPlayed, p.GoldEarned, p.GoldSpent,
                p.CreepKills, p.GoldFromFarming, p.Units.Count(u => !u.IsHero && !u.IsDead))).ToList()
        );
        return Result;
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private void CollectEvents(IReadOnlyList<GameEvent> events, int turn)
    {
        foreach (var e in events)
        {
            EventLog.Add(e);
            Log($"[T{turn:D3}] {e.Description}");
        }
    }

    private void Log(string msg) => TextLog.Add(msg);

    private static int TotalHp(GameState state) =>
        state.Players.SelectMany(p => p.Units).Sum(u => u.CurrentHp);

    private void LogFinalHp(GameState state)
    {
        foreach (var p in state.Players)
        {
            foreach (var u in p.Units)
                Log($"  [{(u.IsDead ? "DEAD" : "ALIVE")}] {u.Id}: {u.CurrentHp}/{u.MaxHp} HP");
        }
    }
}

/// <summary>
/// Summary result of a completed match run.
/// </summary>
public record MatchResult(
    int                  WinnerId,
    int                  TurnCount,
    string               Outcome,
    List<GameEvent>      EventLog,
    List<string>         TextLog,
    double              WallTimeSeconds,
    List<MatchPlayerStats> PlayerStats
);

public record MatchPlayerStats(int PlayerId, int CardsPlayed, int GoldEarned, int GoldSpent,
    int CreepKills, int GoldFromFarming, int LivingCreeps);
