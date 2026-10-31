namespace ChampionsOfAshenveil.Engine;

// ─────────────────────────────────────────────────────────────────────────────
//  RUNTIME STATE — mutable objects that represent one match in progress.
//  The engine's apply(state, action) → {newState, events[]} contract means
//  these records are cloned on each state transition (immutable update pattern).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// An active status effect applied to a unit during a match.
/// </summary>
public record ActiveEffect(
    string             EffectId,
    int                TurnsRemaining,
    int                StackCount    = 1,
    string             SourceUnitId  = "",
    IReadOnlyList<string>? GrantedFlags = null
);

/// <summary>
/// Cooldown tracker for one ability slot.
/// </summary>
public record AbilityCooldown(
    string AbilityId,
    int    TurnsRemaining
);

/// <summary>
/// Runtime state of a single unit (hero or summoned unit) during a match.
/// </summary>
public record UnitState
{
    public string       Id             { get; init; } = "";
    public string       DefinitionId   { get; init; } = "";   // key into DataRegistry
    public int          PlayerId       { get; init; }
    public bool         IsHero         { get; init; }
    public RowPosition  Row            { get; init; }

    // Vitals
    public int   CurrentHp     { get; init; }
    public int   MaxHp         { get; init; }
    public int   CurrentEnergy { get; init; }
    public int   MaxEnergy     { get; init; }

    // Combat stats (base + item bonuses applied at construction)
    public int   DamageMin   { get; init; }
    public int   DamageMax   { get; init; }
    public float Armor       { get; init; }
    public float MagicArmor  { get; init; }

    // Level (heroes only; 1–25)
    public int Level { get; init; } = 1;
    public int Experience { get; init; }
    public int AbilityPoints { get; init; }
    public IReadOnlyDictionary<string, int> AbilityRanks { get; init; } = new Dictionary<string, int>();
    public int ActionsTakenThisTurn { get; init; }

    // Active effects and cooldowns
    public IReadOnlyList<ActiveEffect>    Effects    { get; init; } = [];
    public IReadOnlyList<AbilityCooldown> Cooldowns  { get; init; } = [];
    public IReadOnlyList<string>          ItemIds    { get; init; } = [];

    public bool IsDead      => CurrentHp <= 0;
    public bool IsStunned   => HasStatus(StatusFlags.Stunned);
    public bool IsSilenced  => HasStatus(StatusFlags.Silenced);
    public bool IsDisarmed  => HasStatus(StatusFlags.Disarmed);

    public bool HasStatus(StatusFlags flag)
    {
        var flagName = flag.ToString();
        return Effects.Any(ae =>
            ae.GrantedFlags is not null &&
            ae.GrantedFlags.Contains(flagName, StringComparer.OrdinalIgnoreCase));
    }

    public int GetAbilityCooldown(string abilityId)
    {
        var cd = Cooldowns.FirstOrDefault(c => c.AbilityId == abilityId);
        return cd?.TurnsRemaining ?? 0;
    }
}

/// <summary>
/// State of one player's side of the board.
/// </summary>
public record PlayerState
{
    public int    PlayerId { get; init; }
    public string Name     { get; init; } = "";
    public IReadOnlyList<UnitState> Units { get; init; } = [];
    public int Gold { get; init; }
    public int GoldEarned { get; init; }
    public int GoldSpent { get; init; }
    public int CardsPlayed { get; init; }
    public int CreepKills { get; init; }
    public int GoldFromFarming { get; init; }
    public IReadOnlyList<string> Hand { get; init; } = [];
    public IReadOnlyList<string> DrawPile { get; init; } = [];
    public IReadOnlyList<string> DiscardPile { get; init; } = [];
    public int CardsPlayedThisTurn { get; init; }

    public IEnumerable<UnitState> Heroes => Units.Where(u => u.IsHero && !u.IsDead);
    public IEnumerable<UnitState> AliveUnits => Units.Where(u => !u.IsDead);
    public bool IsDefeated => !Heroes.Any();
}

/// <summary>
/// Full game state for one match. Immutable — the engine produces new states.
/// This is what gets serialised for replays: (InitialState + Seed + ActionList) → deterministic replay.
/// </summary>
public record GameState
{
    public int    TurnNumber  { get; init; } = 1;
    public int    ActivePlayer { get; init; } = 0;  // index into Players
    public bool   IsGameOver   { get; init; }
    public int    WinnerId     { get; init; } = -1;

    public IReadOnlyList<PlayerState> Players { get; init; } = [];

    // Seeded RNG state — serialised so replays are exact.
    public ulong  RngSeed  { get; init; }
    public ulong  RngState { get; init; }

    public PlayerState GetPlayer(int id) =>
        Players.FirstOrDefault(p => p.PlayerId == id)
        ?? throw new InvalidOperationException($"Player {id} not found.");

    public UnitState? FindUnit(string unitId) =>
        Players.SelectMany(p => p.Units).FirstOrDefault(u => u.Id == unitId);
}
