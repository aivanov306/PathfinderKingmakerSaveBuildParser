using PathfinderSaveParser.Models;
using System.Text;

namespace PathfinderSaveParser.Services;

public class TextFileGenerator
{
    private readonly string _outputDir;

    public TextFileGenerator(string outputDir)
    {
        _outputDir = outputDir;
    }

    public async Task GenerateAllTextFilesAsync(CurrentStateJson currentState)
    {
        await GenerateKingdomStatsAsync(currentState.Kingdom);
        await GenerateAllCharactersAsync(currentState.Characters);
        await GenerateSettlementsAsync(currentState.Settlements);
        await GenerateExploredLocationsAsync(currentState.ExploredLocations);
        await GenerateInventoryAsync(currentState.Inventory);
        await GenerateCurrentStateAsync(); // Combined file
    }

    private async Task GenerateKingdomStatsAsync(KingdomStatsJson? kingdom)
    {
        if (kingdom == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("=== KINGDOM STATISTICS ===");
        sb.AppendLine();
        sb.AppendLine($"Kingdom Name: {kingdom.Name}");
        sb.AppendLine($"Alignment: {kingdom.Alignment}");
        sb.AppendLine($"Game Time: {kingdom.GameTime}");
        sb.AppendLine($"Days: {kingdom.Days}");
        sb.AppendLine($"Gold: {kingdom.Gold:N0}");
        sb.AppendLine($"Build Points: {kingdom.BuildPoints}");
        sb.AppendLine($"Build Points Per Turn: {(kingdom.BuildPointsPerTurn.HasValue ? kingdom.BuildPointsPerTurn.Value.ToString() : "Unknown")}");
        sb.AppendLine($"Unrest Level: {kingdom.UnrestLevel}");
        sb.AppendLine();

        if (kingdom.Stats != null && kingdom.Stats.Any())
        {
            sb.AppendLine("Kingdom Stats:");
            foreach (var stat in kingdom.Stats)
            {
                sb.AppendLine($"  {stat.Type,-15} Value: {stat.Value,3}  Rank: {stat.Rank}");
            }
            sb.AppendLine();
        }

        if (kingdom.Advisors != null && kingdom.Advisors.Any())
        {
            sb.AppendLine("Advisors:");
            foreach (var advisor in kingdom.Advisors)
            {
                var assignedTo = !string.IsNullOrEmpty(advisor.Advisor)
                    ? advisor.Advisor
                    : advisor.Status == "Locked" ? "Locked" : "Unassigned";
                sb.AppendLine($"  {advisor.Position,-20} {assignedTo}");
            }
        }

        await File.WriteAllTextAsync(Path.Combine(_outputDir, "kingdom_stats.txt"), sb.ToString());
    }

    private async Task GenerateAllCharactersAsync(List<CharacterJson>? characters)
    {
        if (characters == null || !characters.Any()) return;

        var sb = new StringBuilder();
        sb.AppendLine("=== ALL CHARACTERS ===");
        sb.AppendLine();

        foreach (var character in characters)
        {
            sb.AppendLine($"=== {character.Name} ===");
            sb.AppendLine();
            sb.AppendLine($"Race: {character.Race}");
            
            if (character.Classes != null && character.Classes.Any())
            {
                sb.AppendLine("Classes:");
                foreach (var cls in character.Classes)
                {
                    var line = $"  {cls.ClassName} (Level {cls.Level})";
                    if (!string.IsNullOrEmpty(cls.Archetype))
                    {
                        line += $" - {cls.Archetype}";
                    }
                    sb.AppendLine(line);
                }
                sb.AppendLine();
            }

            if (character.Attributes != null)
            {
                sb.AppendLine("Attributes:");
                sb.AppendLine($"  Strength:     {character.Attributes.Strength}");
                sb.AppendLine($"  Dexterity:    {character.Attributes.Dexterity}");
                sb.AppendLine($"  Constitution: {character.Attributes.Constitution}");
                sb.AppendLine($"  Intelligence: {character.Attributes.Intelligence}");
                sb.AppendLine($"  Wisdom:       {character.Attributes.Wisdom}");
                sb.AppendLine($"  Charisma:     {character.Attributes.Charisma}");
                sb.AppendLine();
            }

            if (character.Skills != null)
            {
                sb.AppendLine("Skills:");
                if (character.Skills.Mobility.HasValue)
                    sb.AppendLine($"  {"Mobility",-25} {character.Skills.Mobility.Value,3}");
                if (character.Skills.Athletics.HasValue)
                    sb.AppendLine($"  {"Athletics",-25} {character.Skills.Athletics.Value,3}");
                if (character.Skills.Stealth.HasValue)
                    sb.AppendLine($"  {"Stealth",-25} {character.Skills.Stealth.Value,3}");
                if (character.Skills.Thievery.HasValue)
                    sb.AppendLine($"  {"Thievery",-25} {character.Skills.Thievery.Value,3}");
                if (character.Skills.KnowledgeArcana.HasValue)
                    sb.AppendLine($"  {"Knowledge (Arcana)",-25} {character.Skills.KnowledgeArcana.Value,3}");
                if (character.Skills.KnowledgeWorld.HasValue)
                    sb.AppendLine($"  {"Knowledge (World)",-25} {character.Skills.KnowledgeWorld.Value,3}");
                if (character.Skills.LoreNature.HasValue)
                    sb.AppendLine($"  {"Lore (Nature)",-25} {character.Skills.LoreNature.Value,3}");
                if (character.Skills.LoreReligion.HasValue)
                    sb.AppendLine($"  {"Lore (Religion)",-25} {character.Skills.LoreReligion.Value,3}");
                if (character.Skills.Perception.HasValue)
                    sb.AppendLine($"  {"Perception",-25} {character.Skills.Perception.Value,3}");
                if (character.Skills.Persuasion.HasValue)
                    sb.AppendLine($"  {"Persuasion",-25} {character.Skills.Persuasion.Value,3}");
                if (character.Skills.UseMagicDevice.HasValue)
                    sb.AppendLine($"  {"Use Magic Device",-25} {character.Skills.UseMagicDevice.Value,3}");
                sb.AppendLine();
            }

            if (character.Equipment != null)
            {
                sb.AppendLine("Equipment:");
                
                if (character.Equipment.MainHand?.Name != null)
                    sb.AppendLine($"  Main Hand:   {FormatEquipmentSlot(character.Equipment.MainHand)}");
                if (character.Equipment.OffHand?.Name != null)
                    sb.AppendLine($"  Off Hand:    {FormatEquipmentSlot(character.Equipment.OffHand)}");
                if (character.Equipment.Head?.Name != null)
                    sb.AppendLine($"  Head:        {FormatEquipmentSlot(character.Equipment.Head)}");
                if (character.Equipment.Neck?.Name != null)
                    sb.AppendLine($"  Neck:        {FormatEquipmentSlot(character.Equipment.Neck)}");
                if (character.Equipment.Body?.Name != null)
                    sb.AppendLine($"  Body:        {FormatEquipmentSlot(character.Equipment.Body)}");
                if (character.Equipment.Belt?.Name != null)
                    sb.AppendLine($"  Belt:        {FormatEquipmentSlot(character.Equipment.Belt)}");
                if (character.Equipment.Gloves?.Name != null)
                    sb.AppendLine($"  Gloves:      {FormatEquipmentSlot(character.Equipment.Gloves)}");
                if (character.Equipment.Boots?.Name != null)
                    sb.AppendLine($"  Boots:       {FormatEquipmentSlot(character.Equipment.Boots)}");
                if (character.Equipment.Ring1?.Name != null)
                    sb.AppendLine($"  Ring 1:      {FormatEquipmentSlot(character.Equipment.Ring1)}");
                if (character.Equipment.Ring2?.Name != null)
                    sb.AppendLine($"  Ring 2:      {FormatEquipmentSlot(character.Equipment.Ring2)}");
                if (character.Equipment.Cloak?.Name != null)
                    sb.AppendLine($"  Cloak:       {FormatEquipmentSlot(character.Equipment.Cloak)}");
                if (character.Equipment.Bracers?.Name != null)
                    sb.AppendLine($"  Bracers:     {FormatEquipmentSlot(character.Equipment.Bracers)}");
                
                if (character.Equipment.QuickSlots != null && character.Equipment.QuickSlots.Any())
                {
                    sb.AppendLine($"  Quick Slots:");
                    for (int i = 0; i < character.Equipment.QuickSlots.Count; i++)
                    {
                        var qs = character.Equipment.QuickSlots[i];
                        if (qs?.Name != null)
                            sb.AppendLine($"    Slot {i + 1}: {FormatEquipmentSlot(qs)}");
                    }
                }
                
                sb.AppendLine();
            }

            if (character.Spellbooks != null && character.Spellbooks.Any())
            {
                sb.AppendLine("Spellcasting:");
                foreach (var spellbook in character.Spellbooks)
                {
                    sb.AppendLine($"  {spellbook.ClassName}:");
                    sb.AppendLine($"    Caster Level: {spellbook.CasterLevel}");
                    
                    if (spellbook.KnownSpells != null && spellbook.KnownSpells.Any())
                    {
                        sb.AppendLine("    Known Spells:");
                        foreach (var level in spellbook.KnownSpells.Keys.OrderBy(k => k))
                        {
                            var spells = spellbook.KnownSpells[level];
                            if (spells != null && spells.Any())
                            {
                                sb.AppendLine($"      Level {level}: {string.Join(", ", spells)}");
                            }
                        }
                    }
                    sb.AppendLine();
                }
            }

            if (character.LevelProgression != null && character.LevelProgression.Any())
            {
                sb.AppendLine("Level Progression:");
                foreach (var level in character.LevelProgression)
                {
                    sb.AppendLine($"  Level {level.Level}:");
                    if (level.Features != null && level.Features.Any())
                    {
                        foreach (var feature in level.Features)
                        {
                            sb.AppendLine($"    • {feature}");
                        }
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine(new string('=', 80));
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(Path.Combine(_outputDir, "all_characters.txt"), sb.ToString());
    }

    private async Task GenerateSettlementsAsync(List<SettlementJson>? settlements)
    {
        if (settlements == null || !settlements.Any()) return;

        var sb = new StringBuilder();
        sb.AppendLine("=== SETTLEMENTS ===");
        sb.AppendLine();

        foreach (var settlement in settlements)
        {
            sb.AppendLine($"Settlement: {settlement.SettlementName}");
            sb.AppendLine($"  Region: {settlement.RegionName}");
            sb.AppendLine($"  Level: {settlement.Level}");
            sb.AppendLine($"  Status: {(settlement.IsClaimed ? "Claimed" : "Unclaimed")}");
            
            if (settlement.Buildings != null && settlement.Buildings.Any())
            {
                sb.AppendLine($"  Buildings ({settlement.Buildings.Count}):");
                foreach (var building in settlement.Buildings.OrderBy(b => b))
                {
                    sb.AppendLine($"    • {building}");
                }
            }

            if (settlement.IsClaimed && settlement.Artisans != null && settlement.Artisans.Any())
            {
                sb.AppendLine($"  Artisans ({settlement.Artisans.Count}):");
                foreach (var artisan in settlement.Artisans)
                {
                    sb.AppendLine($"    • {artisan.Name}");
                    sb.AppendLine($"      Building Unlocked: {(artisan.BuildingUnlocked ? "Yes" : "No")}");
                    sb.AppendLine($"      Tiers Unlocked: {artisan.TiersUnlocked}/6");
                    
                    if (!string.IsNullOrEmpty(artisan.HelpProjectEvent))
                    {
                        sb.AppendLine($"      Help Project: {artisan.HelpProjectEvent}");
                    }
                    
                    sb.AppendLine($"      Production Started On: Day {artisan.ProductionStartedOn}");
                    sb.AppendLine($"      Production Ends On: Day {artisan.ProductionEndsOn}");
                    
                    if (artisan.CurrentProduction != null && artisan.CurrentProduction.Any())
                    {
                        sb.AppendLine($"      Current Production:");
                        foreach (var item in artisan.CurrentProduction)
                        {
                            var itemLine = $"        - {item.Name}";
                            if (!string.IsNullOrEmpty(item.Type))
                                itemLine += $" [{item.Type}]";
                            if (item.Enchantments != null && item.Enchantments.Any())
                                itemLine += $" ({string.Join(", ", item.Enchantments)})";
                            sb.AppendLine(itemLine);
                        }
                    }
                    
                    if (artisan.PreviousItems != null && artisan.PreviousItems.Any())
                    {
                        sb.AppendLine($"      Previous Items:");
                        foreach (var item in artisan.PreviousItems)
                        {
                            var itemLine = $"        - {item.Name}";
                            if (!string.IsNullOrEmpty(item.Type))
                                itemLine += $" [{item.Type}]";
                            if (item.Enchantments != null && item.Enchantments.Any())
                                itemLine += $" ({string.Join(", ", item.Enchantments)})";
                            sb.AppendLine(itemLine);
                        }
                    }
                }
            }
            
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(Path.Combine(_outputDir, "settlements.txt"), sb.ToString());
    }

    private async Task GenerateExploredLocationsAsync(List<string>? locations)
    {
        if (locations == null || !locations.Any()) return;

        var sb = new StringBuilder();
        sb.AppendLine("=== EXPLORED LOCATIONS ===");
        sb.AppendLine();
        sb.AppendLine($"Total Locations Explored: {locations.Count}");
        sb.AppendLine();

        var sortedLocations = locations.OrderBy(l => l).ToList();
        foreach (var location in sortedLocations)
        {
            sb.AppendLine($"  • {location}");
        }

        await File.WriteAllTextAsync(Path.Combine(_outputDir, "explored_locations.txt"), sb.ToString());
    }

    private async Task GenerateInventoryAsync(InventoryJson? inventory)
    {
        if (inventory == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("=== INVENTORY ===");
        sb.AppendLine();

        // Personal Chest
        if (inventory.PersonalChest != null)
        {
            sb.AppendLine("=== PERSONAL CHEST ===");
            sb.AppendLine();

            AppendInventorySection(sb, "Weapons", inventory.PersonalChest.Weapons);
            AppendInventorySection(sb, "Armor", inventory.PersonalChest.Armor);
            AppendInventorySection(sb, "Accessories", inventory.PersonalChest.Accessories);
            AppendInventorySection(sb, "Usables", inventory.PersonalChest.Usables);
            AppendInventorySection(sb, "Other", inventory.PersonalChest.Other);
            
            sb.AppendLine();
        }

        // Shared Inventory
        if (inventory.SharedInventory != null)
        {
            sb.AppendLine("=== SHARED PARTY INVENTORY ===");
            sb.AppendLine();

            AppendInventorySection(sb, "Weapons", inventory.SharedInventory.Weapons);
            AppendInventorySection(sb, "Armor", inventory.SharedInventory.Armor);
            AppendInventorySection(sb, "Accessories", inventory.SharedInventory.Accessories);
            AppendInventorySection(sb, "Usables", inventory.SharedInventory.Usables);
            AppendInventorySection(sb, "Other", inventory.SharedInventory.Other);
        }

        await File.WriteAllTextAsync(Path.Combine(_outputDir, "inventory.txt"), sb.ToString());
    }

    private void AppendInventorySection(StringBuilder sb, string sectionName, List<InventoryItemJson>? items)
    {
        if (items == null || !items.Any()) return;

        sb.AppendLine($"{sectionName}:");
        foreach (var item in items)
        {
            var itemLine = $"  • {item.Name}";
            if (!string.IsNullOrEmpty(item.Type))
            {
                itemLine += $" [{item.Type}]";
            }
            if (item.Enchantments != null && item.Enchantments.Any())
            {
                itemLine += $" ({string.Join(", ", item.Enchantments)})";
            }
            if (item.Count > 1)
            {
                itemLine += $" (x{item.Count})";
            }
            sb.AppendLine(itemLine);
        }
        sb.AppendLine();
    }

    private string FormatEquipmentSlot(EquipmentSlotJson slot)
    {
        var result = slot.Name ?? "";
        
        // Add type in brackets
        if (!string.IsNullOrEmpty(slot.Type))
        {
            result += $" [{slot.Type}]";
        }
        
        // Add enchantments in parentheses
        if (slot.Enchantments != null && slot.Enchantments.Any())
        {
            result += $" ({string.Join(", ", slot.Enchantments)})";
        }
        
        return result;
    }

    private async Task GenerateCurrentStateAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("=== CURRENT GAME STATE ===");
        sb.AppendLine("================================================================================");
        sb.AppendLine();
        sb.AppendLine("This file contains the complete combined information from all other text files.");
        sb.AppendLine();
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();
        sb.AppendLine();

        // Combine all text files
        var filesToCombine = new[]
        {
            "kingdom_stats.txt",
            "all_characters.txt",
            "settlements.txt",
            "explored_locations.txt",
            "inventory.txt"
        };

        foreach (var fileName in filesToCombine)
        {
            var filePath = Path.Combine(_outputDir, fileName);
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                sb.AppendLine(content);
                sb.AppendLine();
                sb.AppendLine(new string('=', 80));
                sb.AppendLine();
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("================================================================================");
        sb.AppendLine("END OF CURRENT GAME STATE");
        sb.AppendLine("================================================================================");

        await File.WriteAllTextAsync(Path.Combine(_outputDir, "CurrentState.txt"), sb.ToString());
    }
}
