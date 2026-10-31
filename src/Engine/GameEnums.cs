namespace ChampionsOfAshenveil.Engine;

/// <summary>
/// Damage types in the game. Corresponds to HoN's physical/magic/true damage model.
/// </summary>
public enum DamageType
{
    Physical,
    Magic,
    True      // bypasses all armor and magic armor
}

/// <summary>
/// Attack type determines row targeting rules (melee vs ranged).
/// </summary>
public enum AttackType
{
    Melee,
    Ranged
}

/// <summary>
/// The three primary attributes. Each attribute contributes to derived stats (see AttributeConstants).
/// </summary>
public enum PrimaryAttribute
{
    Strength,
    Agility,
    Intelligence
}

/// <summary>
/// Combat row positions. Front row is required for melee attackers; ranged can target either.
/// </summary>
public enum RowPosition
{
    Front,
    Back
}

/// <summary>
/// Status effect types. Based on HoN CC system (section 6 of the plan).
/// </summary>
[Flags]
public enum StatusFlags
{
    None        = 0,
    Stunned     = 1 << 0,  // cannot act, cannot move
    Silenced    = 1 << 1,  // cannot use abilities
    Disarmed    = 1 << 2,  // cannot attack
    Immobilized = 1 << 3,  // cannot move/change row (but can act)
    Slowed      = 1 << 4,  // movement penalty (affects row-change cost)
    Rooted      = 1 << 5,  // alias for Immobilize with Restrain semantics
    DoT         = 1 << 6,  // damage over time active
    Shield      = 1 << 7,  // damage absorb active
    Invisible   = 1 << 8,
}

/// <summary>
/// Game-wide constants derived from the verified HoN Reborn source files.
/// Source: research/02_EXTRACTED/hon_reborn_constants.json (status: verified).
/// </summary>
public static class AttributeConstants
{
    // Strength
    public const float HpPerStrength       = 21.0f;  // Reborn; classic 4.x was 19 — version difference confirmed
    public const float HpRegenPerStrength  = 0.08f;  // per second in HoN; per turn in card game (to be scaled by T)

    // Intelligence
    public const float ManaPerIntelligence      = 13.0f;
    public const float ManaRegenPerIntelligence = 0.05f;

    // Agility
    public const float ArmorPerAgility      = 0.14f;
    public const float AttackSpeedPerAgility = 0.01f;  // +1% attack speed per point

    // Primary attribute → bonus damage
    public const float BaseDamagePerPrimaryAttributePoint = 1.0f;

    // Base attack cooldown in HoN (milliseconds). Used to derive attacks-per-turn.
    public const int BaseAttackCooldownMs = 1700;
}
