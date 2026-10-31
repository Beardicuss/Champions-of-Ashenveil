using System.Text.Json.Serialization;
using System.Text.Json;

namespace ChampionsOfAshenveil.Engine.Data;

// ─────────────────────────────────────────────────────────────────────────────
//  DATA LAYER — JSON-serialisable definitions loaded at runtime from /data/.
//  Two-layer rule (plan section 2, rule 3):
//    • "Source" block = original HoN reference values. NEVER overwritten.
//    • "Adapted" block = card-game values. All changes recorded with reasons.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Tracks where a value came from and how reliable it is.</summary>
public class SourceInfo
{
    [JsonPropertyName("hon_id")]    public string HonId    { get; set; } = "unknown";
    [JsonPropertyName("version")]   public string Version  { get; set; } = "unknown";
    /// <summary>verified | partially_verified | reconstructed | community_documented | historical | unknown</summary>
    [JsonPropertyName("status")]    public string Status   { get; set; } = "unknown";
    [JsonPropertyName("notes")]     public string Notes    { get; set; } = "";
}

// ─── ABILITY ─────────────────────────────────────────────────────────────────

/// <summary>Original HoN ability parameters (never overwrite).</summary>
public class AbilityOriginal
{
    [JsonPropertyName("cooldown_ms")]  public JsonElement? CooldownMs { get; set; }
    [JsonPropertyName("mana_cost")]    public JsonElement? ManaCost { get; set; }
    [JsonPropertyName("damage")]       public JsonElement? Damage { get; set; }
    [JsonPropertyName("cast_range")]   public JsonElement? CastRange { get; set; }
    [JsonPropertyName("radius")]       public JsonElement? Radius { get; set; }
    [JsonPropertyName("duration_ms")]  public JsonElement? DurationMs { get; set; }
    [JsonPropertyName("max_level")]    public int    MaxLevel    { get; set; } = 4;
}

/// <summary>Card-game adapted values, derived from the originals via TimeQuantum.</summary>
public class AbilityAdapted
{
    [JsonPropertyName("cooldown_turns")]  public int   CooldownTurns  { get; set; }
    [JsonPropertyName("energy_cost")]     public int   EnergyCost     { get; set; }
    [JsonPropertyName("damage")]          public float Damage         { get; set; }
    [JsonPropertyName("duration_turns")]  public int   DurationTurns  { get; set; }
    [JsonPropertyName("targeting")]       public string Targeting     { get; set; } = "single";
    [JsonPropertyName("adaptation_reason")] public string AdaptationReason { get; set; } = "";
}

public class AbilityDefinition
{
    [JsonPropertyName("id")]          public string Id          { get; set; } = "";
    [JsonPropertyName("name")]        public string Name        { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("source")]      public SourceInfo Source  { get; set; } = new();
    [JsonPropertyName("original")]    public AbilityOriginal Original { get; set; } = new();
    [JsonPropertyName("adapted")]     public AbilityAdapted Adapted   { get; set; } = new();
    [JsonPropertyName("effects")]     public List<string> EffectIds   { get; set; } = [];
}

// ─── HERO ────────────────────────────────────────────────────────────────────

/// <summary>Original HoN hero base stats (never overwrite).</summary>
public class HeroOriginal
{
    [JsonPropertyName("hon_alias")]      public string HonAlias     { get; set; } = "unknown";
    [JsonPropertyName("primary")]        public string Primary      { get; set; } = "unknown";
    [JsonPropertyName("str")]            public float Str           { get; set; }
    [JsonPropertyName("str_per_level")]  public float StrPerLevel   { get; set; }
    [JsonPropertyName("agi")]            public float Agi           { get; set; }
    [JsonPropertyName("agi_per_level")]  public float AgiPerLevel   { get; set; }
    [JsonPropertyName("int")]            public float Int           { get; set; }
    [JsonPropertyName("int_per_level")]  public float IntPerLevel   { get; set; }
    [JsonPropertyName("attack_type")]    public string AttackType   { get; set; } = "melee";
    [JsonPropertyName("attack_range")]   public float AttackRange   { get; set; }
    [JsonPropertyName("dmg_min")]        public float DmgMin        { get; set; }
    [JsonPropertyName("dmg_max")]        public float DmgMax        { get; set; }
    [JsonPropertyName("move_speed")]     public float MoveSpeed     { get; set; }
    [JsonPropertyName("armor")]          public float Armor         { get; set; }
    [JsonPropertyName("magic_armor")]    public float MagicArmor    { get; set; }
    [JsonPropertyName("health_regen")]   public float HealthRegen   { get; set; }
    [JsonPropertyName("category")]       public string Category     { get; set; } = "";
}

/// <summary>Card-game adapted hero stats.</summary>
public class HeroAdapted
{
    [JsonPropertyName("max_hp")]            public int MaxHp           { get; set; }
    [JsonPropertyName("max_energy")]        public int MaxEnergy       { get; set; }
    [JsonPropertyName("base_damage_min")]   public int BaseDamageMin   { get; set; }
    [JsonPropertyName("base_damage_max")]   public int BaseDamageMax   { get; set; }
    [JsonPropertyName("armor")]             public float Armor         { get; set; }
    [JsonPropertyName("magic_armor")]       public float MagicArmor    { get; set; }
    [JsonPropertyName("hp_regen_per_turn")] public float HpRegenPerTurn { get; set; }
    [JsonPropertyName("attack_type")]       public string AttackType   { get; set; } = "melee";
    [JsonPropertyName("row_preference")]    public string RowPreference { get; set; } = "front";
    [JsonPropertyName("adaptation_reason")] public string AdaptationReason { get; set; } = "";
}

public class HeroDefinition
{
    [JsonPropertyName("id")]          public string Id          { get; set; } = "";
    [JsonPropertyName("name")]        public string Name        { get; set; } = "";
    [JsonPropertyName("source")]      public SourceInfo Source  { get; set; } = new();
    [JsonPropertyName("original")]    public HeroOriginal Original { get; set; } = new();
    [JsonPropertyName("adapted")]     public HeroAdapted Adapted   { get; set; } = new();
    [JsonPropertyName("ability_ids")] public List<string> AbilityIds { get; set; } = [];
}

// ─── EFFECT ──────────────────────────────────────────────────────────────────

/// <summary>
/// Data-driven status effect definition.
/// Stacking, refresh, dispel and immunity rules are all fields — not code branches.
/// (Plan section 7: "Effects are data-driven.")
/// </summary>
public class EffectDefinition
{
    [JsonPropertyName("id")]             public string Id             { get; set; } = "";
    [JsonPropertyName("name")]           public string Name           { get; set; } = "";
    [JsonPropertyName("status_flags")]   public List<string> StatusFlags { get; set; } = [];
    [JsonPropertyName("damage_per_turn")] public float DamagePerTurn { get; set; }
    [JsonPropertyName("damage_type")]    public string DamageType     { get; set; } = "magic";
    [JsonPropertyName("duration_turns")] public int DurationTurns    { get; set; }
    [JsonPropertyName("can_stack")]      public bool CanStack         { get; set; }
    [JsonPropertyName("refreshes")]      public bool Refreshes        { get; set; } = true;
    [JsonPropertyName("dispellable")]    public bool Dispellable      { get; set; } = true;
    [JsonPropertyName("immune_flags")]   public List<string> ImmuneFlags { get; set; } = [];
    [JsonPropertyName("source")]         public SourceInfo Source     { get; set; } = new();
}

// ─── ITEM ─────────────────────────────────────────────────────────────────────

public class ItemDefinition
{
    [JsonPropertyName("id")]          public string Id          { get; set; } = "";
    [JsonPropertyName("name")]        public string Name        { get; set; } = "";
    [JsonPropertyName("source")]      public SourceInfo Source  { get; set; } = new();
    [JsonPropertyName("cost")]        public int Cost           { get; set; }
    [JsonPropertyName("stat_bonuses")] public Dictionary<string, float> StatBonuses { get; set; } = [];
    [JsonPropertyName("ability_ids")] public List<string> AbilityIds { get; set; } = [];
    [JsonPropertyName("notes")]       public string Notes       { get; set; } = "";
}

// ─── UNIT ─────────────────────────────────────────────────────────────────────

public class UnitDefinition
{
    [JsonPropertyName("id")]          public string Id          { get; set; } = "";
    [JsonPropertyName("name")]        public string Name        { get; set; } = "";
    [JsonPropertyName("source")]      public SourceInfo Source  { get; set; } = new();
    [JsonPropertyName("max_hp")]      public int MaxHp          { get; set; }
    [JsonPropertyName("damage_min")]  public int DamageMin      { get; set; }
    [JsonPropertyName("damage_max")]  public int DamageMax      { get; set; }
    [JsonPropertyName("armor")]       public float Armor        { get; set; }
    [JsonPropertyName("magic_armor")] public float MagicArmor   { get; set; }
    [JsonPropertyName("attack_type")] public string AttackType  { get; set; } = "melee";
    [JsonPropertyName("ability_ids")] public List<string> AbilityIds { get; set; } = [];
}
