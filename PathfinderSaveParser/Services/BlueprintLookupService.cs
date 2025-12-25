using System.Text.Json;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Service to resolve Pathfinder blueprint GUIDs to human-readable names.
/// Loads comprehensive blueprint database from JSON file.
/// </summary>
public class BlueprintLookupService
{
    private readonly Dictionary<string, string> _blueprintNames;

    public BlueprintLookupService()
    {
        // Try to load comprehensive blueprint database
        var dbPath = Path.Combine(AppContext.BaseDirectory, "blueprint_database.json");
        if (File.Exists(dbPath))
        {
            try
            {
                var json = File.ReadAllText(dbPath);
                var database = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (database != null)
                {
                    _blueprintNames = database;
                    Console.WriteLine($"Loaded {_blueprintNames.Count} blueprints from database.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load blueprint database: {ex.Message}");
            }
        }
        
        Console.WriteLine("Warning: Blueprint database not found. Names will be generic.");
        _blueprintNames = new Dictionary<string, string>();
    }

    public string GetName(string? blueprintId)
    {
        if (string.IsNullOrEmpty(blueprintId))
            return "Unknown";

        return _blueprintNames.TryGetValue(blueprintId, out var name) ? name : $"Blueprint_{blueprintId[..8]}";
    }

    public void AddCustomMapping(string blueprintId, string name)
    {
        _blueprintNames[blueprintId] = name;
    }

    public Dictionary<string, string> GetAllMappings()
    {
        return new Dictionary<string, string>(_blueprintNames);
    }
}
