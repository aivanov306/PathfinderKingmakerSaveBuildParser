using Newtonsoft.Json.Linq;
using System.Text;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Parses equipped items from character data
/// </summary>
public class EquipmentParser
{
    private readonly BlueprintLookupService _blueprintLookup;
    private readonly RefResolver _resolver;

    public EquipmentParser(BlueprintLookupService blueprintLookup, RefResolver resolver)
    {
        _blueprintLookup = blueprintLookup;
        _resolver = resolver;
    }

    public string ParseEquipment(JToken? descriptor)
    {
        var sb = new StringBuilder();
        
        // Check if descriptor is a valid object before accessing properties
        if (descriptor == null || !(descriptor is JObject descriptorObj))
        {
            return "";
        }

        // Get the Body reference and resolve it
        var bodyRef = descriptorObj["Body"];
        
        if (bodyRef == null)
        {
            return "";
        }
        
        var body = _resolver.Resolve(bodyRef);
        
        if (body == null || body.Type == JTokenType.Null)
        {
            return "";
        }
        
        if (!(body is JObject))
        {
            return "";
        }
        
        sb.AppendLine("EQUIPMENT");
        sb.AppendLine(new string('=', 80));
        
        // Parse active weapon set only
        ParseActiveWeaponSet(body, sb);
        
        // Parse armor and accessories
        ParseArmorAndAccessories(body, sb);
        
        return sb.ToString();
    }

    private void ParseActiveWeaponSet(JToken body, StringBuilder sb)
    {
        if (body == null || body.Type != JTokenType.Object) return;
        
        var sets = body["m_HandsEquipmentSets"];
        var activeIndex = body["m_CurrentHandsEquipmentSetIndex"]?.Value<int>() ?? 0;
        
        if (sets != null && sets.HasValues && sets.Count() > activeIndex)
        {
            var activeSet = sets.ElementAtOrDefault(activeIndex);
            
            if (activeSet == null || !(activeSet is JObject)) return;
            
            sb.AppendLine();
            sb.AppendLine($"Active Weapon Set (Set {activeIndex + 1}):");
            
            // Primary hand
            var primaryRef = activeSet["PrimaryHand"];
            var primary = GetItemFromSlot(primaryRef);
            var primaryInfo = GetItemInfo(primary);
            sb.AppendLine($"  Main Hand:  {primaryInfo}");
            
            // Secondary hand
            var secondaryRef = activeSet["SecondaryHand"];
            var secondary = GetItemFromSlot(secondaryRef);
            var secondaryInfo = GetItemInfo(secondary);
            sb.AppendLine($"  Off Hand:   {secondaryInfo}");
        }
    }

    private void ParseArmorAndAccessories(JToken body, StringBuilder sb)
    {
        if (body == null || body.Type != JTokenType.Object) return;
        
        sb.AppendLine();
        sb.AppendLine("Armor & Accessories:");
        
        var slots = new Dictionary<string, string>
        {
            { "Armor", "Body" },
            { "Head", "Head" },
            { "Neck", "Neck" },
            { "Belt", "Belt" },
            { "Shoulders", "Cloak" },
            { "Ring1", "Ring 1" },
            { "Ring2", "Ring 2" },
            { "Wrist", "Bracers" },
            { "Gloves", "Gloves" },
            { "Feet", "Boots" }
        };

        foreach (var slot in slots)
        {
            var itemRef = body[slot.Key];
            var item = GetItemFromSlot(itemRef);
            var itemInfo = GetItemInfo(item);
            
            sb.AppendLine($"  {slot.Value,-10}: {itemInfo}");
        }
    }

    private JToken? GetItemFromSlot(JToken? slotRef)
    {
        if (slotRef == null || slotRef.Type == JTokenType.Null)
        {
            return null; // Slot reference doesn't exist
        }
        
        var slot = _resolver.Resolve(slotRef);
        if (slot == null || slot.Type == JTokenType.Null)
        {
            return null; // Slot is empty
        }
        
        // Check if slot is a reference-only object (just has $ref) or actual slot object
        if (slot is JObject slotObj && slotObj.Property("m_Item") != null)
        {
            var itemRef = slotObj["m_Item"];
            if (itemRef == null || itemRef.Type == JTokenType.Null)
            {
                return null; // Slot exists but has no item
            }
            return _resolver.Resolve(itemRef);
        }
        
        return null;
    }

    private string GetItemInfo(JToken? item)
    {
        // Item is null means slot is empty
        if (item == null)
        {
            return "(empty)";
        }
        
        // Item is not a JObject means we got unexpected data
        if (!(item is JObject itemObj))
        {
            return "(empty)";
        }
        
        var blueprintId = itemObj["m_Blueprint"]?.ToString();
        
        // If we can't get a blueprint ID, the item data is malformed
        if (string.IsNullOrEmpty(blueprintId))
        {
            return "(empty)";
        }
        
        var itemName = _blueprintLookup.GetName(blueprintId);
        
        // If blueprint lookup fails, show as unknown
        if (string.IsNullOrEmpty(itemName) || itemName.StartsWith("Blueprint_"))
        {
            return $"(unknown item: {blueprintId.Substring(0, Math.Min(8, blueprintId.Length))}...)";
        }
        
        // Get enchantments
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
                    // Ensure fact is a JObject before accessing properties
                    if (!(fact is JObject)) continue;
                    
                    var enchantBlueprint = fact["Blueprint"]?.ToString();
                    if (!string.IsNullOrEmpty(enchantBlueprint))
                    {
                        var enchantName = _blueprintLookup.GetName(enchantBlueprint);
                        
                        // Filter out redundant enhancement bonuses that are already in item name
                        // (e.g., if item is "Flaming Longsword +2", skip "Enhancement Bonus +2")
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
            return $"{itemName} ({string.Join(", ", enchantments)})";
        }
        
        return itemName;
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
