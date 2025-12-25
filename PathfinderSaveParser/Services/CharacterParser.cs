using Newtonsoft.Json.Linq;
using System.Text;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Enhanced character parser that uses RefResolver to handle $ref pointers
/// </summary>
public class EnhancedCharacterParser
{
    private readonly BlueprintLookupService _blueprintLookup;
    private readonly RefResolver _resolver;

    public EnhancedCharacterParser(BlueprintLookupService blueprintLookup, RefResolver resolver)
    {
        _blueprintLookup = blueprintLookup;
        _resolver = resolver;
    }

    public List<string> ParseAllCharacters(JToken partyJson)
    {
        var reports = new List<string>();
        var units = partyJson["m_EntityData"];
        
        if (units == null) return reports;

        foreach (var rawUnit in units)
        {
            try
            {
                var report = ParseSingleCharacter(rawUnit);
                if (!string.IsNullOrEmpty(report))
                {
                    reports.Add(report);
                }
            }
            catch (Exception ex)
            {
                // Skip units that can't be parsed
                Console.WriteLine($"Warning: Failed to parse unit: {ex.Message}");
            }
        }

        return reports;
    }

    private string? ParseSingleCharacter(JToken rawUnit)
    {
        // Resolve the unit reference
        var unit = _resolver.Resolve(rawUnit);
        if (unit == null) return null;

        // Resolve the descriptor reference (critical for companions!)
        var descriptor = _resolver.Resolve(unit["Descriptor"]);
        if (descriptor == null) return null;

        // Get character name - prefer CustomName if available
        string? customName = descriptor["CustomName"]?.ToString();
        string? blueprintId = descriptor["Blueprint"]?.ToString();
        
        string? charName;
        if (!string.IsNullOrEmpty(customName))
        {
            // Use custom name and add blueprint name in parentheses
            string blueprintName = _blueprintLookup.GetName(blueprintId ?? "");
            charName = $"{customName} ({blueprintName})";
        }
        else
        {
            // Use blueprint name only
            charName = _blueprintLookup.GetName(blueprintId ?? "");
        }

        // Filter out units without progression (non-playable entities)
        var progression = descriptor["Progression"];
        if (progression == null) return null;

        // Filter out technical/unnamed units
        if (string.IsNullOrEmpty(charName) || charName == "None") return null;
        if (charName.StartsWith("Blueprint_") && progression == null) return null;

        var sb = new StringBuilder();
        sb.AppendLine("=== CHARACTER BUILD ANALYSIS ===");
        sb.AppendLine();
        sb.AppendLine($"Character Name: {charName}");
        sb.AppendLine();

        // Build Summary
        sb.AppendLine("BUILD SUMMARY");
        sb.AppendLine(new string('=', 80));

        // Stats
        var statsRef = descriptor["Stats"];
        var stats = _resolver.Resolve(statsRef);
        if (stats != null)
        {
            sb.Append("Stats: ");
            sb.Append($"Str {GetStatValue(stats, "Strength")}, ");
            sb.Append($"Dex {GetStatValue(stats, "Dexterity")}, ");
            sb.Append($"Con {GetStatValue(stats, "Constitution")}, ");
            sb.Append($"Int {GetStatValue(stats, "Intelligence")}, ");
            sb.Append($"Wis {GetStatValue(stats, "Wisdom")}, ");
            sb.Append($"Cha {GetStatValue(stats, "Charisma")}");
            sb.AppendLine();
            sb.AppendLine();
        }

        // Race
        string? raceId = progression["m_Race"]?.ToString();
        if (!string.IsNullOrEmpty(raceId))
        {
            sb.AppendLine($"Race - {_blueprintLookup.GetName(raceId)}");
        }

        // Classes
        var classes = progression["Classes"];
        if (classes != null && classes.HasValues)
        {
            sb.Append("Class - ");
            var classParts = new List<string>();
            foreach (var c in classes)
            {
                string? classId = c["CharacterClass"]?.ToString();
                string? level = c["Level"]?.ToString();
                string className = _blueprintLookup.GetName(classId ?? "");
                
                var classPart = $"{className} {level}";
                
                var archetypes = c["Archetypes"];
                if (archetypes != null && archetypes.HasValues)
                {
                    var archNames = new List<string>();
                    foreach (var arch in archetypes)
                    {
                        archNames.Add(_blueprintLookup.GetName(arch.ToString()));
                    }
                    if (archNames.Any())
                    {
                        classPart += $" ({string.Join(", ", archNames)})";
                    }
                }
                
                classParts.Add(classPart);
            }
            sb.AppendLine(string.Join(", ", classParts));
        }

        sb.AppendLine();
        sb.AppendLine();

        // Equipment
        try
        {
            var equipmentParser = new EquipmentParser(_blueprintLookup, _resolver);
            var equipmentReport = equipmentParser.ParseEquipment(descriptor);
            if (!string.IsNullOrEmpty(equipmentReport))
            {
                sb.AppendLine(equipmentReport);
                sb.AppendLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to parse equipment: {ex.Message}");
            Console.WriteLine($"  Stack: {ex.StackTrace?.Split('\n')[0]}");
        }

        // Level-by-Level History
        sb.AppendLine("LEVEL-BY-LEVEL BUILD HISTORY");
        sb.AppendLine(new string('=', 80));
        
        ParseHistory(progression["m_Selections"], sb);

        return sb.ToString();
    }

    private string GetStatValue(JToken stats, string statName)
    {
        var statObj = _resolver.Resolve(stats[statName]);
        return statObj?["PermanentValue"]?.ToString() ?? "?";
    }

    private void ParseHistory(JToken? selections, StringBuilder sb)
    {
        if (selections == null) return;

        var historyMap = new SortedDictionary<int, List<string>>();

        foreach (var item in selections)
        {
            var selectionValue = item["Value"];
            var selectionsByLevel = selectionValue?["m_SelectionsByLevel"];

            if (selectionsByLevel != null)
            {
                foreach (var levelEntry in selectionsByLevel)
                {
                    int level = (int?)levelEntry["Key"] ?? 0;
                    var featureGuids = levelEntry["Value"];

                    if (!historyMap.ContainsKey(level))
                    {
                        historyMap[level] = new List<string>();
                    }

                    if (featureGuids != null)
                    {
                        foreach (var featGuid in featureGuids)
                        {
                            string guid = featGuid.ToString();
                            string name = _blueprintLookup.GetName(guid);
                            
                            // Only add recognized features
                            if (!string.IsNullOrEmpty(name) && 
                                name != "None" && 
                                !name.StartsWith("Blueprint_"))
                            {
                                historyMap[level].Add(name);
                            }
                        }
                    }
                }
            }
        }

        // Get class info for level display
        foreach (var kvp in historyMap)
        {
            if (kvp.Value.Count > 0)
            {
                sb.AppendLine($"Level {kvp.Key}: {string.Join(", ", kvp.Value)}");
            }
        }

        if (!historyMap.Any())
        {
            sb.AppendLine("(No level-by-level data available)");
        }
    }
}
