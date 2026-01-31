using PathfinderSaveParser.Models;
using System.Text;

namespace PathfinderSaveParser.Services;

public class TextFileGenerator
{
    private readonly string _outputDir;
    private readonly ReportOptions _options;
    private readonly EnhancedCharacterParser _characterFormatter;

    public TextFileGenerator(string outputDir, ReportOptions options, EnhancedCharacterParser characterFormatter)
    {
        _outputDir = outputDir;
        _options = options;
        _characterFormatter = characterFormatter;
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
        sb.AppendLine($"Kingdom Days: {kingdom.KingdomDays}");
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
            // Delegate formatting to CharacterParser - single source of formatting logic
            sb.AppendLine(_characterFormatter.FormatCharacter(character));
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
            
            // Add description if enabled and available
            if (_options.ShowItemDescriptions && !string.IsNullOrEmpty(item.Description))
            {
                sb.AppendLine($"    {item.Description}");
                sb.AppendLine();
            }
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
        
        // Add description on new line if enabled
        if (_options.ShowItemDescriptions && !string.IsNullOrEmpty(slot.Description))
        {
            result += $"\n      {slot.Description}";
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
