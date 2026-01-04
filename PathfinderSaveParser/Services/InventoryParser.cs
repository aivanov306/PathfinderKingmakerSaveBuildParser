using System.Text;
using PathfinderSaveParser.Models;
using Newtonsoft.Json.Linq;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Parses inventory items from the shared stash (personal chest) and party shared inventory
/// </summary>
public class InventoryParser
{
    private readonly BlueprintLookupService _blueprintLookup;

    public InventoryParser(BlueprintLookupService blueprintLookup)
    {
        _blueprintLookup = blueprintLookup;
    }

    public string ParseBothInventories(ItemCollection? personalChest, JToken? partyJson)
    {
        var sb = new StringBuilder();
        sb.AppendLine("INVENTORY");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();
        
        // Parse Personal Chest
        sb.AppendLine("PERSONAL CHEST (SharedStash):");
        sb.AppendLine(new string('-', 80));
        var personalItems = ParseInventoryItems(personalChest);
        if (personalItems.Any())
        {
            AppendCategorizedItems(sb, personalItems);
        }
        else
        {
            sb.AppendLine("(empty)");
        }
        
        sb.AppendLine();
        sb.AppendLine();
        
        // Parse Shared Party Inventory
        sb.AppendLine("SHARED PARTY INVENTORY:");
        sb.AppendLine(new string('-', 80));
        var sharedItems = ParseSharedInventoryFromParty(partyJson);
        if (sharedItems.Any())
        {
            AppendCategorizedItems(sb, sharedItems);
        }
        else
        {
            sb.AppendLine("(empty)");
        }
        
        return sb.ToString();
    }

    private List<(string blueprint, int count, List<string>? enchantments)> ParseInventoryItems(ItemCollection? inventory)
    {
        var items = new List<(string blueprint, int count, List<string>? enchantments)>();
        
        if (inventory?.Items == null || inventory.Items.Count == 0)
        {
            return items;
        }

        foreach (var item in inventory.Items)
        {
            if (item?.Blueprint == null) continue;
            
            // Get enchantments from the item's Enchantments list
            List<string>? enchantments = null;
            if (item.Enchantments?.Facts != null && item.Enchantments.Facts.Any())
            {
                enchantments = new List<string>();
                foreach (var enchantRef in item.Enchantments.Facts)
                {
                    if (!string.IsNullOrEmpty(enchantRef.Blueprint))
                    {
                        var enchantName = _blueprintLookup.GetName(enchantRef.Blueprint);
                        if (enchantName != enchantRef.Blueprint)
                        {
                            enchantments.Add(enchantName);
                        }
                    }
                }
            }
            
            items.Add((item.Blueprint, item.Count, enchantments?.Any() == true ? enchantments : null));
        }
        
        return items;
    }

    private List<(string blueprint, int count, List<string>? enchantments)> ParseSharedInventoryFromParty(JToken? partyJson)
    {
        var items = new List<(string blueprint, int count, List<string>? enchantments)>();
        
        if (partyJson == null) return items;

        try
        {
            // Navigate to m_EntityData[0].Descriptor.m_Inventory.m_Items
            var entityData = partyJson["m_EntityData"];
            if (entityData == null || !entityData.Any()) return items;

            var firstEntity = entityData.First;
            var descriptor = firstEntity?["Descriptor"];
            var inventory = descriptor?["m_Inventory"];
            var itemsArray = inventory?["m_Items"];

            if (itemsArray == null || !itemsArray.Any()) return items;

            foreach (var item in itemsArray)
            {
                try
                {
                    // Skip null items or items without required properties
                    if (item == null || item.Type == JTokenType.Null) continue;

                    // Get inventory slot index - only include items with index >= 0 (shared inventory)
                    // Items with index -1 are equipped items
                    var slotIndex = item["m_InventorySlotIndex"]?.Value<int>();
                    if (slotIndex == null || slotIndex < 0) continue;

                    var blueprint = item["m_Blueprint"]?.Value<string>();
                    var count = item["m_Count"]?.Value<int>() ?? 1;

                    if (!string.IsNullOrEmpty(blueprint))
                    {
                        // Parse enchantments from m_Enchantments.m_Facts[] (same structure as personal chest)
                        var enchantments = new List<string>();
                        try
                        {
                            var enchantsToken = item["m_Enchantments"];
                            if (enchantsToken != null && enchantsToken.Type == JTokenType.Object)
                            {
                                // Look for m_Facts array inside m_Enchantments
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
                            // If enchantment parsing fails, continue with empty enchantments list
                        }
                        
                        items.Add((blueprint, count, enchantments.Any() ? enchantments : null));
                    }
                }
                catch
                {
                    // If parsing this item fails, skip it and continue
                    continue;
                }
            }
        }
        catch (Exception)
        {
            // If parsing fails, return empty list
            return new List<(string blueprint, int count, List<string>? enchantments)>();
        }

        return items;
    }

    private void AppendCategorizedItems(StringBuilder sb, List<(string blueprint, int count, List<string>? enchantments)> items)
    {
        // Group items by category
        var weapons = new List<(string name, string? type, int count, List<string>? enchantments)>();
        var armor = new List<(string name, string? type, int count, List<string>? enchantments)>();
        var accessories = new List<(string name, int count, List<string>? enchantments)>();
        var usables = new List<(string name, int count)>();
        var other = new List<(string name, int count)>();

        foreach (var (blueprint, count, enchantments) in items)
        {
            var itemName = _blueprintLookup.GetName(blueprint);
            if (itemName == blueprint) continue; // Skip unknown items

            var equipmentType = _blueprintLookup.GetEquipmentType(blueprint);

            // Categorize items (order matters: check specific types first)
            if (IsWeapon(equipmentType))
            {
                weapons.Add((itemName, equipmentType, count, enchantments));
            }
            else if (IsArmor(equipmentType))
            {
                armor.Add((itemName, equipmentType, count, enchantments));
            }
            else if (IsUsable(itemName))
            {
                usables.Add((itemName, count));
            }
            else if (IsAccessory(itemName))
            {
                accessories.Add((itemName, count, enchantments));
            }
            else
            {
                other.Add((itemName, count));
            }
        }

        // Print weapons
        if (weapons.Count > 0)
        {
            sb.AppendLine("WEAPONS:");
            foreach (var (name, type, count, enchantments) in weapons.OrderBy(w => w.name))
            {
                var countStr = count > 1 ? $" x{count}" : "";
                var typeStr = !string.IsNullOrEmpty(type) ? $" [{type}]" : "";
                var enchantStr = enchantments != null && enchantments.Any() ? $" ({string.Join(", ", enchantments)})" : "";
                sb.AppendLine($"  {name}{typeStr}{enchantStr}{countStr}");
            }
            sb.AppendLine();
        }

        // Print armor
        if (armor.Count > 0)
        {
            sb.AppendLine("ARMOR & SHIELDS:");
            foreach (var (name, type, count, enchantments) in armor.OrderBy(a => a.name))
            {
                var countStr = count > 1 ? $" x{count}" : "";
                var typeStr = !string.IsNullOrEmpty(type) ? $" [{type}]" : "";
                var enchantStr = enchantments != null && enchantments.Any() ? $" ({string.Join(", ", enchantments)})" : "";
                sb.AppendLine($"  {name}{typeStr}{enchantStr}{countStr}");
            }
            sb.AppendLine();
        }

        // Print accessories (belts, amulets, rings, etc.)
        if (accessories.Count > 0)
        {
            sb.AppendLine("ACCESSORIES (Belts, Amulets, Rings, etc.):");
            foreach (var (name, count, enchantments) in accessories.OrderBy(a => a.name))
            {
                var countStr = count > 1 ? $" x{count}" : "";
                var enchantStr = enchantments != null && enchantments.Any() ? $" ({string.Join(", ", enchantments)})" : "";
                sb.AppendLine($"  {name}{enchantStr}{countStr}");
            }
            sb.AppendLine();
        }

        // Print usables (potions, scrolls, etc.)
        if (usables.Count > 0)
        {
            sb.AppendLine("USABLES (Potions, Scrolls, Flasks, etc.):");
            foreach (var (name, count) in usables.OrderBy(u => u.name))
            {
                var countStr = count > 1 ? $" x{count}" : "";
                sb.AppendLine($"  {name}{countStr}");
            }
            sb.AppendLine();
        }

        // Print other items
        if (other.Count > 0)
        {
            sb.AppendLine("OTHER ITEMS:");
            foreach (var (name, count) in other.OrderBy(o => o.name))
            {
                var countStr = count > 1 ? $" x{count}" : "";
                sb.AppendLine($"  {name}{countStr}");
            }
            sb.AppendLine();
        }

        // Summary
        var totalUniqueItems = weapons.Count + armor.Count + accessories.Count + usables.Count + other.Count;
        var totalItems = items.Sum(i => i.count);
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"Total: {totalItems} items ({totalUniqueItems} unique)");
        sb.AppendLine();
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
            "Dart", "Javelin", "Throwing Axe", "Sling",
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
        var accessoryKeywords = new[]
        {
            "Amulet", "Belt", "Ring", "Bracers", "Cloak", "Headband",
            "Circlet", "Helmet", "Gloves", "Boots"
        };

        return accessoryKeywords.Any(keyword => name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsUsable(string name)
    {
        var usableKeywords = new[]
        {
            "Potion", "Scroll", "Elixir", "Extract", "Wand",
            "Oil of ", "Antidote", "Antitoxin", "Holy Water",
            "Flask", "Alchemist", "Alchemists Fire"
        };

        return usableKeywords.Any(keyword => name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
