namespace PathfinderSaveParser.Models;

/// <summary>
/// Combined JSON output containing all game state data
/// </summary>
public class CurrentStateJson
{
    public KingdomStatsJson? Kingdom { get; set; }
    public List<CharacterJson>? Characters { get; set; }
    public List<SettlementJson>? Settlements { get; set; }
    public List<string>? ExploredLocations { get; set; }
    public InventoryJson? Inventory { get; set; }
}

/// <summary>
/// Kingdom statistics in JSON format
/// </summary>
public class KingdomStatsJson
{
    public string? Name { get; set; }
    public string? Alignment { get; set; }
    public int KingdomDays { get; set; }
    public int Gold { get; set; }
    public int BuildPoints { get; set; }
    public int? BuildPointsPerTurn { get; set; }
    public string? UnrestLevel { get; set; }
    public List<KingdomStatJson>? Stats { get; set; }
    public List<AdvisorJson>? Advisors { get; set; }
}

/// <summary>
/// Individual kingdom stat
/// </summary>
public class KingdomStatJson
{
    public string? Type { get; set; }
    public int Value { get; set; }
    public int Rank { get; set; }
}

/// <summary>
/// Kingdom advisor assignment
/// </summary>
public class AdvisorJson
{
    public string? Position { get; set; }
    public string? Advisor { get; set; }
    public string? Status { get; set; } // "Assigned", "Vacant", "Locked"
}

/// <summary>
/// Inventory data in JSON format
/// </summary>
public class InventoryJson
{
    public InventoryCollectionJson? PersonalChest { get; set; }
    public InventoryCollectionJson? SharedInventory { get; set; }
}

/// <summary>
/// Collection of categorized inventory items
/// </summary>
public class InventoryCollectionJson
{
    public List<InventoryItemJson>? Weapons { get; set; }
    public List<InventoryItemJson>? Armor { get; set; }
    public List<InventoryItemJson>? Accessories { get; set; }
    public List<InventoryItemJson>? Usables { get; set; }
    public List<InventoryItemJson>? Other { get; set; }
    public int TotalItems { get; set; }
    public int UniqueItems { get; set; }
}

/// <summary>
/// Single inventory item
/// </summary>
public class InventoryItemJson
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public int Count { get; set; }
    public List<string>? Enchantments { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Character data in JSON format
/// </summary>
public class CharacterJson
{
    public string? Name { get; set; }
    public string? Race { get; set; }
    public string? Alignment { get; set; }
    public List<ClassInfoJson>? Classes { get; set; }
    public AttributesJson? Attributes { get; set; }
    public SkillsJson? Skills { get; set; }
    public EquipmentJson? Equipment { get; set; }
    public List<SpellbookJson>? Spellbooks { get; set; }
    public string? FormattedSpellcasting { get; set; }  // Full formatted text from SpellbookParser
    public List<LevelProgressionJson>? LevelProgression { get; set; }
}

/// <summary>
/// Class information
/// </summary>
public class ClassInfoJson
{
    public string? ClassName { get; set; }
    public string? Archetype { get; set; }
    public int Level { get; set; }
}

/// <summary>
/// Character attributes
/// </summary>
public class AttributesJson
{
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }
}

/// <summary>
/// Character skills
/// </summary>
public class SkillsJson
{
    public int? Mobility { get; set; }
    public int? Athletics { get; set; }
    public int? Stealth { get; set; }
    public int? Thievery { get; set; }
    public int? KnowledgeArcana { get; set; }
    public int? KnowledgeWorld { get; set; }
    public int? LoreNature { get; set; }
    public int? LoreReligion { get; set; }
    public int? Perception { get; set; }
    public int? Persuasion { get; set; }
    public int? UseMagicDevice { get; set; }
}

/// <summary>
/// Equipped items
/// </summary>
public class EquipmentJson
{
    public List<WeaponSetJson>? WeaponSets { get; set; }
    public int ActiveWeaponSetIndex { get; set; }
    
    // Legacy properties for backward compatibility (active set only)
    public EquipmentSlotJson? MainHand { get; set; }
    public EquipmentSlotJson? OffHand { get; set; }
    
    public EquipmentSlotJson? Body { get; set; }
    public EquipmentSlotJson? Head { get; set; }
    public EquipmentSlotJson? Neck { get; set; }
    public EquipmentSlotJson? Belt { get; set; }
    public EquipmentSlotJson? Cloak { get; set; }
    public EquipmentSlotJson? Ring1 { get; set; }
    public EquipmentSlotJson? Ring2 { get; set; }
    public EquipmentSlotJson? Bracers { get; set; }
    public EquipmentSlotJson? Gloves { get; set; }
    public EquipmentSlotJson? Boots { get; set; }
    public List<EquipmentSlotJson>? QuickSlots { get; set; }
}

/// <summary>
/// Weapon set (main hand + off hand)
/// </summary>
public class WeaponSetJson
{
    public int SetNumber { get; set; }
    public EquipmentSlotJson? MainHand { get; set; }
    public EquipmentSlotJson? OffHand { get; set; }
}

/// <summary>
/// Equipment slot item
/// </summary>
public class EquipmentSlotJson
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public List<string>? Enchantments { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Spellbook information
/// </summary>
public class SpellbookJson
{
    public string? ClassName { get; set; }
    public int CasterLevel { get; set; }
    public Dictionary<int, int>? SpellSlotsPerDay { get; set; }
    public Dictionary<int, List<string>>? KnownSpells { get; set; }
    public List<int>? DomainSlotLevels { get; set; } // Spell levels that have domain/special slots
}

/// <summary>
/// Level progression entry
/// </summary>
public class LevelProgressionJson
{
    public int Level { get; set; }
    public List<string>? Features { get; set; }
}

/// <summary>
/// Settlement with buildings
/// </summary>
public class SettlementJson
{
    public string? RegionName { get; set; }
    public string? SettlementName { get; set; }
    public string? Level { get; set; }
    public bool IsClaimed { get; set; }
    public List<string>? Buildings { get; set; }
    public List<ArtisanJson>? Artisans { get; set; }
}

/// <summary>
/// Artisan information
/// </summary>
public class ArtisanJson
{
    public string? Name { get; set; }
    public int ProductionStartedOn { get; set; }
    public int ProductionEndsOn { get; set; }
    public bool BuildingUnlocked { get; set; }
    public int TiersUnlocked { get; set; }
    public string? HelpProjectEvent { get; set; }
    public List<ArtisanItemJson>? PreviousItems { get; set; }
    public List<ArtisanItemJson>? CurrentProduction { get; set; }
}

/// <summary>
/// Artisan item with type and enchantments
/// </summary>
public class ArtisanItemJson
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public List<string>? Enchantments { get; set; }
}
