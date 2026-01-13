namespace PathfinderSaveParser.Services;

/// <summary>
/// Service for categorizing items by type (weapons, armor, accessories, usables)
/// Shared logic to avoid duplication across parsers
/// </summary>
public class ItemCategorizationService
{
    /// <summary>
    /// Determines item category from JSON $type field (more reliable than name-based detection)
    /// </summary>
    public string? GetCategoryFromJsonType(string? jsonType)
    {
        if (string.IsNullOrEmpty(jsonType)) return null;

        if (jsonType.Contains("ItemEntityWeapon", StringComparison.OrdinalIgnoreCase))
            return "Weapon";
        if (jsonType.Contains("ItemEntityArmor", StringComparison.OrdinalIgnoreCase))
            return "Armor";
        if (jsonType.Contains("ItemEntityShield", StringComparison.OrdinalIgnoreCase))
            return "Armor"; // Shields are armor
        if (jsonType.Contains("ItemEntityUsable", StringComparison.OrdinalIgnoreCase))
            return "Usable";
        if (jsonType.Contains("ItemEntitySimple", StringComparison.OrdinalIgnoreCase))
            return null; // Simple items need further categorization

        return null;
    }

    public bool IsWeapon(string? type)
    {
        if (string.IsNullOrEmpty(type)) return false;

        var weaponTypes = new[]
        {
            "Longsword", "Shortsword", "Greatsword", "Bastard Sword", "Dueling Sword",
            "Dagger", "Kukri", "Punching Dagger", "Sickle", "Starknife",
            "Battleaxe", "Handaxe", "Greataxe", "Warhammer", "Light Hammer", "Dwarven Waraxe",
            "Heavy Flail", "Light Flail", "Flail", "Greatclub", "Club", "Heavy Mace", "Light Mace",
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

    public bool IsWeaponByName(string name)
    {
        // Fallback for items without equipment type in database
        var weaponKeywords = new[] { "Flail", "Waraxe", "Sword", "Axe", "Mace", "Bow", "Crossbow" };
        return weaponKeywords.Any(kw => name.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsArmor(string? type)
    {
        if (string.IsNullOrEmpty(type)) return false;

        var armorTypes = new[]
        {
            "Light Armor", "Medium Armor", "Heavy Armor",
            "Buckler", "Light Shield", "Heavy Shield", "Tower Shield",
            "Padded", "Leather", "Studded", "Chainshirt", "Chain Shirt", "Hide", "Scale Mail",
            "Chainmail", "Breastplate", "Splint Mail", "Banded Mail", "Half-Plate", "Halfplate", "Fullplate", "Full Plate",
            "Cloth", "Ring Mail"
        };

        return armorTypes.Any(at => type.Contains(at, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsArmorByName(string name)
    {
        // Fallback for items without equipment type in database
        var armorKeywords = new[] { "plate", "mail", "armor", "shield" };
        return armorKeywords.Any(kw => name.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsAccessory(string name)
    {
        // Use exact word matching for shorter keywords that can be part of other words
        var exactMatchKeywords = new[] { "Ring", "Belt", "Hat", "Cap" };
        var containsKeywords = new[] { "Amulet", "Bracers", "Cloak", "Headband", "Circlet", "Helmet", "Gloves", "Boots", "Companion" };

        // Check for exact word matches (with word boundaries)
        foreach (var keyword in exactMatchKeywords)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(name, $@"\b{keyword}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return true;
        }

        // Check for contains matches
        return containsKeywords.Any(keyword => name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsUsable(string name)
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
