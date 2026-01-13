using System.Text.Json;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Service to resolve Pathfinder blueprint GUIDs to human-readable names and equipment types.
/// Loads comprehensive blueprint database from JSON file.
/// </summary>
public class BlueprintLookupService
{
    private readonly Dictionary<string, string> _blueprintNames;
    private readonly Dictionary<string, string> _equipmentTypes;
    private readonly Dictionary<string, string> _blueprintTypes;
    private readonly Dictionary<string, string> _blueprintDescriptions;

    public BlueprintLookupService()
    {
        // Try to load comprehensive blueprint database
        var dbPath = Path.Combine(AppContext.BaseDirectory, "blueprint_database.json");
        if (File.Exists(dbPath))
        {
            try
            {
                var json = File.ReadAllText(dbPath);
                
                // Try to deserialize as new format (with separate Names and EquipmentTypes)
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("Names", out var namesElement) && 
                        root.TryGetProperty("EquipmentTypes", out var typesElement))
                    {
                        // New format with separate dictionaries
                        _blueprintNames = JsonSerializer.Deserialize<Dictionary<string, string>>(namesElement.GetRawText()) ?? new();
                        _equipmentTypes = JsonSerializer.Deserialize<Dictionary<string, string>>(typesElement.GetRawText()) ?? new();
                        
                        // Try to load BlueprintTypes if available (newer format)
                        if (root.TryGetProperty("BlueprintTypes", out var blueprintTypesElement))
                        {
                            _blueprintTypes = JsonSerializer.Deserialize<Dictionary<string, string>>(blueprintTypesElement.GetRawText()) ?? new();
                        }
                        else
                        {
                            _blueprintTypes = new Dictionary<string, string>();
                        }
                        
                        // Try to load Descriptions if available (newest format)
                        if (root.TryGetProperty("Descriptions", out var descriptionsElement))
                        {
                            _blueprintDescriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionsElement.GetRawText()) ?? new();
                            Console.WriteLine($"Loaded {_blueprintNames.Count} blueprints, {_equipmentTypes.Count} equipment types, {_blueprintTypes.Count} blueprint types, and {_blueprintDescriptions.Count} descriptions from database.");
                        }
                        else
                        {
                            _blueprintDescriptions = new Dictionary<string, string>();
                            Console.WriteLine($"Loaded {_blueprintNames.Count} blueprints, {_equipmentTypes.Count} equipment types, and {_blueprintTypes.Count} blueprint types from database.");
                        }
                        return;
                    }
                }
                catch
                {
                    // Fall through to try old formats
                }
                
                // Try old format (simple string dictionary)
                try
                {
                    var oldDatabase = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (oldDatabase != null)
                    {
                        _blueprintNames = oldDatabase;
                        _equipmentTypes = new Dictionary<string, string>();
                        _blueprintTypes = new Dictionary<string, string>();
                        _blueprintDescriptions = new Dictionary<string, string>();
                        Console.WriteLine($"Loaded {_blueprintNames.Count} blueprints from database (legacy format, no equipment types).");
                        return;
                    }
                }
                catch
                {
                    // Ignore and continue
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load blueprint database: {ex.Message}");
            }
        }
        
        Console.WriteLine("Warning: Blueprint database not found. Names will be generic.");
        _blueprintNames = new Dictionary<string, string>();
        _equipmentTypes = new Dictionary<string, string>();
        _blueprintTypes = new Dictionary<string, string>();
        _blueprintDescriptions = new Dictionary<string, string>();
    }

    public string GetName(string? blueprintId)
    {
        if (string.IsNullOrEmpty(blueprintId))
            return "Unknown";

        return _blueprintNames.TryGetValue(blueprintId, out var name) ? name : $"Blueprint_{blueprintId[..8]}";
    }
    
    public string? GetEquipmentType(string? blueprintId)
    {
        if (string.IsNullOrEmpty(blueprintId))
            return null;

        return _equipmentTypes.TryGetValue(blueprintId, out var type) ? type : null;
    }
    
    public (string Name, string? Type) GetNameAndType(string? blueprintId)
    {
        if (string.IsNullOrEmpty(blueprintId))
            return ("Unknown", null);

        var name = _blueprintNames.TryGetValue(blueprintId, out var n) ? n : $"Blueprint_{blueprintId[..8]}";
        var type = _equipmentTypes.TryGetValue(blueprintId, out var t) ? t : null;
        
        return (name, type);
    }
    
    /// <summary>
    /// Get blueprint type from database (e.g., BlueprintItem, BlueprintItemWeapon, BlueprintItemEquipmentRing)
    /// </summary>
    public string? GetBlueprintType(string? blueprintId)
    {
        if (string.IsNullOrEmpty(blueprintId))
            return null;

        return _blueprintTypes.TryGetValue(blueprintId, out var type) ? type : null;
    }
    
    /// <summary>
    /// Get item description from database
    /// </summary>
    public string? GetDescription(string? blueprintId)
    {
        if (string.IsNullOrEmpty(blueprintId))
            return null;

        return _blueprintDescriptions.TryGetValue(blueprintId, out var desc) ? desc : null;
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
