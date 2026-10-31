using ChampionsOfAshenveil.Engine.Abilities;
using ChampionsOfAshenveil.Engine.Data;
using ChampionsOfAshenveil.Engine.Effects;
using ChampionsOfAshenveil.Engine.Items;
using ChampionsOfAshenveil.Engine.Lanes;
using ChampionsOfAshenveil.Engine.Progression;

namespace ChampionsOfAshenveil.Engine;

/// <summary>
/// Result of apply(state, action).
/// </summary>
public record EngineResult(GameState NewState, IReadOnlyList<GameEvent> Events);

/// <summary>
/// The core rules engine.
/// Pure function: apply(state, action) → {newState, events[]}
/// No UI, no I/O, no hidden global state. (Plan section 7.)
///
/// Combat resolution order (plan section 7):
///   1. Validate action
///   2. Validate target
///   3. Check restrictions (silence, stun, disarm, etc.)
///   4. Base values
///   5. Bonuses
///   6. Reductions
///   7. Defenses (armor / magic armor)
///   8. Apply damage or healing
///   9. Secondary effects
///  10. Triggers
///  11. Cooldown and state updates
///  12. Victory check
/// </summary>
public class GameEngine
{
    private readonly DataRegistry _data;
    private readonly SeededRng    _rng;

    public GameEngine(DataRegistry data, ulong seed)
    {
        _data = data;
        _rng  = new SeededRng(seed);
    }

    /// <summary>
    /// Apply one player action to the current state and return the new state plus all events emitted.
    /// </summary>
    public EngineResult Apply(GameState state, GameAction action)
    {
        var events = new List<GameEvent>();

        // 1. Validate: is it this player's turn?
        if (action.PlayerId != state.ActivePlayer)
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, $"Player {action.PlayerId} acted out of turn.", "Not your turn."));
            return new EngineResult(state, events);
        }
        if (state.IsGameOver)
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, "Match is over.", "Match is over."));
            return new EngineResult(state, events);
        }

        // 2. Dispatch to handler
        var newState = action switch
        {
            AttackAction     a => HandleAttack(state, a, events),
            UseAbilityAction a => HandleAbility(state, a, events),
            PlayCardAction   a => ItemCardPlay.Apply(state, a, _data, events),
            ChangeRowAction  a => HandleChangeRow(state, a, events),
            PassAction       a => HandlePass(state, a, events),
            UpgradeAbilityAction a => HandleUpgradeAbility(state, a, events),
            _ => state
        };
        newState = InterruptedEffects.AfterDamage(state, newState, _data, events);
        newState = FarmingRewards.Apply(state, newState, events);

        // 12. Victory check
        newState = CheckVictory(newState, events);
        if (!events.Any(e => e is InvalidActionEvent) && action is AttackAction or UseAbilityAction or ChangeRowAction)
        {
            string unitId = action switch
            {
                AttackAction a => a.AttackerId,
                UseAbilityAction a => a.CasterId,
                ChangeRowAction a => a.UnitId,
                _ => ""
            };
            newState = UpdateUnit(newState, unitId, u => u with { ActionsTakenThisTurn = 1 });
        }

        return new EngineResult(newState, events);
    }

    // ─── TURN MANAGEMENT ────────────────────────────────────────────────────

    /// <summary>
    /// Advances to the next player's turn. Ticks down cooldowns and effect durations.
    /// Call this after the active player has completed their turn.
    /// </summary>
    public EngineResult AdvanceTurn(GameState state)
    {
        var events = new List<GameEvent>();

        // Advance active player
        int nextPlayer = (state.ActivePlayer + 1) % state.Players.Count;
        int nextTurn   = nextPlayer == 0 ? state.TurnNumber + 1 : state.TurnNumber;

        var phaseState = state;
        if (nextPlayer == 0)
        {
            phaseState = CreepCombat.Resolve(phaseState, _rng, events);
            phaseState = phaseState with { Players = phaseState.Players.Select(p =>
                p with { Units = TickUnits(p.Units, state.TurnNumber, events).ToList() }).ToList() };
            foreach (var player in phaseState.Players)
                phaseState = HeroProgression.GrantTeamXp(phaseState, player.PlayerId,
                    HeroProgression.PassiveXpPerRound, events);
        }

        var newPlayers = phaseState.Players.Select(p => p.PlayerId == nextPlayer
            ? p with { Units = p.Units.Select(u => u with { ActionsTakenThisTurn = 0 }).ToList() }
            : p).ToList();

        var newState = state with
        {
            Players      = newPlayers,
            ActivePlayer = nextPlayer,
            TurnNumber   = nextTurn,
            RngState     = _rng.State
        };
        if (nextPlayer == 0)
            newState = LaneWaves.Spawn(newState, _data, events);
        var incoming = newState.GetPlayer(nextPlayer);
        var hand = incoming.Hand.ToList();
        var drawPile = incoming.DrawPile.ToList();
        if (drawPile.Count > 0 && hand.Count < 7)
        {
            hand.Add(drawPile[0]);
            drawPile.RemoveAt(0);
        }
        int income = 100;
        newState = newState with { Players = newState.Players.Select(p => p.PlayerId == nextPlayer
            ? p with { Gold = p.Gold + income, GoldEarned = p.GoldEarned + income, Hand = hand, DrawPile = drawPile, CardsPlayedThisTurn = 0 }
            : p).ToList() };
        events.Add(new GoldEvent(nextTurn, $"Player {nextPlayer} earns {income} gold.", nextPlayer, income, false));

        events.Add(new TurnStartEvent(nextTurn, $"Turn {nextTurn} — Player {nextPlayer}'s turn.", nextPlayer));

        newState = CheckVictory(newState, events);
        return new EngineResult(newState, events);
    }

    // ─── HANDLERS ───────────────────────────────────────────────────────────

    private GameState HandleAttack(GameState state, AttackAction action, List<GameEvent> events)
    {
        var attacker = state.FindUnit(action.AttackerId);
        var target   = state.FindUnit(action.TargetId);

        if (attacker is null || target is null || attacker.IsDead || target.IsDead)
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, "Attack invalid.", "Unit not found or dead."));
            return state;
        }
        if (attacker.PlayerId != action.PlayerId || target.PlayerId == action.PlayerId || attacker.ActionsTakenThisTurn > 0 || attacker.IsStunned)
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, "Attack invalid.", "Ownership, target, stun, or Hero action limit."));
            return state;
        }

        if (attacker.IsDisarmed)
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, $"{action.AttackerId} is disarmed.", "Disarmed — cannot attack."));
            return state;
        }

        // 4–7: Compute damage
        int rawDamage    = _rng.RollRange(attacker.DamageMin, attacker.DamageMax);
        int finalDamage  = ApplyPhysicalArmor(rawDamage, target.Armor);
        bool lethal      = target.CurrentHp - finalDamage <= 0;

        events.Add(new DamageEvent(state.TurnNumber,
            $"{action.AttackerId} attacks {action.TargetId} for {finalDamage} physical damage.",
            action.AttackerId, action.TargetId, finalDamage, DamageType.Physical, lethal));

        // 8: Apply damage
        var newState = ApplyDamageToUnit(state, action.TargetId, finalDamage, events);
        return newState;
    }

    private GameState HandleAbility(GameState state, UseAbilityAction action, List<GameEvent> events)
    {
        var caster = state.FindUnit(action.CasterId);
        if (caster is null || caster.IsDead)
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, "Ability invalid.", "Caster not found or dead."));
            return state;
        }
        if (caster.PlayerId != action.PlayerId || caster.ActionsTakenThisTurn > 0 || caster.IsStunned ||
            !_data.Heroes.TryGetValue(caster.DefinitionId, out var hero) || !hero.AbilityIds.Contains(action.AbilityId) ||
            caster.AbilityRanks.GetValueOrDefault(action.AbilityId) <= 0)
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, "Ability invalid.", "Ownership, kit, stun, or Hero action limit."));
            return state;
        }
        if (caster.IsSilenced)
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, $"{action.CasterId} is silenced.", "Silenced — cannot use abilities."));
            return state;
        }
        if (caster.GetAbilityCooldown(action.AbilityId) > 0)
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, $"{action.AbilityId} is on cooldown.", "Ability on cooldown."));
            return state;
        }

        // Look up ability and check energy cost
        AbilityDefinition? abilityDef = null;
        int energyCost = 0;
        if (_data.Abilities.TryGetValue(action.AbilityId, out abilityDef))
        {
            energyCost = abilityDef.Adapted.EnergyCost;
            if (caster.CurrentEnergy < energyCost)
            {
                events.Add(new InvalidActionEvent(state.TurnNumber,
                    $"{action.CasterId} has insufficient energy for {action.AbilityId}.",
                    $"Need {energyCost} energy, have {caster.CurrentEnergy}."));
                return state;
            }
        }

        events.Add(new AbilityUsedEvent(state.TurnNumber,
            $"{action.CasterId} uses {action.AbilityId}" +
            (abilityDef is not null ? $" [{abilityDef.Name}]" : "") +
            (action.TargetId is not null ? $" on {action.TargetId}" : "") + ".",
            action.CasterId, action.AbilityId, action.TargetId));

        // 8: Deduct energy cost
        var newState = energyCost > 0
            ? UpdateUnit(state, action.CasterId, u => u with { CurrentEnergy = u.CurrentEnergy - energyCost })
            : state;

        // 9: Execute ability effects (damage, heal, apply status effects)
        if (abilityDef is not null)
        {
            var (stateAfter, execEvents) = AbilityExecutor.Execute(newState, action, _data, _rng);
            newState = stateAfter;
            events.AddRange(execEvents);
        }

        // 11: Set cooldown
        newState = SetCooldown(newState, action.CasterId, action.AbilityId, events);
        return newState;
    }

    private GameState HandleUpgradeAbility(GameState state, UpgradeAbilityAction action, List<GameEvent> events)
    {
        var hero = state.FindUnit(action.HeroId);
        if (hero is null || hero.PlayerId != action.PlayerId ||
            !HeroProgression.CanUpgrade(hero, action.AbilityId, _data))
        {
            events.Add(new InvalidActionEvent(state.TurnNumber, "Ability upgrade invalid.",
                "Ownership, level, points, kit, or rank limit."));
            return state;
        }
        return HeroProgression.Upgrade(state, action.HeroId, action.AbilityId, _data, events);
    }

    private GameState HandleChangeRow(GameState state, ChangeRowAction action, List<GameEvent> events)
    {
        // TODO: row change logic (move cost, restrictions)
        return state;
    }

    private GameState HandlePass(GameState state, PassAction action, List<GameEvent> events)
    {
        events.Add(new TurnStartEvent(state.TurnNumber, $"Player {action.PlayerId} passes.", action.PlayerId));
        return state;
    }

    // ─── COMBAT MATH ────────────────────────────────────────────────────────

    /// <summary>
    /// HoN physical armor damage reduction formula.
    /// damage_taken = damage * (1 - armor_factor)
    /// armor_factor = armor * 0.052 / (1 + armor * 0.052)  [standard MOBA formula]
    /// Negative armor increases damage taken.
    /// </summary>
    public static int ApplyPhysicalArmor(int rawDamage, float armor)
    {
        float factor = armor >= 0
            ? (armor * 0.052f) / (1f + armor * 0.052f)
            : -((-armor) * 0.052f) / (1f + (-armor) * 0.052f);
        float taken = rawDamage * (1f - factor);
        return Math.Max(0, (int)MathF.Round(taken));
    }

    /// <summary>
    /// HoN magic armor damage reduction formula.
    /// damage_taken = damage * (1 - magic_armor / (magic_armor + 5.5))
    /// Base magic armor of 5 = ~47.6% reduction.
    /// </summary>
    public static int ApplyMagicArmor(int rawDamage, float magicArmor)
    {
        float taken = rawDamage * (1f - magicArmor / (magicArmor + 5.5f));
        return Math.Max(0, (int)MathF.Round(taken));
    }

    // ─── STATE MUTATION HELPERS ─────────────────────────────────────────────

    private static GameState ApplyDamageToUnit(GameState state, string targetId, int damage, List<GameEvent> events)
    {
        var unit  = state.FindUnit(targetId);
        if (unit is null) return state;
        int newHp = Math.Max(0, unit.CurrentHp - damage);
        var updated = unit with { CurrentHp = newHp };
        if (updated.IsDead)
            events.Add(new UnitDiedEvent(state.TurnNumber, $"{targetId} has died.", targetId, unit.PlayerId));
        return StateUtils.UpdateUnit(state, updated);
    }

    private GameState SetCooldown(GameState state, string unitId, string abilityId, List<GameEvent> events)
    {
        int turns = _data.Abilities.TryGetValue(abilityId, out var abilityDef)
            ? abilityDef.Adapted.CooldownTurns
            : 2; // fallback

        return UpdateUnit(state, unitId, unit =>
        {
            var newCooldowns = unit.Cooldowns
                .Where(c => c.AbilityId != abilityId)
                .Append(new AbilityCooldown(abilityId, turns))
                .ToList();
            return unit with { Cooldowns = newCooldowns };
        });
    }

    private static GameState UpdateUnit(GameState state, string unitId, Func<UnitState, UnitState> updater)
    {
        var newPlayers = state.Players.Select(player =>
        {
            var newUnits = player.Units.Select(u => u.Id == unitId ? updater(u) : u).ToList();
            return player with { Units = newUnits };
        }).ToList();

        return state with { Players = newPlayers };
    }

    private IEnumerable<UnitState> TickUnits(IReadOnlyList<UnitState> units, int turn, List<GameEvent> events)
    {
        foreach (var unit in units)
        {
            if (unit.IsDead) { yield return unit; continue; }

            // Tick cooldowns
            var newCooldowns = unit.Cooldowns
                .Select(c => c with { TurnsRemaining = c.TurnsRemaining - 1 })
                .Where(c => c.TurnsRemaining > 0)
                .ToList();

            // Tick effects — delegates DoT damage and expiry to EffectTicker
            var (tickedUnit, tickEvents) = EffectTicker.Tick(unit, _data, turn, _rng);
            events.AddRange(tickEvents);

            yield return tickedUnit with { Cooldowns = newCooldowns };
        }
    }

    private GameState CheckVictory(GameState state, List<GameEvent> events)
    {
        var defeated = state.Players.Where(p => p.IsDefeated).ToList();
        if (!defeated.Any()) return state;

        // Win condition: last player with heroes alive wins
        var winner = state.Players.FirstOrDefault(p => !p.IsDefeated);
        if (winner is null) return state; // draw — all defeated same turn

        events.Add(new GameOverEvent(state.TurnNumber,
            $"Player {winner.PlayerId} wins! All enemy heroes defeated.", winner.PlayerId));

        return state with { IsGameOver = true, WinnerId = winner.PlayerId };
    }
}
