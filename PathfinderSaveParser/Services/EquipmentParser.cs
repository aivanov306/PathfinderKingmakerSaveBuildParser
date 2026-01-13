using Newtonsoft.Json.Linq;
using System.Text;
using PathfinderSaveParser.Models;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Parses equipped items from character data
/// </summary>
public class EquipmentParser
{
    private readonly BlueprintLookupService _blueprintLookup;
    private readonly RefResolver _resolver;
    private readonly ReportOptions _options;

    public EquipmentParser(BlueprintLookupService blueprintLookup, RefResolver resolver, ReportOptions options)
    {
        _blueprintLookup = blueprintLookup;
        _resolver = resolver;
        _options = options;
    }

    /// <summary>
    /// Parse equipment data and return structured data object
    /// </summary>
    public EquipmentJson? ParseData(JToken? descriptor)
    {
        if (descriptor == null || !(descriptor is JObject descriptorObj))
            return null;

        var bodyRef = descriptorObj["Body"];
        if (bodyRef == null)
            return null;
        
        var body = _resolver.Resolve(bodyRef);
        if (body == null || body.Type == JTokenType.Null || !(body is JObject))
            return null;

        var equipment = new EquipmentJson();

        // Parse all weapon sets
        var sets = body["m_HandsEquipmentSets"];
        var activeIndex = body["m_CurrentHandsEquipmentSetIndex"]?.Value<int>() ?? 0;
        equipment.ActiveWeaponSetIndex = activeIndex;
        
        if (sets != null && sets.HasValues)
        {
            var weaponSets = new List<WeaponSetJson>();
            for (int i = 0; i < sets.Count(); i++)
            {
                var set = sets.ElementAtOrDefault(i);
                if (set is JObject)
                {
                    var primaryRef = set["PrimaryHand"];
                    var secondaryRef = set["SecondaryHand"];
                    
                    var mainHand = ParseEquipmentSlotData(primaryRef);
                    var offHand = ParseEquipmentSlotData(secondaryRef);
                    
                    // Only add non-empty weapon sets
                    if (mainHand != null || offHand != null)
                    {
                        weaponSets.Add(new WeaponSetJson
                        {
                            SetNumber = i + 1,
                            MainHand = mainHand,
                            OffHand = offHand
                        });
                    }
                    
                    // Keep legacy properties for backward compatibility (active set only)
                    if (i == activeIndex)
                    {
                        equipment.MainHand = mainHand;
                        equipment.OffHand = offHand;
                    }
                }
            }
            
            if (weaponSets.Any())
            {
                equipment.WeaponSets = weaponSets;
            }
        }

        // Parse armor slots (note: keys are without "m_" prefix)
        equipment.Body = ParseEquipmentSlotData(body["Armor"]);
        equipment.Head = ParseEquipmentSlotData(body["Head"]);
        equipment.Neck = ParseEquipmentSlotData(body["Neck"]);
        equipment.Belt = ParseEquipmentSlotData(body["Belt"]);
        equipment.Cloak = ParseEquipmentSlotData(body["Shoulders"]);
        equipment.Ring1 = ParseEquipmentSlotData(body["Ring1"]);
        equipment.Ring2 = ParseEquipmentSlotData(body["Ring2"]);
        equipment.Bracers = ParseEquipmentSlotData(body["Wrist"]);
        equipment.Gloves = ParseEquipmentSlotData(body["Gloves"]);
        equipment.Boots = ParseEquipmentSlotData(body["Feet"]);

        // Parse quick slots (potions, scrolls, rods, wands)
        var quickSlots = body["m_QuickSlots"];
        if (quickSlots != null && quickSlots.HasValues)
        {
            var quickSlotsList = new List<EquipmentSlotJson>();
            foreach (var slotRef in quickSlots)
            {
                var slot = ParseEquipmentSlotData(slotRef);
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

    /// <summary>
    /// Parse equipment and format as text report (uses ParseData internally)
    /// </summary>
    public string ParseEquipment(JToken? descriptor)
    {
        var data = ParseData(descriptor);
        if (data == null)
            return "";

        var sb = new StringBuilder();
        sb.AppendLine("EQUIPMENT");
        sb.AppendLine(new string('=', 80));
        
        // Format active weapon set
        if (_options.IncludeActiveWeaponSet && data.WeaponSets != null && data.WeaponSets.Any())
        {
            var activeSet = data.WeaponSets.FirstOrDefault(ws => ws.SetNumber == data.ActiveWeaponSetIndex + 1);
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
                ("Body", data.Body, true),
                ("Head", data.Head, false),
                ("Neck", data.Neck, false),
                ("Belt", data.Belt, false),
                ("Cloak", data.Cloak, false),
                ("Ring 1", data.Ring1, false),
                ("Ring 2", data.Ring2, false),
                ("Bracers", data.Bracers, false),
                ("Gloves", data.Gloves, false),
                ("Boots", data.Boots, false)
            };

            foreach (var (name, slot, isArmor) in slots)
            {
                // Skip if armor is disabled and this is armor slot
                if (isArmor && !_options.IncludeArmor) continue;
                
                // Skip if accessories are disabled and this is not armor slot
                if (!isArmor && !_options.IncludeAccessories) continue;
                
                var itemInfo = FormatEquipmentSlot(slot);
                
                // Skip empty slots if ShowEmptySlots is false
                if (!_options.ShowEmptySlots && itemInfo == "(empty)") continue;
                
                sb.AppendLine($"  {name,-10}: {itemInfo}");
            }
        }
        
        // Format quick slots
        if (data.QuickSlots != null && data.QuickSlots.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Quick Slots (Potions, Scrolls, Rods, Wands):");
            int slotNumber = 1;
            foreach (var slot in data.QuickSlots)
            {
                var itemInfo = FormatEquipmentSlot(slot);
                if (itemInfo != "(empty)")
                {
                    sb.AppendLine($"  Slot {slotNumber}: {itemInfo}");
                }
                slotNumber++;
            }
        }
        
        return sb.ToString();
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

    /// <summary>
    /// Parse equipment slot and return structured data
    /// </summary>
    private EquipmentSlotJson? ParseEquipmentSlotData(JToken? slotRef)
    {
        if (slotRef == null || slotRef.Type == JTokenType.Null)
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

        if (item == null || item.Type == JTokenType.Null || !(item is JObject itemObj))
            return null;

        var blueprintId = itemObj["m_Blueprint"]?.ToString();
        if (string.IsNullOrEmpty(blueprintId))
            return null;

        var (itemName, equipmentType) = _blueprintLookup.GetNameAndType(blueprintId);
        if (string.IsNullOrEmpty(itemName) || itemName.StartsWith("Blueprint_"))
            return null;

        // Get description if ShowItemDescriptions is enabled
        var description = _options.ShowItemDescriptions ? _blueprintLookup.GetDescription(blueprintId) : null;

        var result = new EquipmentSlotJson
        {
            Name = itemName,
            Type = equipmentType,
            Description = description
        };

        // Parse enchantments
        var enchantments = new List<string>();
        var enchantmentsRef = itemObj["m_Enchantments"];
        var enchantmentsObj = _resolver.Resolve(enchantmentsRef);
        
        if (enchantmentsObj != null && enchantmentsObj is JObject enchantmentsJObj)
        {
            var facts = enchantmentsJObj["m_Facts"];
            if (facts != null && facts.HasValues)
            {
                foreach (var fact in facts)
                {
                    if (!(fact is JObject)) continue;
                    
                    var enchantBlueprint = fact["Blueprint"]?.ToString();
                    if (!string.IsNullOrEmpty(enchantBlueprint))
                    {
                        var enchantName = _blueprintLookup.GetName(enchantBlueprint);
                        
                        // Filter out redundant enhancement bonuses
                        if (ShouldIncludeEnchantment(enchantName, itemName))
                        {
                            enchantments.Add(enchantName);
                        }
                    }
                }
            }
        }
        
        if (enchantments.Any())
        {
            result.Enchantments = enchantments;
        }

        return result;
    }

    private bool ShouldIncludeEnchantment(string enchantName, string itemName)
    {
        // Skip generic enhancement bonuses if they're already in the item name
        if (enchantName.Contains("Enhancement Bonus") && 
            (itemName.Contains("+1") || itemName.Contains("+2") || 
             itemName.Contains("+3") || itemName.Contains("+4") || 
             itemName.Contains("+5") || itemName.Contains("+6")))
        {
            return false;
        }

        // Skip armor enhancement bonuses already in item name
        if (enchantName.Contains("Armor Enhancement Bonus") &&
            (itemName.Contains("+1") || itemName.Contains("+2") ||
             itemName.Contains("+3") || itemName.Contains("+4") ||
             itemName.Contains("+5") || itemName.Contains("+6")))
        {
            return false;
        }

        return true;
    }
}
