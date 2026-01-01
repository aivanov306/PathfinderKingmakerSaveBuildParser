using PathfinderSaveParser.Models;
using Newtonsoft.Json.Linq;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Converts parsed data to JSON format for structured output
/// </summary>
public class JsonOutputBuilder
{
    private readonly BlueprintLookupService _blueprintLookup;
    private readonly RefResolver? _resolver;
    private readonly ReportOptions _options;

    public JsonOutputBuilder(BlueprintLookupService blueprintLookup, RefResolver? resolver = null, ReportOptions? options = null)
    {
        _blueprintLookup = blueprintLookup;
        _resolver = resolver;
        _options = options ?? new ReportOptions();
    }

    public KingdomStatsJson? BuildKingdomJson(Kingdom? kingdom, int money)
    {
        if (kingdom == null) return null;

        var json = new KingdomStatsJson
        {
            Name = kingdom.KingdomName,
            Alignment = kingdom.Alignment,
            Gold = money,
            BuildPoints = kingdom.BuildPoints,
            BuildPointsPerTurn = kingdom.BuildPointsPerTurn,
            UnrestLevel = kingdom.Unrest,
            Stats = new List<KingdomStatJson>(),
            Advisors = new List<AdvisorJson>()
        };

        // Add kingdom stats
        if (kingdom.Stats?.Stats != null)
        {
            foreach (var stat in kingdom.Stats.Stats)
            {
                json.Stats.Add(new KingdomStatJson
                {
                    Type = stat.Type,
                    Value = stat.Value,
                    Rank = stat.Rank
                });
            }
        }

        // Add advisors
        if (kingdom.Leaders != null)
        {
            foreach (var leader in kingdom.Leaders)
            {
                var advisorName = leader.LeaderSelection?.Blueprint != null
                    ? _blueprintLookup.GetName(leader.LeaderSelection.Blueprint)
                    : null;

                var status = advisorName != null ? "Assigned" :
                    IsPositionAvailable(leader.Type, kingdom) ? "Vacant" : "Locked";

                json.Advisors.Add(new AdvisorJson
                {
                    Position = FormatPositionName(leader.Type),
                    Advisor = advisorName,
                    Status = status
                });
            }
        }

        return json;
    }

    public InventoryJson BuildInventoryJson(ItemCollection? personalChest, JToken? partyJson)
    {
        var json = new InventoryJson
        {
            PersonalChest = BuildInventoryCollectionJson(ParseItemsFromCollection(personalChest)),
            SharedInventory = BuildInventoryCollectionJson(ParseItemsFromParty(partyJson))
        };

        return json;
    }

    private List<(string blueprint, int count)> ParseItemsFromCollection(ItemCollection? collection)
    {
        var items = new List<(string blueprint, int count)>();
        if (collection?.Items == null) return items;

        foreach (var item in collection.Items)
        {
            if (item?.Blueprint != null)
            {
                items.Add((item.Blueprint, item.Count));
            }
        }

        return items;
    }

    private List<(string blueprint, int count)> ParseItemsFromParty(JToken? partyJson)
    {
        var items = new List<(string blueprint, int count)>();
        if (partyJson == null) return items;

        try
        {
            var entityData = partyJson["m_EntityData"];
            if (entityData == null || !entityData.Any()) return items;

            var firstEntity = entityData.First;
            var descriptor = firstEntity?["Descriptor"];
            var inventory = descriptor?["m_Inventory"];
            var itemsArray = inventory?["m_Items"];

            if (itemsArray == null || !itemsArray.Any()) return items;

            foreach (var item in itemsArray)
            {
                if (item == null || item.Type == JTokenType.Null) continue;

                var slotIndex = item["m_InventorySlotIndex"]?.Value<int>();
                if (slotIndex == null || slotIndex < 0) continue;

                var blueprint = item["m_Blueprint"]?.Value<string>();
                var count = item["m_Count"]?.Value<int>() ?? 1;

                if (!string.IsNullOrEmpty(blueprint))
                {
                    items.Add((blueprint, count));
                }
            }
        }
        catch
        {
            // Return empty on error
        }

        return items;
    }

    private InventoryCollectionJson BuildInventoryCollectionJson(List<(string blueprint, int count)> items)
    {
        var collection = new InventoryCollectionJson
        {
            Weapons = new List<InventoryItemJson>(),
            Armor = new List<InventoryItemJson>(),
            Accessories = new List<InventoryItemJson>(),
            Usables = new List<InventoryItemJson>(),
            Other = new List<InventoryItemJson>()
        };

        foreach (var (blueprint, count) in items)
        {
            var name = _blueprintLookup.GetName(blueprint);
            if (name == blueprint) continue; // Skip unknown items

            var type = _blueprintLookup.GetEquipmentType(blueprint);
            var item = new InventoryItemJson
            {
                Name = name,
                Type = type,
                Count = count
            };

            if (IsWeapon(type))
                collection.Weapons!.Add(item);
            else if (IsArmor(type))
                collection.Armor!.Add(item);
            else if (IsUsable(name))
                collection.Usables!.Add(item);
            else if (IsAccessory(name))
                collection.Accessories!.Add(item);
            else
                collection.Other!.Add(item);
        }

        collection.TotalItems = items.Sum(i => i.count);
        collection.UniqueItems = collection.Weapons!.Count + collection.Armor!.Count +
                                collection.Accessories!.Count + collection.Usables!.Count +
                                collection.Other!.Count;

        return collection;
    }

    private bool IsWeapon(string? type)
    {
        if (string.IsNullOrEmpty(type)) return false;
        var weaponTypes = new[]
        {
            "Longsword", "Shortsword", "Greatsword", "Bastard Sword", "Dueling Sword",
            "Dagger", "Kukri", "Punching Dagger", "Sickle", "Starknife",
            "Battleaxe", "Handaxe", "Greataxe", "Warhammer", "Light Hammer",
            "Heavy Flail", "Light Flail", "Greatclub", "Club", "Heavy Mace", "Light Mace",
            "Scimitar", "Falchion", "Rapier", "Estoc", "Sai",
            "Glaive", "Scythe", "Bardiche", "Fauchard", "Nunchaku",
            "Light Pick", "Heavy Pick", "Kama", "Trident", "Sling Staff",
            "Quarterstaff", "Spear", "Longspear", "Javelin",
            "Shortbow", "Longbow", "Light Crossbow", "Heavy Crossbow",
            "Dart", "Throwing Axe", "Sling",
            "Composite Shortbow", "Composite Longbow"
        };
        return weaponTypes.Any(wt => type.Contains(wt, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsArmor(string? type)
    {
        if (string.IsNullOrEmpty(type)) return false;
        var armorTypes = new[]
        {
            "Light Armor", "Medium Armor", "Heavy Armor",
            "Buckler", "Light Shield", "Heavy Shield", "Tower Shield"
        };
        return armorTypes.Any(at => type.Equals(at, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAccessory(string name)
    {
        var keywords = new[] { "Amulet", "Belt", "Ring", "Bracers", "Cloak", "Headband", "Circlet", "Helmet", "Gloves", "Boots" };
        return keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsUsable(string name)
    {
        var keywords = new[] { "Potion", "Scroll", "Elixir", "Extract", "Wand", "Oil of", "Antidote", "Antitoxin", "Holy Water", "Flask", "Alchemist", "Alchemists Fire" };
        return keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsPositionAvailable(string? positionType, Kingdom kingdom)
    {
        if (string.IsNullOrEmpty(positionType) || kingdom.Stats?.Stats == null)
            return true;

        var requirements = new Dictionary<string, (string statType, int minRank, int minValue)>
        {
            { "GrandDiplomat", ("Community", 3, 60) },
            { "Warden", ("Military", 3, 60) },
            { "Magister", ("Arcane", 3, 60) },
            { "Curator", ("Loyalty", 3, 60) },
            { "Spymaster", ("Relations", 3, 60) }
        };

        if (!requirements.TryGetValue(positionType, out var req))
            return true;

        var stat = kingdom.Stats.Stats.FirstOrDefault(s => s.Type == req.statType);
        return stat != null && stat.Rank >= req.minRank && stat.Value >= req.minValue;
    }

    private string? FormatPositionName(string? position)
    {
        if (string.IsNullOrEmpty(position)) return position;
        if (position == "Spymaster") return "Minister";
        return System.Text.RegularExpressions.Regex.Replace(position, "([a-z])([A-Z])", "$1 $2");
    }

    public List<string> BuildExploredLocationsJson(GlobalMap? globalMap)
    {
        var exploredLocations = new List<string>();
        
        if (globalMap?.Locations == null) return exploredLocations;

        foreach (var locationEntry in globalMap.Locations)
        {
            if (locationEntry.Value?.IsExplored == true && !string.IsNullOrEmpty(locationEntry.Value.Blueprint))
            {
                var locationName = _blueprintLookup.GetName(locationEntry.Value.Blueprint);
                if (!string.IsNullOrEmpty(locationName) && locationName != "None")
                {
                    exploredLocations.Add(locationName);
                }
            }
        }

        return exploredLocations.OrderBy(l => l).ToList();
    }

    public List<SettlementJson> BuildSettlementsJson(Kingdom? kingdom)
    {
        var settlements = new List<SettlementJson>();
        
        if (kingdom?.Regions == null) return settlements;

        foreach (var region in kingdom.Regions)
        {
            if (region.Settlement != null)
            {
                // Skip unclaimed regions unless explicitly requested
                if (!region.IsClaimed && !_options.IncludeUnclaimedSettlements)
                    continue;

                var regionName = _blueprintLookup.GetName(region.Blueprint ?? "");
                if (string.IsNullOrEmpty(regionName) || regionName == "None")
                    continue;

                var buildings = new List<string>();
                if (region.Settlement.Buildings?.Facts != null)
                {
                    foreach (var building in region.Settlement.Buildings.Facts)
                    {
                        if (building.IsFinished && !string.IsNullOrEmpty(building.Blueprint))
                        {
                            var buildingName = _blueprintLookup.GetName(building.Blueprint);
                            if (!string.IsNullOrEmpty(buildingName) && buildingName != "None")
                            {
                                buildings.Add(buildingName);
                            }
                        }
                    }
                }

                settlements.Add(new SettlementJson
                {
                    RegionName = regionName,
                    SettlementName = region.Settlement.Name,
                    Level = region.Settlement.Level,
                    Buildings = buildings.OrderBy(b => b).ToList()
                });
            }
        }

        return settlements.OrderBy(s => s.RegionName).ToList();
    }

    public List<CharacterJson> BuildCharactersJson(JToken? partyJson)
    {
        var characters = new List<CharacterJson>();
        
        if (partyJson == null || _resolver == null) return characters;

        var units = partyJson["m_EntityData"];
        if (units == null) return characters;

        foreach (var rawUnit in units)
        {
            try
            {
                var character = ParseSingleCharacterJson(rawUnit);
                if (character != null)
                {
                    // Apply ReportOptions filtering
                    if (!_options.IncludeStats) character.Attributes = null;
                    if (!_options.IncludeSkills) character.Skills = null;
                    if (!_options.IncludeRace) character.Race = null;
                    if (!_options.IncludeClass) character.Classes = null;
                    if (!_options.IncludeEquipment) character.Equipment = null;
                    if (!_options.IncludeSpellcasting) character.Spellbooks = null;
                    if (!_options.IncludeLevelHistory) character.LevelProgression = null;
                    
                    characters.Add(character);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse character for JSON: {ex.Message}");
            }
        }

        return characters;
    }

    private CharacterJson? ParseSingleCharacterJson(JToken rawUnit)
    {
        if (_resolver == null) return null;

        // Resolve the unit reference
        var unit = _resolver.Resolve(rawUnit);
        if (unit == null) return null;

        // Resolve the descriptor reference
        var descriptor = _resolver.Resolve(unit["Descriptor"]);
        if (descriptor == null) return null;

        // Get character name
        string? customName = descriptor["CustomName"]?.ToString();
        string? blueprintId = descriptor["Blueprint"]?.ToString();
        
        string? charName;
        if (!string.IsNullOrEmpty(customName))
        {
            string blueprintName = _blueprintLookup.GetName(blueprintId ?? "");
            charName = $"{customName} ({blueprintName})";
        }
        else
        {
            charName = _blueprintLookup.GetName(blueprintId ?? "");
        }

        // Filter out units without progression
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
                Strength = GetStatValue(stats, "Strength"),
                Dexterity = GetStatValue(stats, "Dexterity"),
                Constitution = GetStatValue(stats, "Constitution"),
                Intelligence = GetStatValue(stats, "Intelligence"),
                Wisdom = GetStatValue(stats, "Wisdom"),
                Charisma = GetStatValue(stats, "Charisma")
            };

            // Get skills
            character.Skills = ParseSkillsJson(stats);
        }

        // Get equipment
        character.Equipment = ParseEquipmentJson(descriptor);

        // Get spellbooks
        character.Spellbooks = ParseSpellbooksJson(descriptor);

        // Get level progression
        character.LevelProgression = ParseLevelProgressionJson(progression);

        return character;
    }

    private int GetStatValue(JToken stats, string statName)
    {
        if (_resolver == null) return 0;
        var statObj = _resolver.Resolve(stats[statName]);
        return (int?)statObj?["PermanentValue"] ?? 0;
    }

    private SkillsJson? ParseSkillsJson(JToken stats)
    {
        if (_resolver == null) return null;

        var skills = new SkillsJson();

        // Parse each attribute's dependent skills
        // Skills are stored as m_Dependents array on each attribute stat
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
                if (typeName == null || !typeName.Contains("ModifiableValueSkill")) continue;

                string? skillType = resolvedDep["Type"]?.ToString();
                int skillValue = (int?)resolvedDep["PermanentValue"] ?? 0;

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

    private EquipmentJson? ParseEquipmentJson(JToken descriptor)
    {
        if (_resolver == null) return null;

        var bodyRef = descriptor["Body"];
        var body = _resolver.Resolve(bodyRef);
        if (body == null) return null;

        var equipment = new EquipmentJson();

        // Parse primary hand
        var primaryHandRef = body["m_PrimaryHand"];
        var primaryHand = _resolver.Resolve(primaryHandRef);
        equipment.MainHand = ParseEquipmentSlotJson(primaryHand);

        // Parse secondary hand
        var secondaryHandRef = body["m_SecondaryHand"];
        var secondaryHand = _resolver.Resolve(secondaryHandRef);
        equipment.OffHand = ParseEquipmentSlotJson(secondaryHand);

        // Parse armor slots
        var armor = body["m_Armor"];
        var armorRef = _resolver.Resolve(armor);
        equipment.Body = ParseEquipmentSlotJson(armorRef);

        var head = body["m_Head"];
        var headRef = _resolver.Resolve(head);
        equipment.Head = ParseEquipmentSlotJson(headRef);

        var neck = body["m_Neck"];
        var neckRef = _resolver.Resolve(neck);
        equipment.Neck = ParseEquipmentSlotJson(neckRef);

        var belt = body["m_Belt"];
        var beltRef = _resolver.Resolve(belt);
        equipment.Belt = ParseEquipmentSlotJson(beltRef);

        var shoulders = body["m_Shoulders"];
        var shouldersRef = _resolver.Resolve(shoulders);
        equipment.Cloak = ParseEquipmentSlotJson(shouldersRef);

        var ring1 = body["m_Ring1"];
        var ring1Ref = _resolver.Resolve(ring1);
        equipment.Ring1 = ParseEquipmentSlotJson(ring1Ref);

        var ring2 = body["m_Ring2"];
        var ring2Ref = _resolver.Resolve(ring2);
        equipment.Ring2 = ParseEquipmentSlotJson(ring2Ref);

        var wrist = body["m_Wrist"];
        var wristRef = _resolver.Resolve(wrist);
        equipment.Bracers = ParseEquipmentSlotJson(wristRef);

        var gloves = body["m_Gloves"];
        var glovesRef = _resolver.Resolve(gloves);
        equipment.Gloves = ParseEquipmentSlotJson(glovesRef);

        var feet = body["m_Feet"];
        var feetRef = _resolver.Resolve(feet);
        equipment.Boots = ParseEquipmentSlotJson(feetRef);

        return equipment;
    }

    private EquipmentSlotJson? ParseEquipmentSlotJson(JToken? itemRef)
    {
        if (itemRef == null || _resolver == null) return null;

        var item = _resolver.Resolve(itemRef);
        if (item == null) return null;

        var blueprintId = item["Blueprint"]?.ToString();
        if (string.IsNullOrEmpty(blueprintId)) return null;

        var itemName = _blueprintLookup.GetName(blueprintId);
        if (string.IsNullOrEmpty(itemName) || itemName == "None") return null;

        var enchantments = new List<string>();
        var enchantsRef = item["m_Enchantments"];
        if (enchantsRef != null)
        {
            foreach (var ench in enchantsRef)
            {
                var enchObj = _resolver.Resolve(ench);
                if (enchObj != null)
                {
                    var enchBlueprintId = enchObj["Blueprint"]?.ToString();
                    if (!string.IsNullOrEmpty(enchBlueprintId))
                    {
                        var enchName = _blueprintLookup.GetName(enchBlueprintId);
                        if (!string.IsNullOrEmpty(enchName) && enchName != "None")
                        {
                            enchantments.Add(enchName);
                        }
                    }
                }
            }
        }

        // Get item type from blueprint
        string? itemType = _blueprintLookup.GetEquipmentType(blueprintId);

        return new EquipmentSlotJson
        {
            Name = itemName,
            Type = itemType,
            Enchantments = enchantments.Any() ? enchantments : null
        };
    }

    private List<SpellbookJson>? ParseSpellbooksJson(JToken descriptor)
    {
        if (_resolver == null) return null;

        var spellbooks = new List<SpellbookJson>();
        var demilichRef = descriptor["Demilich"];
        var demilich = _resolver.Resolve(demilichRef);
        if (demilich == null) return null;

        var spellbooksArray = demilich["m_Spellbooks"];
        if (spellbooksArray == null || !spellbooksArray.HasValues) return null;

        foreach (var spellbookRef in spellbooksArray)
        {
            var spellbook = _resolver.Resolve(spellbookRef);
            if (spellbook == null) continue;

            var classBlueprint = spellbook["Blueprint"]?.ToString();
            if (string.IsNullOrEmpty(classBlueprint)) continue;

            var className = _blueprintLookup.GetName(classBlueprint);
            var casterLevel = (int?)spellbook["CasterLevel"] ?? 0;

            var spellbookJson = new SpellbookJson
            {
                ClassName = className,
                CasterLevel = casterLevel,
                SpellSlotsPerDay = new Dictionary<int, int>(),
                KnownSpells = new Dictionary<int, List<string>>()
            };

            // Parse spell slots per day
            var memorizedSpells = spellbook["m_MemorizedSpells"];
            if (memorizedSpells != null)
            {
                foreach (var levelEntry in memorizedSpells)
                {
                    var spellLevel = (int?)levelEntry["Key"] ?? 0;
                    var spells = levelEntry["Value"];
                    if (spells != null && spells.HasValues)
                    {
                        spellbookJson.SpellSlotsPerDay[spellLevel] = spells.Count();
                    }
                }
            }

            // Parse known spells
            var knownSpells = spellbook["m_KnownSpells"];
            if (knownSpells != null)
            {
                foreach (var levelEntry in knownSpells)
                {
                    var spellLevel = (int?)levelEntry["Key"] ?? 0;
                    var spells = levelEntry["Value"];
                    
                    if (spells != null && spells.HasValues)
                    {
                        var spellNames = new List<string>();
                        foreach (var spell in spells)
                        {
                            var spellBlueprint = spell["Blueprint"]?.ToString();
                            if (!string.IsNullOrEmpty(spellBlueprint))
                            {
                                var spellName = _blueprintLookup.GetName(spellBlueprint);
                                if (!string.IsNullOrEmpty(spellName) && spellName != "None")
                                {
                                    spellNames.Add(spellName);
                                }
                            }
                        }
                        if (spellNames.Any())
                        {
                            spellbookJson.KnownSpells[spellLevel] = spellNames;
                        }
                    }
                }
            }

            spellbooks.Add(spellbookJson);
        }

        return spellbooks.Any() ? spellbooks : null;
    }

    private List<LevelProgressionJson>? ParseLevelProgressionJson(JToken progression)
    {
        if (_resolver == null) return null;

        var levelProgression = new List<LevelProgressionJson>();
        var selections = progression["m_Selections"];
        if (selections == null) return null;

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
                                historyMap[level].Add(name);
                            }
                        }
                    }
                }
            }
        }

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
}
