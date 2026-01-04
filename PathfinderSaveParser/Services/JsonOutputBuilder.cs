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

    public KingdomStatsJson? BuildKingdomJson(Kingdom? kingdom, int money, string? gameTime, int? bpPerTurnOverride)
    {
        if (kingdom == null) return null;

        // Calculate days from game time (format: "days.hours:minutes:seconds")
        int days = 0;
        if (!string.IsNullOrEmpty(gameTime))
        {
            var parts = gameTime.Split('.');
            if (parts.Length > 0 && int.TryParse(parts[0], out int parsedDays))
            {
                days = parsedDays;
            }
        }

        var json = new KingdomStatsJson
        {
            Name = kingdom.KingdomName,
            Alignment = kingdom.Alignment,
            GameTime = gameTime,
            Days = days,
            Gold = money,
            BuildPoints = kingdom.BuildPoints,
            BuildPointsPerTurn = bpPerTurnOverride,
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
            PersonalChest = BuildInventoryCollectionJson(ParseItemsFromCollectionWithEnchantments(personalChest)),
            SharedInventory = BuildInventoryCollectionJson(ParseItemsFromPartyWithEnchantments(partyJson))
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

    private List<(string blueprint, int count, List<string>? enchantments)> ParseItemsFromCollectionWithEnchantments(ItemCollection? collection)
    {
        var items = new List<(string blueprint, int count, List<string>? enchantments)>();
        if (collection?.Items == null) return items;

        foreach (var item in collection.Items)
        {
            if (item?.Blueprint != null)
            {
                var enchantments = new List<string>();
                if (item.Enchantments?.Facts != null)
                {
                    foreach (var enchant in item.Enchantments.Facts)
                    {
                        if (!string.IsNullOrEmpty(enchant?.Blueprint))
                        {
                            var enchantName = _blueprintLookup.GetName(enchant.Blueprint);
                            if (enchantName != enchant.Blueprint)
                            {
                                enchantments.Add(enchantName);
                            }
                        }
                    }
                }
                items.Add((item.Blueprint, item.Count, enchantments.Any() ? enchantments : null));
            }
        }

        return items;
    }

    private List<(string blueprint, int count, List<string>? enchantments)> ParseItemsFromPartyWithEnchantments(JToken? partyJson)
    {
        var items = new List<(string blueprint, int count, List<string>? enchantments)>();
        if (partyJson == null) return items;

        int totalItems = 0;
        int sharedItems = 0;
        int failedItems = 0;
        try
        {
            var entityData = partyJson["m_EntityData"];
            if (entityData == null || !entityData.Any()) return items;

            var firstEntity = entityData.First;
            var descriptor = firstEntity?["Descriptor"];
            var inventory = descriptor?["m_Inventory"];
            var itemsArray = inventory?["m_Items"];

            if (itemsArray == null || !itemsArray.Any()) return items;

            totalItems = itemsArray.Count();
            foreach (var item in itemsArray)
            {
                try
                {
                    if (item == null || item.Type == JTokenType.Null) continue;

                    var slotIndex = item["m_InventorySlotIndex"]?.Value<int>();
                    if (slotIndex == null || slotIndex < 0) continue;
                    
                    sharedItems++;

                    var blueprint = item["m_Blueprint"]?.Value<string>();
                    var count = item["m_Count"]?.Value<int>() ?? 1;

                    if (!string.IsNullOrEmpty(blueprint))
                    {
                        // Parse enchantments
                        var enchantments = new List<string>();
                        try
                        {
                            var enchantsToken = item["m_Enchantments"];
                            if (enchantsToken != null)
                            {
                                // Handle case where m_Enchantments might be nested (e.g., m_Enchantments -> m_Enchantments)
                                var enchantsArray = enchantsToken is JArray ? enchantsToken : enchantsToken["m_Enchantments"];
                                
                                if (enchantsArray != null && enchantsArray is JArray arr && arr.Any())
                                {
                                    foreach (var enchant in arr)
                                    {
                                        var enchantBlueprint = enchant?["m_Blueprint"]?.Value<string>();
                                        if (!string.IsNullOrEmpty(enchantBlueprint))
                                        {
                                            var enchantName = _blueprintLookup.GetName(enchantBlueprint);
                                            if (enchantName != enchantBlueprint)
                                            {
                                                enchantments.Add(enchantName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignore enchantment parsing errors
                        }

                        items.Add((blueprint, count, enchantments.Any() ? enchantments : null));
                    }
                }
                catch
                {
                    failedItems++;
                    // Skip items that fail to parse
                }
            }
            
            if (failedItems > 0)
            {
                Console.WriteLine($"Warning: Failed to parse {failedItems} shared inventory items (out of {sharedItems} shared items from {totalItems} total)");
            }
        }
        catch
        {
            // Return empty on error
        }

        return items;
    }

    private InventoryCollectionJson BuildInventoryCollectionJson(List<(string blueprint, int count, List<string>? enchantments)> items)
    {
        var collection = new InventoryCollectionJson
        {
            Weapons = new List<InventoryItemJson>(),
            Armor = new List<InventoryItemJson>(),
            Accessories = new List<InventoryItemJson>(),
            Usables = new List<InventoryItemJson>(),
            Other = new List<InventoryItemJson>()
        };

        int skippedCount = 0;
        foreach (var (blueprint, count, enchantments) in items)
        {
            var name = _blueprintLookup.GetName(blueprint);
            if (name == blueprint)
            {
                skippedCount++;
                continue; // Skip unknown items
            }

            var type = _blueprintLookup.GetEquipmentType(blueprint);
            var item = new InventoryItemJson
            {
                Name = name,
                Type = type,
                Count = count,
                Enchantments = enchantments
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

        if (skippedCount > 0)
        {
            Console.WriteLine($"Warning: Skipped {skippedCount} items with unknown blueprints (not in database)");
        }

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
            "Scimitar", "Falchion", "Falcata", "Rapier", "Estoc", "Sai",
            "Glaive", "Scythe", "Bardiche", "Fauchard", "Nunchaku",
            "Light Pick", "Heavy Pick", "Kama", "Trident", "Sling Staff",
            "Quarterstaff", "Spear", "Longspear", "Javelin", "Earth Breaker",
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
            "Buckler", "Light Shield", "Heavy Shield", "Tower Shield",
            "Padded", "Leather", "Studded", "Chain Shirt", "Hide", "Scale Mail",
            "Chainmail", "Breastplate", "Splint Mail", "Banded Mail", "Half-Plate", "Full Plate"
        };
        return armorTypes.Any(at => type.Contains(at, StringComparison.OrdinalIgnoreCase));
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
                    IsClaimed = region.IsClaimed,
                    Buildings = buildings.OrderBy(b => b).ToList(),
                    Artisans = region.IsClaimed ? BuildArtisansJson(region.Artisans) : null
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
                string? typeProperty = resolvedDep["Type"]?.ToString();
                
                // Check if it's a skill: either has ModifiableValueSkill in $type, or Type starts with "Skill"
                bool isSkill = (typeName != null && typeName.Contains("ModifiableValueSkill")) ||
                              (typeProperty != null && typeProperty.StartsWith("Skill"));
                              
                if (!isSkill) continue;

                string? skillType = typeProperty;
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
        if (bodyRef == null) return null;
        
        var body = _resolver.Resolve(bodyRef);
        if (body == null || body.Type == JTokenType.Null)
            return null; // Can't parse equipment without body reference

        var equipment = new EquipmentJson();

        // Parse active weapon set
        var sets = body["m_HandsEquipmentSets"];
        var activeIndex = body["m_CurrentHandsEquipmentSetIndex"]?.Value<int>() ?? 0;
        
        if (sets != null && sets.HasValues && sets.Count() > activeIndex)
        {
            var activeSet = sets.ElementAtOrDefault(activeIndex);
            if (activeSet is JObject)
            {
                var primaryRef = activeSet["PrimaryHand"];
                equipment.MainHand = ParseEquipmentSlotJson(primaryRef);
                
                var secondaryRef = activeSet["SecondaryHand"];
                equipment.OffHand = ParseEquipmentSlotJson(secondaryRef);
            }
        }

        // Parse armor slots (note: keys are without "m_" prefix)
        equipment.Body = ParseEquipmentSlotJson(body["Armor"]);
        equipment.Head = ParseEquipmentSlotJson(body["Head"]);
        equipment.Neck = ParseEquipmentSlotJson(body["Neck"]);
        equipment.Belt = ParseEquipmentSlotJson(body["Belt"]);
        equipment.Cloak = ParseEquipmentSlotJson(body["Shoulders"]);
        equipment.Ring1 = ParseEquipmentSlotJson(body["Ring1"]);
        equipment.Ring2 = ParseEquipmentSlotJson(body["Ring2"]);
        equipment.Bracers = ParseEquipmentSlotJson(body["Wrist"]);
        equipment.Gloves = ParseEquipmentSlotJson(body["Gloves"]);
        equipment.Boots = ParseEquipmentSlotJson(body["Feet"]);

        // Parse quick slots (potions, scrolls, rods, wands)
        var quickSlots = body["m_QuickSlots"];
        if (quickSlots != null && quickSlots.HasValues)
        {
            var quickSlotsList = new List<EquipmentSlotJson>();
            foreach (var slotRef in quickSlots)
            {
                var slot = ParseEquipmentSlotJson(slotRef);
                if (slot != null)
                {
                    quickSlotsList.Add(slot);
                }
            }
            if (quickSlotsList.Any())
            {
                equipment.QuickSlots = quickSlotsList;
            }
        }

        return equipment;
    }

    private EquipmentSlotJson? ParseEquipmentSlotJson(JToken? slotRef)
    {
        if (slotRef == null || slotRef.Type == JTokenType.Null || _resolver == null)
            return null;

        // Resolve the slot reference
        var slot = _resolver.Resolve(slotRef);
        if (slot == null || slot.Type == JTokenType.Null)
            return null;

        // Check if slot has m_Item property (armor/accessory slots)
        JToken? item = null;
        if (slot is JObject slotObj && slotObj.Property("m_Item") != null)
        {
            var itemRef = slotObj["m_Item"];
            if (itemRef == null || itemRef.Type == JTokenType.Null)
                return null; // Slot exists but is empty
            
            item = _resolver.Resolve(itemRef);
        }
        else
        {
            // For weapon slots, the slot reference directly points to the item
            item = slot;
        }

        if (item == null || item.Type == JTokenType.Null)
            return null;

        var blueprintId = item["m_Blueprint"]?.ToString(); // NOTE: it's "m_Blueprint" not "Blueprint"!
        if (string.IsNullOrEmpty(blueprintId)) return null;

        var itemName = _blueprintLookup.GetName(blueprintId);
        if (string.IsNullOrEmpty(itemName) || itemName == "None") return null;

        var enchantments = new List<string>();
        var enchantmentsRef = item["m_Enchantments"];
        if (enchantmentsRef != null)
        {
            var enchantmentsObj = _resolver.Resolve(enchantmentsRef);
            if (enchantmentsObj != null && enchantmentsObj is JObject enchantmentsJObj)
            {
                var facts = enchantmentsJObj["m_Facts"];
                if (facts != null && facts.HasValues)
                {
                    foreach (var fact in facts)
                    {
                        if (!(fact is JObject)) continue;
                        
                        var enchBlueprintId = fact["Blueprint"]?.ToString();
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
        
        // Get spellbooks directly from descriptor (like the text parser does)
        var spellbooksArray = descriptor["m_Spellbooks"];
        if (spellbooksArray == null || !spellbooksArray.HasValues) return null;

        foreach (var spellbookEntry in spellbooksArray)
        {
            // Each entry has a "Value" that references the spellbook
            var spellbookData = spellbookEntry["Value"];
            if (spellbookData == null) continue;

            var spellbook = _resolver.Resolve(spellbookData);
            if (spellbook == null) continue;

            var classBlueprint = spellbook["Blueprint"]?.ToString();
            if (string.IsNullOrEmpty(classBlueprint)) continue;

            var className = _blueprintLookup.GetName(classBlueprint);
            var casterLevel = (int?)spellbook["m_CasterLevelInternal"] ?? 0;

            var spellbookJson = new SpellbookJson
            {
                ClassName = className,
                CasterLevel = casterLevel,
                SpellSlotsPerDay = new Dictionary<int, int>(),
                KnownSpells = new Dictionary<int, List<string>>()
            };

            // Parse spell slots per day
            var memorizedSpells = spellbook["m_MemorizedSpells"];
            if (memorizedSpells != null && memorizedSpells.HasValues)
            {
                int level = 0;
                foreach (var levelSlots in memorizedSpells)
                {
                    if (levelSlots != null && levelSlots.HasValues)
                    {
                        spellbookJson.SpellSlotsPerDay[level] = levelSlots.Count();
                    }
                    level++;
                }
            }

            // Parse known spells
            var knownSpells = spellbook["m_KnownSpells"];
            if (knownSpells != null && knownSpells.HasValues)
            {
                int level = 0;
                foreach (var levelSpells in knownSpells)
                {
                    if (levelSpells != null && levelSpells.HasValues)
                    {
                        var spellNames = new List<string>();
                        foreach (var spell in levelSpells)
                        {
                            var spellBlueprint = spell["Blueprint"]?.ToString();
                            if (!string.IsNullOrEmpty(spellBlueprint))
                            {
                                var spellName = _blueprintLookup.GetName(spellBlueprint);
                                if (!string.IsNullOrEmpty(spellName) && 
                                    !spellName.StartsWith("Blueprint_") && 
                                    spellName != "None")
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
                    level++;
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

    private List<ArtisanJson>? BuildArtisansJson(List<Artisan>? artisans)
    {
        if (artisans == null || artisans.Count == 0) return null;

        var result = new List<ArtisanJson>();
        foreach (var artisan in artisans)
        {
            if (string.IsNullOrEmpty(artisan.Blueprint)) continue;

            var artisanName = _blueprintLookup.GetName(artisan.Blueprint);
            if (string.IsNullOrEmpty(artisanName) || artisanName == artisan.Blueprint) continue;

            // Build previous items with type and enchantments
            var previousItems = new List<ArtisanItemJson>();
            if (artisan.PreviousItems != null)
            {
                foreach (var itemBlueprint in artisan.PreviousItems)
                {
                    var item = BuildArtisanItem(itemBlueprint);
                    if (item != null) previousItems.Add(item);
                }
            }

            // Build current production with type and enchantments
            var currentProduction = new List<ArtisanItemJson>();
            if (artisan.CurrentProduction != null)
            {
                foreach (var itemBlueprint in artisan.CurrentProduction)
                {
                    var item = BuildArtisanItem(itemBlueprint);
                    if (item != null) currentProduction.Add(item);
                }
            }

            // Count unlocked tiers
            int tiersUnlocked = artisan.TiersUnlocked?.Count(t => t) ?? 0;

            // Get help project event name if exists
            string? helpProjectEvent = null;
            if (!string.IsNullOrEmpty(artisan.HelpProjectEvent))
            {
                helpProjectEvent = _blueprintLookup.GetName(artisan.HelpProjectEvent);
                if (helpProjectEvent == artisan.HelpProjectEvent)
                    helpProjectEvent = null; // Don't show if name wasn't resolved
            }

            result.Add(new ArtisanJson
            {
                Name = artisanName,
                ProductionStartedOn = artisan.ProductionStartedOn,
                ProductionEndsOn = artisan.ProductionEndsOn,
                BuildingUnlocked = artisan.BuildingUnlocked,
                TiersUnlocked = tiersUnlocked,
                HelpProjectEvent = helpProjectEvent,
                PreviousItems = previousItems.Count > 0 ? previousItems : null,
                CurrentProduction = currentProduction.Count > 0 ? currentProduction : null
            });
        }

        return result.Count > 0 ? result : null;
    }

    private ArtisanItemJson? BuildArtisanItem(string itemBlueprint)
    {
        if (string.IsNullOrEmpty(itemBlueprint)) return null;

        var itemName = _blueprintLookup.GetName(itemBlueprint);
        if (string.IsNullOrEmpty(itemName) || itemName == itemBlueprint) return null;

        var itemType = _blueprintLookup.GetEquipmentType(itemBlueprint);

        return new ArtisanItemJson
        {
            Name = itemName,
            Type = itemType,
            Enchantments = null // Artisan items are blueprints, not instances, so no enchantment data
        };
    }
}
