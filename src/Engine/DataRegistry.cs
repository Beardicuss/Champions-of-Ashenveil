using System.Text.Json;
using ChampionsOfAshenveil.Engine.Data;

namespace ChampionsOfAshenveil.Engine;

/// <summary>
/// Loads all game data definitions from JSON files in /data/.
/// Called once at startup; data is immutable after loading.
/// </summary>
public class DataRegistry
{
    public IReadOnlyDictionary<string, HeroDefinition>    Heroes    { get; private set; } = new Dictionary<string, HeroDefinition>();
    public IReadOnlyDictionary<string, AbilityDefinition> Abilities { get; private set; } = new Dictionary<string, AbilityDefinition>();
    public IReadOnlyDictionary<string, EffectDefinition>  Effects   { get; private set; } = new Dictionary<string, EffectDefinition>();
    public IReadOnlyDictionary<string, ItemDefinition>    Items     { get; private set; } = new Dictionary<string, ItemDefinition>();
    public IReadOnlyDictionary<string, UnitDefinition>    Units     { get; private set; } = new Dictionary<string, UnitDefinition>();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Loads all JSON files from the given data root directory.
    /// Expects subfolders: heroes/, abilities/, effects/, items/, units/
    /// </summary>
    public void Load(string dataRootPath)
    {
        Heroes    = LoadFolder<HeroDefinition>   (Path.Combine(dataRootPath, "heroes"));
        Abilities = LoadFolder<AbilityDefinition>(Path.Combine(dataRootPath, "abilities"));
        Effects   = LoadFolder<EffectDefinition> (Path.Combine(dataRootPath, "effects"));
        Items     = LoadFolder<ItemDefinition>   (Path.Combine(dataRootPath, "items"));
        Units     = LoadFolder<UnitDefinition>   (Path.Combine(dataRootPath, "units"));
    }

    private static Dictionary<string, T> LoadFolder<T>(string folderPath) where T : class
    {
        var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(folderPath)) return result;

        foreach (var file in Directory.EnumerateFiles(folderPath, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(file);
                var obj = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                if (obj is null) continue;

                // Use filename (without extension) as the key
                var key = Path.GetFileNameWithoutExtension(file);
                result[key] = obj;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DataRegistry] Failed to load {file}: {ex.Message}");
            }
        }

        return result;
    }

    public HeroDefinition GetHero(string id) =>
        Heroes.TryGetValue(id, out var h) ? h : throw new KeyNotFoundException($"Hero not found: {id}");

    public AbilityDefinition GetAbility(string id) =>
        Abilities.TryGetValue(id, out var a) ? a : throw new KeyNotFoundException($"Ability not found: {id}");

    public EffectDefinition GetEffect(string id) =>
        Effects.TryGetValue(id, out var e) ? e : throw new KeyNotFoundException($"Effect not found: {id}");
}
