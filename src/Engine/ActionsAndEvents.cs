namespace ChampionsOfAshenveil.Engine;

// ─────────────────────────────────────────────────────────────────────────────
//  ACTIONS — every player decision is an Action record.
//  The engine validates, executes and records them in the event log.
// ─────────────────────────────────────────────────────────────────────────────

public abstract record GameAction(int PlayerId);

/// <summary>Basic attack from one unit to a target.</summary>
public record AttackAction(int PlayerId, string AttackerId, string TargetId)
    : GameAction(PlayerId);

/// <summary>Use an ability on a target (or no target for self-casts).</summary>
public record UseAbilityAction(int PlayerId, string CasterId, string AbilityId, string? TargetId = null)
    : GameAction(PlayerId);

/// <summary>Play a card from hand (item, buff, summon unit).</summary>
public record PlayCardAction(int PlayerId, string CardId, string? TargetId = null)
    : GameAction(PlayerId);

/// <summary>Move a unit between front and back rows.</summary>
public record ChangeRowAction(int PlayerId, string UnitId, RowPosition NewRow)
    : GameAction(PlayerId);

/// <summary>Pass the turn without acting.</summary>
public record PassAction(int PlayerId) : GameAction(PlayerId);

/// <summary>Spend an earned point on one ability in the Hero's fixed kit.</summary>
public record UpgradeAbilityAction(int PlayerId, string HeroId, string AbilityId) : GameAction(PlayerId);

// ─────────────────────────────────────────────────────────────────────────────
//  EVENTS — produced by the engine, power the combat log and UI.
//  Every state change emits at least one human-readable event.
// ─────────────────────────────────────────────────────────────────────────────

public abstract record GameEvent(int Turn, string Description);

public record DamageEvent(int Turn, string Description, string AttackerId, string TargetId, int Amount, DamageType Type, bool WasLethal)
    : GameEvent(Turn, Description);

public record HealEvent(int Turn, string Description, string HealerId, string TargetId, int Amount)
    : GameEvent(Turn, Description);

public record EffectAppliedEvent(int Turn, string Description, string SourceId, string TargetId, string EffectId)
    : GameEvent(Turn, Description);

public record EffectExpiredEvent(int Turn, string Description, string TargetId, string EffectId)
    : GameEvent(Turn, Description);

public record AbilityUsedEvent(int Turn, string Description, string CasterId, string AbilityId, string? TargetId)
    : GameEvent(Turn, Description);

public record UnitDiedEvent(int Turn, string Description, string UnitId, int PlayerId)
    : GameEvent(Turn, Description);

public record TurnStartEvent(int Turn, string Description, int ActivePlayerId)
    : GameEvent(Turn, Description);

public record GameOverEvent(int Turn, string Description, int WinnerPlayerId)
    : GameEvent(Turn, Description);

public record InvalidActionEvent(int Turn, string Description, string Reason)
    : GameEvent(Turn, Description);

public record GoldEvent(int Turn, string Description, int PlayerId, int Amount, bool IsSpending)
    : GameEvent(Turn, Description);

public record CardPlayedEvent(int Turn, string Description, int PlayerId, string CardId, string TargetId)
    : GameEvent(Turn, Description);

public record CreepSpawnedEvent(int Turn, string Description, int PlayerId, string UnitId)
    : GameEvent(Turn, Description);

public record ExperienceEvent(int Turn, string Description, string HeroId, int Amount)
    : GameEvent(Turn, Description);

public record HeroLeveledEvent(int Turn, string Description, string HeroId, int Level)
    : GameEvent(Turn, Description);

public record AbilityUpgradedEvent(int Turn, string Description, string HeroId, string AbilityId, int Rank)
    : GameEvent(Turn, Description);
