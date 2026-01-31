using Newtonsoft.Json.Linq;
using System.Text;
using PathfinderSaveParser.Models;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Enhanced character parser that uses RefResolver to handle $ref pointers
/// </summary>
public class EnhancedCharacterParser
{
    private readonly BlueprintLookupService _blueprintLookup;
    private readonly RefResolver _resolver;
    private readonly ReportOptions _options;

    public EnhancedCharacterParser(BlueprintLookupService blueprintLookup, RefResolver resolver, ReportOptions options)
    {
        _blueprintLookup = blueprintLookup;
        _resolver = resolver;
        _options = options;
    }

    public List<CharacterReport> ParseAllCharacters(JToken partyJson)
    {
        var reports = new List<CharacterReport>();
        var units = partyJson["m_EntityData"];
        
        if (units == null) return reports;

        foreach (var rawUnit in units)
        {
            try
            {
                var report = ParseSingleCharacter(rawUnit);
                if (report != null)
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

    private CharacterReport? ParseSingleCharacter(JToken rawUnit)
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
        if (_options.IncludeStats)
        {
            var statsRef = descriptor["Stats"];
            var stats = _resolver.Resolve(statsRef);
            if (stats != null)
            {
                sb.Append("Stats (base values + level-up allocated points, without racial/class/item/size modifiers): ");
                sb.Append($"Str {GetStatValue(stats, "Strength")}, ");
                sb.Append($"Dex {GetStatValue(stats, "Dexterity")}, ");
                sb.Append($"Con {GetStatValue(stats, "Constitution")}, ");
                sb.Append($"Int {GetStatValue(stats, "Intelligence")}, ");
                sb.Append($"Wis {GetStatValue(stats, "Wisdom")}, ");
                sb.Append($"Cha {GetStatValue(stats, "Charisma")}");
                sb.AppendLine();
                sb.AppendLine();

                // Skills
                if (_options.IncludeSkills)
                {
                    AppendSkills(stats, sb);
                }
            }
        }

        // Race
        if (_options.IncludeRace)
        {
            string? raceId = progression["m_Race"]?.ToString();
            if (!string.IsNullOrEmpty(raceId))
            {
                sb.AppendLine($"Race - {_blueprintLookup.GetName(raceId)}");
            }
        }

        // Classes
        if (_options.IncludeClass)
        {
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
        }

        sb.AppendLine();
        sb.AppendLine();

        // Level-by-Level History
        if (_options.IncludeLevelHistory)
        {
            sb.AppendLine("LEVEL-BY-LEVEL BUILD HISTORY");
            sb.AppendLine(new string('=', 80));
            
            ParseHistoryWithParams(progression["m_Selections"], progression, sb);

            sb.AppendLine();
            sb.AppendLine();
        }

        // Equipment
        if (_options.IncludeEquipment)
        {
            try
            {
                var equipmentParser = new EquipmentParser(_blueprintLookup, _resolver, _options);
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
        }

        // Spellcasting
        if (_options.IncludeSpellcasting)
        {
            try
            {
                var spellbookParser = new SpellbookParser(_blueprintLookup, _resolver, _options);
                var spellbookReport = spellbookParser.ParseSpellbooks(descriptor);
                if (!string.IsNullOrEmpty(spellbookReport))
                {
                    sb.AppendLine(spellbookReport);
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse spellbooks: {ex.Message}");
                Console.WriteLine($"  Stack: {ex.StackTrace?.Split('\n')[0]}");
            }
        }

        return new CharacterReport
        {
            CharacterName = charName,
            Report = sb.ToString()
        };
    }

    private string GetStatValue(JToken stats, string statName)
    {
        var statObj = _resolver.Resolve(stats[statName]);
        return statObj?["m_BaseValue"]?.ToString() ?? "?";
    }

    private void AppendSkills(JToken stats, StringBuilder sb)
    {
        var skills = new Dictionary<string, int>();

        // Parse each attribute's dependent skills
        foreach (var attributeName in new[] { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" })
        {
            var attrObj = _resolver.Resolve(stats[attributeName]);
            if (attrObj == null) continue;

            var dependents = attrObj["m_Dependents"];
            if (dependents == null || !dependents.HasValues) continue;

            foreach (var dependent in dependents)
            {
                var resolvedDep = _resolver.Resolve(dependent);
                if (resolvedDep == null) continue;

                string? typeName = resolvedDep["$type"]?.ToString();
                string? typeProperty = resolvedDep["Type"]?.ToString();
                
                // Check if it's a skill: either has ModifiableValueSkill in $type, or Type starts with "Skill"
                bool isSkill = (typeName != null && typeName.Contains("ModifiableValueSkill")) ||
                              (typeProperty != null && typeProperty.StartsWith("Skill"));
                              
                if (!isSkill) continue;

                string? skillType = typeProperty;
                int skillValue = (int?)resolvedDep["m_BaseValue"] ?? 0;

                // Map skill types to display names
                string? skillName = skillType switch
                {
                    "SkillMobility" => "Mobility",
                    "SkillAthletics" => "Athletics",
                    "SkillStealth" => "Stealth",
                    "SkillThievery" => "Thievery",
                    "SkillKnowledgeArcana" => "Knowledge (Arcana)",
                    "SkillKnowledgeWorld" => "Knowledge (World)",
                    "SkillLoreNature" => "Lore (Nature)",
                    "SkillLoreReligion" => "Lore (Religion)",
                    "SkillPerception" => "Perception",
                    "SkillPersuasion" => "Persuasion",
                    "SkillUseMagicDevice" => "Use Magic Device",
                    _ => null
                };

                if (skillName != null)
                {
                    skills[skillName] = skillValue;
                }
            }
        }

        if (skills.Any())
        {
            sb.AppendLine("Skills (allocated points, without racial/class/item/size modifiers):");
            foreach (var skill in skills.OrderBy(s => s.Key))
            {
                sb.AppendLine($"  {skill.Key}: {skill.Value:+0;-0;0}");
            }
            sb.AppendLine();
        }
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

    private void ParseHistoryWithParams(JToken? selections, JToken? progression, StringBuilder sb)
    {
        if (selections == null) return;

        // Build a map of blueprint GUID -> Param object from the Features collection
        var featureParams = new Dictionary<string, JToken>();
        var features = progression?["Features"]?["m_Facts"];
        if (features != null)
        {
            foreach (var feature in features)
            {
                var resolved = _resolver.Resolve(feature);
                if (resolved != null)
                {
                    var blueprint = resolved["Blueprint"]?.ToString();
                    var param = resolved["Param"];
                    if (!string.IsNullOrEmpty(blueprint) && param != null)
                    {
                        featureParams[blueprint] = param;
                    }
                }
            }
        }

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
                                // Check if this feature has parameters
                                string displayName = name;
                                if (_options.ShowFeatParameters && featureParams.TryGetValue(guid, out var param))
                                {
                                    var weaponCategory = param["WeaponCategory"]?.ToString();
                                    var spellSchool = param["SpellSchool"]?.ToString();
                                    var statType = param["StatType"]?.ToString();
                                    
                                    if (!string.IsNullOrEmpty(weaponCategory))
                                    {
                                        displayName = $"{name} ({weaponCategory})";
                                    }
                                    else if (!string.IsNullOrEmpty(spellSchool))
                                    {
                                        displayName = $"{name} ({spellSchool})";
                                    }
                                    else if (!string.IsNullOrEmpty(statType))
                                    {
                                        displayName = $"{name} ({statType})";
                                    }
                                }
                                
                                historyMap[level].Add(displayName);
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
