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
    private readonly ItemCategorizationService _categorization;
    private readonly EquipmentParser _equipmentParser;
    private readonly EnhancedCharacterParser _characterParser;
    private readonly string? _mainCharacterUniqueId;

    public JsonOutputBuilder(BlueprintLookupService blueprintLookup, RefResolver? resolver = null, ReportOptions? options = null, string? mainCharacterUniqueId = null)
    {
        _blueprintLookup = blueprintLookup;
        _resolver = resolver;
        _options = options ?? new ReportOptions();
        _categorization = new ItemCategorizationService();
        _equipmentParser = new EquipmentParser(blueprintLookup, resolver ?? throw new ArgumentNullException(nameof(resolver)), _options);
        _characterParser = new EnhancedCharacterParser(blueprintLookup, resolver ?? throw new ArgumentNullException(nameof(resolver)), _options, mainCharacterUniqueId);
        _mainCharacterUniqueId = mainCharacterUniqueId;
    }

    public KingdomStatsJson? BuildKingdomJson(Kingdom? kingdom, int money, string? gameTime, int? bpPerTurnOverride)
    {
        if (kingdom == null) return null;

        var json = new KingdomStatsJson
        {
            Name = kingdom.KingdomName,
            Alignment = kingdom.Alignment,
            KingdomDays = kingdom.CurrentDay,
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

    private List<(string blueprint, int count, List<string>? enchantments, string? jsonType)> ParseItemsFromCollectionWithEnchantments(ItemCollection? collection)
    {
        var items = new List<(string blueprint, int count, List<string>? enchantments, string? jsonType)>();
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
                items.Add((item.Blueprint, item.Count, enchantments.Any() ? enchantments : null, null)); // null jsonType for personal chest items
            }
        }

        return items;
    }

    private List<(string blueprint, int count, List<string>? enchantments, string? jsonType)> ParseItemsFromPartyWithEnchantments(JToken? partyJson)
    {
        var items = new List<(string blueprint, int count, List<string>? enchantments, string? jsonType)>();
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
                    var jsonType = item["$type"]?.Value<string>(); // Get item type from JSON

                    if (!string.IsNullOrEmpty(blueprint))
                    {
                        // Parse enchantments from m_Enchantments.m_Facts[]
                        var enchantments = new List<string>();
                        try
                        {
                            var enchantsToken = item["m_Enchantments"];
                            if (enchantsToken != null && enchantsToken.Type == JTokenType.Object)
                            {
                                var factsArray = enchantsToken["m_Facts"];
                                
                                if (factsArray != null && factsArray is JArray arr && arr.Any())
                                {
                                    foreach (var fact in arr)
                                    {
                                        var enchantBlueprint = fact?["Blueprint"]?.Value<string>();
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

                        items.Add((blueprint, count, enchantments.Any() ? enchantments : null, jsonType));
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

    private InventoryCollectionJson BuildInventoryCollectionJson(List<(string blueprint, int count, List<string>? enchantments, string? jsonType)> items)
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
        foreach (var (blueprint, count, enchantments, jsonType) in items)
        {
            var name = _blueprintLookup.GetName(blueprint);
            if (name == blueprint)
            {
                skippedCount++;
                continue; // Skip unknown items
            }

            var type = _blueprintLookup.GetEquipmentType(blueprint);
            var blueprintType = _blueprintLookup.GetBlueprintType(blueprint);
            
            // Extract slot type from blueprint type for accessories and notes
            var displayType = GetDisplayType(blueprintType, type);
            
            // Get description if ShowItemDescriptions is enabled
            var description = _options.ShowItemDescriptions ? _blueprintLookup.GetDescription(blueprint) : null;
            
            var item = new InventoryItemJson
            {
                Name = name,
                Type = displayType,
                Count = count,
                Enchantments = enchantments,
                Description = description
            };

            // Categorize using priority: 1) Blueprint type (most reliable), 2) JSON $type, 3) equipment type, 4) name-based fallback
            var categoryFromBlueprint = _categorization.GetCategoryFromBlueprintType(blueprintType);
            var categoryFromJsonType = _categorization.GetCategoryFromJsonType(jsonType);
            
            if (categoryFromBlueprint == "Weapon")
                collection.Weapons!.Add(item);
            else if (categoryFromBlueprint == "Armor")
                collection.Armor!.Add(item);
            else if (categoryFromBlueprint == "Usable")
                collection.Usables!.Add(item);
            else if (categoryFromBlueprint == "Accessories")
                collection.Accessories!.Add(item);
            else if (categoryFromBlueprint == "Other")
                collection.Other!.Add(item);
            else if (categoryFromJsonType == "Weapon")
                collection.Weapons!.Add(item);
            else if (categoryFromJsonType == "Armor")
                collection.Armor!.Add(item);
            else if (categoryFromJsonType == "Usable")
                collection.Usables!.Add(item);
            else if (_categorization.IsWeapon(type))
                collection.Weapons!.Add(item);
            else if (_categorization.IsArmor(type))
                collection.Armor!.Add(item);
            else if (_categorization.IsUsable(name))
                collection.Usables!.Add(item);
            else if (_categorization.IsAccessory(name))
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
        // Delegate to CharacterParser which now has a ParseData method
        return _characterParser.ParseData(rawUnit);
    }

    private EquipmentJson? ParseEquipmentJson(JToken descriptor)
    {
        // Delegate to EquipmentParser which now has a ParseData method
        return _equipmentParser.ParseData(descriptor);
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

    /// <summary>
    /// Formats weapon category names with proper spacing (e.g., HeavyMace -> Heavy Mace)
    /// </summary>
    private string FormatWeaponCategory(string weaponCategory)
    {
        if (string.IsNullOrEmpty(weaponCategory)) return weaponCategory;
        
        // Insert space before uppercase letters (except first)
        var result = System.Text.RegularExpressions.Regex.Replace(
            weaponCategory, 
            "([a-z])([A-Z])", 
            "$1 $2"
        );
        
        return result;
    }

    /// <summary>
    /// Normalizes parameterized feat names to use parentheses format
    /// (e.g., "Skill Focus Thievery" -> "Skill Focus (Thievery)")
    /// </summary>
    private string NormalizeParameterizedFeatName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        
        // List of feat prefixes that should use parentheses format
        var featPrefixes = new[]
        {
            "Skill Focus ",
            "Exotic Weapon Proficiency ",
            "Martial Weapon Proficiency ",
            "Simple Weapon Proficiency "
        };
        
        foreach (var prefix in featPrefixes)
        {
            if (name.StartsWith(prefix))
            {
                var parameter = name.Substring(prefix.Length);
                // Only format if there are no parentheses already
                if (!parameter.Contains("(") && !parameter.Contains(")"))
                {
                    return prefix.TrimEnd() + " (" + parameter + ")";
                }
            }
        }
        
        return name;
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

    /// <summary>
    /// Extracts display type from blueprint type for accessories and notes
    /// </summary>
    private string GetDisplayType(string? blueprintType, string? equipmentType)
    {
        if (string.IsNullOrEmpty(blueprintType))
            return equipmentType ?? "";

        // For notes, show "Note"
        if (blueprintType.Equals("BlueprintItemNote", StringComparison.OrdinalIgnoreCase))
            return "Note";

        // For equipment accessories, extract slot name
        if (blueprintType.StartsWith("BlueprintItemEquipment", StringComparison.OrdinalIgnoreCase))
        {
            // BlueprintItemEquipmentRing -> Ring
            // BlueprintItemEquipmentHead -> Head
            // BlueprintItemEquipmentNeck -> Neck
            // etc.
            var slotName = blueprintType.Substring("BlueprintItemEquipment".Length);
            if (!string.IsNullOrEmpty(slotName) && !slotName.Equals("Usable", StringComparison.OrdinalIgnoreCase))
                return slotName;
        }

        // For weapons and armor, use equipment type (e.g., "Longsword", "Breastplate")
        return equipmentType ?? "";
    }
}
