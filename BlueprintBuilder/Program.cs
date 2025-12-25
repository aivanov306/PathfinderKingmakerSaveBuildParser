using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.WriteLine("Building comprehensive blueprint dictionary...");

var blueprints = new Dictionary<string, string>();

// Look for any folder starting with "Blueprints" in the parent directory (solution root)
var currentDir = Directory.GetCurrentDirectory();
var solutionRoot = Path.Combine(currentDir, "..");
var solutionRootFullPath = Path.GetFullPath(solutionRoot);

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
                
            blueprints[guid] = normalizedName;
            count++;
        }
    }
}

Console.WriteLine($"Extracted {count} blueprints");

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

var jsonContent = JsonSerializer.Serialize(blueprints, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(fullPath, jsonContent);
Console.WriteLine($"JSON database saved to:");
Console.WriteLine($"  - {fullPath}");
Console.WriteLine($"\nTotal blueprints: {blueprints.Count}");
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

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
    if (name.Contains("_Old") || name.Contains("_Copy") || name.Contains("Deprecated") || 
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
    if (name.Length <= 4)
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
