namespace PathfinderSaveParser.Models;

/// <summary>
/// Configuration options for controlling report output
/// </summary>
public class ReportOptions
{
    // Character Information Options
    public bool IncludeStats { get; set; } = true;
    public bool IncludeSkills { get; set; } = true;
    public bool IncludeRace { get; set; } = true;
    public bool IncludeClass { get; set; } = true;
    public bool IncludeEquipment { get; set; } = true;
    public bool IncludeSpellcasting { get; set; } = true;
    public bool IncludeLevelHistory { get; set; } = true;
    
    // Kingdom Options
    public bool IncludeKingdomStats { get; set; } = true;
    public bool IncludeKingdomAdvisors { get; set; } = true;
    public bool IncludeInventory { get; set; } = true;
    
    // Equipment Detail Options
    public bool IncludeActiveWeaponSet { get; set; } = true;
    public bool IncludeAllWeaponSets { get; set; } = false;
    public bool IncludeArmor { get; set; } = true;
    public bool IncludeAccessories { get; set; } = true;
    public bool ShowEmptySlots { get; set; } = true;
    public bool ShowEnchantments { get; set; } = true;
    
    // Feature/Feat Options
    public bool ShowFeatParameters { get; set; } = true;
}

/// <summary>
/// Contains character name and report for ordering
/// </summary>
public class CharacterReport
{
    public string CharacterName { get; set; } = string.Empty;
    public string Report { get; set; } = string.Empty;
}
