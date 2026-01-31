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
    private readonly string? _mainCharacterUniqueId;

    public EnhancedCharacterParser(BlueprintLookupService blueprintLookup, RefResolver resolver, ReportOptions options, string? mainCharacterUniqueId = null)
    {
        _blueprintLookup = blueprintLookup;
        _resolver = resolver;
        _options = options;
        _mainCharacterUniqueId = mainCharacterUniqueId;
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

    /// <summary>
    /// Parse character data into structured CharacterJson object (used by both text and JSON output)
    /// </summary>
    public CharacterJson? ParseData(JToken rawUnit)
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

        var character = new CharacterJson
        {
            Name = charName
        };

        // Get race
        string? raceId = progression["m_Race"]?.ToString();
        if (!string.IsNullOrEmpty(raceId))
        {
            character.Race = _blueprintLookup.GetName(raceId);
        }

        // Get alignment - use first entry for main character, last entry for companions
        var alignmentObj = descriptor["Alignment"];
        if (alignmentObj != null)
        {
            var history = alignmentObj["m_History"];
            if (history != null && history.HasValues)
            {
                // Check if this is the main character by comparing unique IDs
                var unitUniqueId = unit["UniqueId"]?.ToString();
                bool isMainCharacter = !string.IsNullOrEmpty(_mainCharacterUniqueId) && 
                                      !string.IsNullOrEmpty(unitUniqueId) && 
                                      unitUniqueId.Equals(_mainCharacterUniqueId, StringComparison.OrdinalIgnoreCase);
                
                // Main character uses starting alignment, companions use current alignment
                var alignmentEntry = isMainCharacter ? history.First() : history.Last();
                var direction = alignmentEntry?["Direction"]?.ToString();
                if (!string.IsNullOrEmpty(direction))
                {
                    // Format alignment: "LawfulGood" -> "Lawful Good"
                    character.Alignment = System.Text.RegularExpressions.Regex.Replace(
                        direction, "([a-z])([A-Z])", "$1 $2");
                }
            }
        }

        // Get classes
        character.Classes = new List<ClassInfoJson>();
        var classes = progression["Classes"];
        if (classes != null && classes.HasValues)
        {
            foreach (var c in classes)
            {
                string? classId = c["CharacterClass"]?.ToString();
                int level = (int?)c["Level"] ?? 0;
                string className = _blueprintLookup.GetName(classId ?? "");
                
                var classInfo = new ClassInfoJson
                {
                    ClassName = className,
                    Level = level
                };
                
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
                        classInfo.Archetype = string.Join(", ", archNames);
                    }
                }
                
                character.Classes.Add(classInfo);
            }
        }

        // Get stats
        var statsRef = descriptor["Stats"];
        var stats = _resolver.Resolve(statsRef);
        if (stats != null)
        {
            character.Attributes = new AttributesJson
            {
                Strength = GetStatValueInt(stats, "Strength"),
                Dexterity = GetStatValueInt(stats, "Dexterity"),
                Constitution = GetStatValueInt(stats, "Constitution"),
                Intelligence = GetStatValueInt(stats, "Intelligence"),
                Wisdom = GetStatValueInt(stats, "Wisdom"),
                Charisma = GetStatValueInt(stats, "Charisma")
            };

            // Get skills
            character.Skills = ParseSkillsData(stats);
        }

        // Get equipment
        var equipmentParser = new EquipmentParser(_blueprintLookup, _resolver, _options);
        character.Equipment = equipmentParser.ParseData(descriptor);

        // Get spellbooks
        character.Spellbooks = ParseSpellbooksData(descriptor);
        
        // Get formatted spellcasting text from SpellbookParser for full detail
        if (_options.IncludeSpellcasting)
        {
            var spellbookParser = new SpellbookParser(_blueprintLookup, _resolver, _options);
            character.FormattedSpellcasting = spellbookParser.ParseSpellbooks(descriptor);
        }

        // Get level progression
        character.LevelProgression = ParseLevelProgressionData(progression, descriptor);

        return character;
    }

    /// <summary>
    /// Format CharacterJson into text report (used by both CharacterParser and TextFileGenerator)
    /// </summary>
    public string FormatCharacter(CharacterJson character)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CHARACTER BUILD ANALYSIS ===");
        sb.AppendLine();
        sb.AppendLine($"Character Name: {character.Name}");
        sb.AppendLine();

        // Build Summary
        sb.AppendLine("BUILD SUMMARY");
        sb.AppendLine(new string('=', 80));

        // Stats
        if (_options.IncludeStats && character.Attributes != null)
        {
            sb.Append("Stats (base values + level-up allocated points, without racial/class/item/size modifiers): ");
            sb.Append($"Str {character.Attributes.Strength}, ");
            sb.Append($"Dex {character.Attributes.Dexterity}, ");
            sb.Append($"Con {character.Attributes.Constitution}, ");
            sb.Append($"Int {character.Attributes.Intelligence}, ");
            sb.Append($"Wis {character.Attributes.Wisdom}, ");
            sb.Append($"Cha {character.Attributes.Charisma}");
            sb.AppendLine();
            sb.AppendLine();

            // Skills
            if (_options.IncludeSkills && character.Skills != null)
            {
                sb.AppendLine("Skills (allocated points, without racial/class/item/size modifiers):");
                if (character.Skills.Mobility > 0) sb.AppendLine($"  Mobility: {character.Skills.Mobility}");
                if (character.Skills.Athletics > 0) sb.AppendLine($"  Athletics: {character.Skills.Athletics}");
                if (character.Skills.Stealth > 0) sb.AppendLine($"  Stealth: {character.Skills.Stealth}");
                if (character.Skills.Thievery > 0) sb.AppendLine($"  Thievery: {character.Skills.Thievery}");
                if (character.Skills.KnowledgeArcana > 0) sb.AppendLine($"  Knowledge (Arcana): {character.Skills.KnowledgeArcana}");
                if (character.Skills.KnowledgeWorld > 0) sb.AppendLine($"  Knowledge (World): {character.Skills.KnowledgeWorld}");
                if (character.Skills.LoreNature > 0) sb.AppendLine($"  Lore (Nature): {character.Skills.LoreNature}");
                if (character.Skills.LoreReligion > 0) sb.AppendLine($"  Lore (Religion): {character.Skills.LoreReligion}");
                if (character.Skills.Perception > 0) sb.AppendLine($"  Perception: {character.Skills.Perception}");
                if (character.Skills.Persuasion > 0) sb.AppendLine($"  Persuasion: {character.Skills.Persuasion}");
                if (character.Skills.UseMagicDevice > 0) sb.AppendLine($"  Use Magic Device: {character.Skills.UseMagicDevice}");
                sb.AppendLine();
            }
        }

        // Race
        if (_options.IncludeRace && !string.IsNullOrEmpty(character.Race))
        {
            sb.AppendLine($"Race - {character.Race}");
        }

        // Alignment
        if (!string.IsNullOrEmpty(character.Alignment))
        {
            sb.AppendLine($"Alignment - {character.Alignment}");
        }

        // Classes
        if (_options.IncludeClass && character.Classes != null && character.Classes.Any())
        {
            sb.Append("Class - ");
            var classParts = new List<string>();
            foreach (var c in character.Classes)
            {
                var classPart = $"{c.ClassName} {c.Level}";
                if (!string.IsNullOrEmpty(c.Archetype))
                {
                    classPart += $" ({c.Archetype})";
                }
                classParts.Add(classPart);
            }
            sb.AppendLine(string.Join(", ", classParts));
        }

        sb.AppendLine();
        sb.AppendLine();

        // Level-by-Level History
        if (_options.IncludeLevelHistory && character.LevelProgression != null && character.LevelProgression.Any())
        {
            sb.AppendLine("LEVEL-BY-LEVEL BUILD HISTORY");
            sb.AppendLine(new string('=', 80));
            
            foreach (var level in character.LevelProgression)
            {
                sb.AppendLine($"Level {level.Level}: {string.Join(", ", level.Features ?? new List<string>())}");
            }

            sb.AppendLine();
            sb.AppendLine();
        }

        // Equipment
        if (_options.IncludeEquipment && character.Equipment != null)
        {
            try
            {
                var equipmentParser = new EquipmentParser(_blueprintLookup, _resolver, _options);
                // Format equipment from CharacterJson.Equipment
                sb.AppendLine("EQUIPMENT");
                sb.AppendLine(new string('=', 80));
                
                // Format active weapon set
                if (_options.IncludeActiveWeaponSet && character.Equipment.WeaponSets != null && character.Equipment.WeaponSets.Any())
                {
                    var activeSet = character.Equipment.WeaponSets.FirstOrDefault(ws => ws.SetNumber == character.Equipment.ActiveWeaponSetIndex + 1);
                    if (activeSet != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"Active Weapon Set (Set {activeSet.SetNumber}):");
                        sb.AppendLine($"  Main Hand:  {FormatEquipmentSlot(activeSet.MainHand)}");
                        sb.AppendLine($"  Off Hand:   {FormatEquipmentSlot(activeSet.OffHand)}");
                    }
                }
                
                // Format armor and accessories
                if (_options.IncludeArmor || _options.IncludeAccessories)
                {
                    sb.AppendLine();
                    sb.AppendLine("Armor & Accessories:");
                    
                    var slots = new List<(string name, EquipmentSlotJson? slot, bool isArmor)>
                    {
                        ("Body", character.Equipment.Body, true),
                        ("Head", character.Equipment.Head, false),
                        ("Neck", character.Equipment.Neck, false),
                        ("Belt", character.Equipment.Belt, false),
                        ("Cloak", character.Equipment.Cloak, false),
                        ("Ring 1", character.Equipment.Ring1, false),
                        ("Ring 2", character.Equipment.Ring2, false),
                        ("Bracers", character.Equipment.Bracers, false),
                        ("Gloves", character.Equipment.Gloves, false),
                        ("Boots", character.Equipment.Boots, false)
                    };

                    foreach (var (name, slot, isArmor) in slots)
                    {
                        if (isArmor && !_options.IncludeArmor) continue;
                        if (!isArmor && !_options.IncludeAccessories) continue;
                        
                        var itemInfo = FormatEquipmentSlot(slot);
                        if (!_options.ShowEmptySlots && itemInfo == "(empty)") continue;
                        
                        sb.AppendLine($"  {name,-10}: {itemInfo}");
                    }
                }
                
                // Format quick slots
                if (character.Equipment.QuickSlots != null && character.Equipment.QuickSlots.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("Quick Slots (Potions, Scrolls, Rods, Wands):");
                    int slotNumber = 1;
                    foreach (var slot in character.Equipment.QuickSlots)
                    {
                        var itemInfo = FormatEquipmentSlot(slot);
                        if (itemInfo != "(empty)")
                        {
                            sb.AppendLine($"  Slot {slotNumber}: {itemInfo}");
                        }
                        slotNumber++;
                    }
                }
                
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to format equipment: {ex.Message}");
                Console.WriteLine($"  Stack: {ex.StackTrace?.Split('\n')[0]}");
            }
        }

        // Spellcasting - use pre-formatted text from SpellbookParser
        if (_options.IncludeSpellcasting && !string.IsNullOrEmpty(character.FormattedSpellcasting))
        {
            sb.AppendLine(character.FormattedSpellcasting);
        }

        return sb.ToString();
    }

    private CharacterReport? ParseSingleCharacter(JToken rawUnit)
    {
        // Use ParseData to get structured character data
        var character = ParseData(rawUnit);
        if (character == null) return null;

        return new CharacterReport
        {
            CharacterName = character.Name,
            Report = FormatCharacter(character)
        };
    }

    private string FormatEquipmentSlot(EquipmentSlotJson? slot)
    {
        if (slot == null || string.IsNullOrEmpty(slot.Name))
            return "(empty)";

        var result = slot.Name;
        
        // Add equipment type if available
        if (!string.IsNullOrEmpty(slot.Type))
        {
            result = $"{result} [{slot.Type}]";
        }
        
        // Add enchantments if available and option is enabled
        if (_options.ShowEnchantments && slot.Enchantments != null && slot.Enchantments.Any())
        {
            var enchantmentText = $"({string.Join(", ", slot.Enchantments)})";
            result = $"{result} {enchantmentText}";
        }
        
        return result;
    }

    private string GetStatValue(JToken stats, string statName)
    {
        var statObj = _resolver.Resolve(stats[statName]);
        return statObj?["m_BaseValue"]?.ToString() ?? "?";
    }

    private int GetStatValueInt(JToken stats, string statName)
    {
        var statObj = _resolver.Resolve(stats[statName]);
        return (int?)statObj?["m_BaseValue"] ?? 0;
    }

    private SkillsJson? ParseSkillsData(JToken stats)
    {
        var skills = new SkillsJson();
        var skillValues = new Dictionary<string, int>();

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
                
                // Check if it's a skill
                bool isSkill = (typeName != null && typeName.Contains("ModifiableValueSkill")) ||
                              (typeProperty != null && typeProperty.StartsWith("Skill"));
                              
                if (!isSkill) continue;

                string? skillType = typeProperty;
                int skillValue = (int?)resolvedDep["m_BaseValue"] ?? 0;

                // Map skill types to properties
                switch (skillType)
                {
                    case "SkillMobility":
                        skills.Mobility = skillValue;
                        break;
                    case "SkillAthletics":
                        skills.Athletics = skillValue;
                        break;
                    case "SkillStealth":
                        skills.Stealth = skillValue;
                        break;
                    case "SkillThievery":
                        skills.Thievery = skillValue;
                        break;
                    case "SkillKnowledgeArcana":
                        skills.KnowledgeArcana = skillValue;
                        break;
                    case "SkillKnowledgeWorld":
                        skills.KnowledgeWorld = skillValue;
                        break;
                    case "SkillLoreNature":
                        skills.LoreNature = skillValue;
                        break;
                    case "SkillLoreReligion":
                        skills.LoreReligion = skillValue;
                        break;
                    case "SkillPerception":
                        skills.Perception = skillValue;
                        break;
                    case "SkillPersuasion":
                        skills.Persuasion = skillValue;
                        break;
                    case "SkillUseMagicDevice":
                        skills.UseMagicDevice = skillValue;
                        break;
                }
            }
        }

        return skills;
    }

    private List<SpellbookJson>? ParseSpellbooksData(JToken? descriptor)
    {
        if (descriptor == null) return null;

        var spellbookParser = new SpellbookParser(_blueprintLookup, _resolver, _options);
        var spellbooksRef = descriptor["Spellbooks"];
        if (spellbooksRef == null || !spellbooksRef.HasValues) return null;

        var spellbooks = new List<SpellbookJson>();

        foreach (var spellbookRef in spellbooksRef)
        {
            var spellbook = _resolver.Resolve(spellbookRef);
            if (spellbook == null) continue;

            var blueprintId = spellbook["Blueprint"]?.ToString();
            if (string.IsNullOrEmpty(blueprintId)) continue;

            var spellbookName = _blueprintLookup.GetName(blueprintId);
            if (string.IsNullOrEmpty(spellbookName) || spellbookName.StartsWith("Blueprint_")) continue;

            var spellbookJson = new SpellbookJson
            {
                ClassName = spellbookName,
                CasterLevel = (int?)spellbook["CasterLevel"] ?? 0,
                SpellSlotsPerDay = new Dictionary<int, int>(),
                KnownSpells = new Dictionary<int, List<string>>()
            };

            // Parse spell slots
            var spellSlots = spellbook["m_SpontaneousSlots"];
            if (spellSlots != null && spellSlots.HasValues)
            {
                foreach (var slotEntry in spellSlots)
                {
                    int level = (int?)slotEntry["Key"] ?? -1;
                    int count = (int?)slotEntry["Value"] ?? 0;
                    if (level >= 0 && count > 0)
                    {
                        spellbookJson.SpellSlotsPerDay[level] = count;
                    }
                }
            }

            // Parse known spells
            var knownSpells = spellbook["m_KnownSpells"];
            if (knownSpells != null && knownSpells.HasValues)
            {
                foreach (var spellLevel in knownSpells)
                {
                    int level = (int?)spellLevel["Key"] ?? -1;
                    if (level < 0) continue;

                    var spellList = spellLevel["Value"];
                    if (spellList == null || !spellList.HasValues) continue;

                    var spellNames = new List<string>();
                    foreach (var spell in spellList)
                    {
                        var spellBlueprintId = spell["Blueprint"]?.ToString();
                        if (!string.IsNullOrEmpty(spellBlueprintId))
                        {
                            var spellName = _blueprintLookup.GetName(spellBlueprintId);
                            if (!string.IsNullOrEmpty(spellName) && !spellName.StartsWith("Blueprint_"))
                            {
                                spellNames.Add(spellName);
                            }
                        }
                    }

                    if (spellNames.Any())
                    {
                        spellbookJson.KnownSpells[level] = spellNames;
                    }
                }
            }

            spellbooks.Add(spellbookJson);
        }

        return spellbooks.Any() ? spellbooks : null;
    }

    private List<LevelProgressionJson>? ParseLevelProgressionData(JToken? progression, JToken? descriptor)
    {
        if (progression == null) return null;

        var selections = progression["m_Selections"];
        if (selections == null) return null;

        // Build a map of blueprint GUID -> Param object from the Features collection
        var featureParams = new Dictionary<string, JToken>();
        var features = progression["Features"]?["m_Facts"];
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
                            
                            if (!string.IsNullOrEmpty(name) && 
                                name != "None" && 
                                !name.StartsWith("Blueprint_"))
                            {
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

        var levelProgression = new List<LevelProgressionJson>();
        foreach (var kvp in historyMap)
        {
            if (kvp.Value.Count > 0)
            {
                levelProgression.Add(new LevelProgressionJson
                {
                    Level = kvp.Key,
                    Features = kvp.Value
                });
            }
        }

        return levelProgression.Any() ? levelProgression : null;
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
