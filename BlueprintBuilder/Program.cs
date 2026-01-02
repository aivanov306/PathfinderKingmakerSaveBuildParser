using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.WriteLine("Building comprehensive blueprint dictionary...");

// Store blueprint names
var blueprintNames = new Dictionary<string, string>();

// Store equipment types separately (only for weapons, armor, shields)
var equipmentTypes = new Dictionary<string, string>();

// Look for any folder starting with "Blueprints" in the parent directory (solution root)
var currentDir = Directory.GetCurrentDirectory();

// When run with dotnet run, we're in BlueprintBuilder folder
// When run from bin, we're in bin/Release/net8.0 folder
// Need to find solution root
string solutionRootFullPath;
if (currentDir.EndsWith("BlueprintBuilder"))
{
    // Running with dotnet run from BlueprintBuilder folder
    solutionRootFullPath = Path.GetFullPath(Path.Combine(currentDir, ".."));
}
else if (currentDir.Contains("BlueprintBuilder"))
{
    // Running from bin folder
    var blueprintBuilderIndex = currentDir.IndexOf("BlueprintBuilder");
    solutionRootFullPath = currentDir.Substring(0, blueprintBuilderIndex);
}
else
{
    // Running from solution root
    solutionRootFullPath = currentDir;
}

Console.WriteLine($"Solution root: {solutionRootFullPath}");

// Find any directory starting with "Blueprints"
var blueprintsDirs = Directory.GetDirectories(solutionRootFullPath, "Blueprints*");

if (blueprintsDirs.Length == 0)
{
    Console.WriteLine("Error: No folders starting with 'Blueprints' found in solution root!");
    Console.WriteLine("Please create a folder like 'Blueprints2.1.4' with Blueprints.txt inside.");
    return;
}

// Use the first matching directory
var blueprintsDir = blueprintsDirs[0];
var blueprintsFile = Path.Combine(blueprintsDir, "Blueprints.txt");

Console.WriteLine($"Found blueprints folder: {Path.GetFileName(blueprintsDir)}");
Console.WriteLine($"Looking for: {blueprintsFile}");

if (!File.Exists(blueprintsFile))
{
    Console.WriteLine($"Error: {blueprintsFile} not found!");
    return;
}

// Parse the main Blueprints.txt file
var lines = File.ReadAllLines(blueprintsFile);
Console.WriteLine($"Processing {lines.Length} blueprint entries...");

int count = 0;
foreach (var line in lines.Skip(1)) // Skip header
{
    var parts = line.Split('\t');
    if (parts.Length >= 2)
    {
        var name = parts[0].Trim();
        var guid = parts[1].Trim();
        
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(guid) && guid.Length == 32)
        {
            // Filter out unnecessary blueprints
            if (ShouldSkipBlueprint(name))
                continue;
            
            // Normalize the name by adding spaces between words
            var normalizedName = NormalizeBlueprintName(name);
                
            blueprintNames[guid] = normalizedName;
            count++;
        }
    }
}

Console.WriteLine($"Extracted {count} blueprints");

// Now extract weapon, armor, and shield type information from JSON files
Console.WriteLine("\nExtracting equipment type information from JSON files...");
ExtractEquipmentTypes(blueprintsDir, equipmentTypes);

// Determine output path: PathfinderSaveParser folder if exists, otherwise current directory
var parserProjectPath = Path.Combine(solutionRootFullPath, "PathfinderSaveParser");
string outputPath;

if (Directory.Exists(parserProjectPath))
{
    outputPath = Path.Combine(parserProjectPath, "blueprint_database.json");
    Console.WriteLine($"PathfinderSaveParser folder found, saving to project directory...");
}
else
{
    outputPath = Path.Combine(solutionRootFullPath, "blueprint_database.json");
    Console.WriteLine($"PathfinderSaveParser folder not found, saving to solution root...");
}

var fullPath = Path.GetFullPath(outputPath);

// Ensure the directory exists
Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

// Create combined database with both dictionaries
var database = new
{
    Names = blueprintNames,
    EquipmentTypes = equipmentTypes
};

var jsonContent = JsonSerializer.Serialize(database, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(fullPath, jsonContent);
Console.WriteLine($"JSON database saved to:");
Console.WriteLine($"  - {fullPath}");
Console.WriteLine($"\nTotal blueprints: {blueprintNames.Count}");
Console.WriteLine($"Equipment types: {equipmentTypes.Count}");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

void ExtractEquipmentTypes(string blueprintsDir, Dictionary<string, string> equipmentTypes)
{
    int weaponTypesFound = 0;
    int armorTypesFound = 0;
    int shieldTypesFound = 0;

    // Process weapon blueprints
    var weaponDir = Path.Combine(blueprintsDir, "Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon");
    if (Directory.Exists(weaponDir))
    {
        weaponTypesFound = ProcessWeaponFiles(weaponDir, equipmentTypes);
        Console.WriteLine($"  Processed {weaponTypesFound} weapon types");
    }

    // Process armor blueprints
    var armorDir = Path.Combine(blueprintsDir, "Kingmaker.Blueprints.Items.Armors.BlueprintItemArmor");
    if (Directory.Exists(armorDir))
    {
        armorTypesFound = ProcessArmorFiles(armorDir, equipmentTypes);
        Console.WriteLine($"  Processed {armorTypesFound} armor types");
    }

    // Process shield blueprints - shields have m_ArmorComponent that points to armor blueprint with type
    var shieldDir = Path.Combine(blueprintsDir, "Kingmaker.Blueprints.Items.Shields.BlueprintItemShield");
    if (Directory.Exists(shieldDir))
    {
        shieldTypesFound = ProcessShieldFiles(shieldDir, equipmentTypes, blueprintsDir);
        Console.WriteLine($"  Processed {shieldTypesFound} shield types");
    }

    Console.WriteLine($"Total equipment types extracted: {weaponTypesFound + armorTypesFound + shieldTypesFound}");
}

int ProcessWeaponFiles(string weaponDir, Dictionary<string, string> equipmentTypes)
{
    int count = 0;
    foreach (var jsonFile in Directory.GetFiles(weaponDir, "*.json"))
    {
        try
        {
            var json = File.ReadAllText(jsonFile);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract weapon GUID from filename (format: Name.GUID.json)
            var fileName = Path.GetFileNameWithoutExtension(jsonFile);
            var parts = fileName.Split('.');
            if (parts.Length >= 2)
            {
                var guid = parts[parts.Length - 1];
                
                // Look for m_Type field
                if (root.TryGetProperty("m_Type", out var typeElement))
                {
                    var typeString = typeElement.GetString();
                    if (!string.IsNullOrEmpty(typeString))
                    {
                        // Format: "Blueprint:GUID:TypeName"
                        var typeParts = typeString.Split(':');
                        if (typeParts.Length >= 3)
                        {
                            var typeName = typeParts[2];
                            equipmentTypes[guid] = NormalizeBlueprintName(typeName);
                            count++;
                        }
                    }
                }
            }
        }
        catch
        {
            // Skip files that can't be parsed
        }
    }
    return count;
}

int ProcessArmorFiles(string armorDir, Dictionary<string, string> equipmentTypes)
{
    int count = 0;
    foreach (var jsonFile in Directory.GetFiles(armorDir, "*.json"))
    {
        try
        {
            var json = File.ReadAllText(jsonFile);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract armor GUID from filename
            var fileName = Path.GetFileNameWithoutExtension(jsonFile);
            var parts = fileName.Split('.');
            if (parts.Length >= 2)
            {
                var guid = parts[parts.Length - 1];
                
                // Look for m_Type field
                if (root.TryGetProperty("m_Type", out var typeElement))
                {
                    var typeString = typeElement.GetString();
                    if (!string.IsNullOrEmpty(typeString))
                    {
                        var typeParts = typeString.Split(':');
                        if (typeParts.Length >= 3)
                        {
                            var typeName = typeParts[2];
                            // Remove "Type" suffix if present (e.g., "ChainmailType" -> "Chainmail")
                            if (typeName.EndsWith("Type"))
                                typeName = typeName.Substring(0, typeName.Length - 4);
                            
                            equipmentTypes[guid] = NormalizeBlueprintName(typeName);
                            count++;
                        }
                    }
                }
            }
        }
        catch
        {
            // Skip files that can't be parsed
        }
    }
    return count;
}

int ProcessShieldFiles(string shieldDir, Dictionary<string, string> equipmentTypes, string blueprintsDir)
{
    int count = 0;
    foreach (var jsonFile in Directory.GetFiles(shieldDir, "*.json"))
    {
        try
        {
            var json = File.ReadAllText(jsonFile);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract shield GUID from filename
            var fileName = Path.GetFileNameWithoutExtension(jsonFile);
            var parts = fileName.Split('.');
            if (parts.Length >= 2)
            {
                var shieldGuid = parts[parts.Length - 1];
                
                // Look for m_ArmorComponent field which points to the armor blueprint
                if (root.TryGetProperty("m_ArmorComponent", out var armorComponentElement))
                {
                    var armorComponentString = armorComponentElement.GetString();
                    if (!string.IsNullOrEmpty(armorComponentString))
                    {
                        // Format: "Blueprint:GUID:Name"
                        var componentParts = armorComponentString.Split(':');
                        if (componentParts.Length >= 2)
                        {
                            var armorGuid = componentParts[1];
                            
                            // Read the armor component file to get its m_Type
                            var armorDir = Path.Combine(blueprintsDir, "Kingmaker.Blueprints.Items.Armors.BlueprintItemArmor");
                            var armorFiles = Directory.GetFiles(armorDir, $"*.{armorGuid}.json");
                            
                            if (armorFiles.Length > 0)
                            {
                                var armorJson = File.ReadAllText(armorFiles[0]);
                                using var armorDoc = JsonDocument.Parse(armorJson);
                                var armorRoot = armorDoc.RootElement;
                                
                                if (armorRoot.TryGetProperty("m_Type", out var typeElement))
                                {
                                    var typeString = typeElement.GetString();
                                    if (!string.IsNullOrEmpty(typeString))
                                    {
                                        var typeParts = typeString.Split(':');
                                        if (typeParts.Length >= 3)
                                        {
                                            var typeName = typeParts[2];
                                            // Remove "Type" suffix (e.g., "HeavyShieldType" -> "Heavy Shield")
                                            if (typeName.EndsWith("Type"))
                                                typeName = typeName.Substring(0, typeName.Length - 4);
                                            
                                            equipmentTypes[shieldGuid] = NormalizeBlueprintName(typeName);
                                            count++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Skip files that can't be parsed
        }
    }
    return count;
}

bool ShouldSkipBlueprint(string name)
{
    // Skip test blueprints (except test abilities which might be real)
    if (name.StartsWith("Test") && !name.Contains("Ability"))
        return true;
    
    // Skip internal/debug entries
    if (name.Contains("Internal") || name.Contains("Debug") || name.Contains("Editor"))
        return true;
    
    // Skip temporary entries
    if (name.StartsWith("Temp") || name.Contains("Temporary"))
        return true;
    
    // Skip old/deprecated/copy entries
    // Check for "_Old" at the end or followed by underscore/number (not part of a word like "OldSycamore")
    if (name.EndsWith("_Old") || name.Contains("_Old_") || name.Contains("_Old1") || 
        name.Contains("_Copy") || name.Contains("Deprecated") || 
        name.Contains("Unused") || name.Contains("Obsolete"))
        return true;
    
    // Skip entries with "UseThisOne" suffix (indicates duplicates)
    if (name.Contains("UseThisOne"))
        return true;
    
    // Skip AI action/condition entries (internal game logic, not player-facing)
    if (name.EndsWith("AiAction") || name.EndsWith("AiCondition"))
        return true;
    
    // Skip cooldown buffs (internal timing mechanics)
    if (name.EndsWith("Cooldown") || name.EndsWith("CooldownBuff"))
        return true;
    
    // Skip "NoBuff" condition entries
    if (name.StartsWith("NoBuff") || name.StartsWith("AlliesNoBuff"))
        return true;
    
    // Skip dialog/cutscene system entries
    if (name.StartsWith("Answer") || name.StartsWith("Answers ") ||
        name.StartsWith("Cue") || name.StartsWith("Check ") ||
        name.StartsWith("Book ") || name.StartsWith("Dialogue ") || 
        name.StartsWith("Banter ") || name.StartsWith("Sequence "))
        return true;
    
    // Skip very short generic names (likely internal identifiers)
    // Changed from <= 4 to <= 3 to allow legitimate 4-letter names like "Shop", "Daze", "Bard", etc.
    if (name.Length <= 3)
        return true;
    
    return false;
}

string NormalizeBlueprintName(string name)
{
    // Remove common suffixes for cleaner names
    if (name.EndsWith("_Companion"))
        name = name.Substring(0, name.Length - "_Companion".Length);
    
    // Replace underscores with spaces
    name = name.Replace("_", " ");
    
    // Add spaces before capital letters (PascalCase to readable format)
    // PowerAttackFeature -> Power Attack Feature
    var withSpaces = Regex.Replace(name, "(?<!^)(?=[A-Z])", " ");
    
    // Handle common abbreviations and numbers
    withSpaces = Regex.Replace(withSpaces, @"(\d+)", " $1"); // Add space before numbers
    withSpaces = Regex.Replace(withSpaces, @"\s+", " "); // Collapse multiple spaces
    
    return withSpaces.Trim();
}
