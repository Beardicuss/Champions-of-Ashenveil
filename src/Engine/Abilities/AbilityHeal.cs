namespace ChampionsOfAshenveil.Engine.Abilities;

/// <summary>
/// Heals a unit up to its MaxHp cap.
/// </summary>
public static class AbilityHeal
{
    public static GameState Heal(
        GameState state, string sourceId, string targetId,
        int amount, int turn, List<GameEvent> events)
    {
        var target = state.FindUnit(targetId);
        if (target is null || target.IsDead) return state;

        int healed = Math.Min(amount, target.MaxHp - target.CurrentHp);
        if (healed <= 0) return state;

        events.Add(new HealEvent(turn,
            $"{targetId} healed for {healed} HP (→ {target.CurrentHp + healed}/{target.MaxHp} HP).",
            sourceId, targetId, healed));

        return StateUtils.UpdateUnit(state, target with { CurrentHp = target.CurrentHp + healed });
    }
}
