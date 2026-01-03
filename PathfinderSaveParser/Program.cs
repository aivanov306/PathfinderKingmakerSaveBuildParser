using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PathfinderSaveParser.Models;
using PathfinderSaveParser.Services;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PathfinderSaveParser;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== Pathfinder: Kingmaker Save File Parser ===");
        Console.WriteLine();

        try
        {
            // Load configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            // Load report options from config
            var reportOptions = new ReportOptions();
            config.GetSection("ReportOptions").Bind(reportOptions);
            
            // Load character display order from config
            var characterDisplayOrder = config.GetSection("CharacterDisplayOrder").Get<List<string>>() ?? new List<string>();
            var excludeCharacters = config.GetSection("ExcludeCharacters").Get<List<string>>() ?? new List<string>();
            
            Console.WriteLine($"Report Options Loaded:");
            Console.WriteLine($"  Include Stats: {reportOptions.IncludeStats}");
            Console.WriteLine($"  Include Equipment: {reportOptions.IncludeEquipment}");
            Console.WriteLine($"  Show Feat Parameters: {reportOptions.ShowFeatParameters}");
            Console.WriteLine();

            // Determine save game directory
            string saveGameDir;
            if (args.Length > 0)
            {
                saveGameDir = args[0];
            }
            else
            {
                saveGameDir = Path.Combine(Directory.GetCurrentDirectory(), "SavedGame");
            }

            // Ensure SavedGame directory exists
            if (!Directory.Exists(saveGameDir))
            {
                Directory.CreateDirectory(saveGameDir);
                Console.WriteLine($"Created directory: {saveGameDir}");
            }

            var playerJsonPath = Path.Combine(saveGameDir, "player.json");
            var partyJsonPath = Path.Combine(saveGameDir, "party.json");

            // Check what we have in the directory
            bool hasJsonFiles = File.Exists(playerJsonPath) && File.Exists(partyJsonPath);
            bool hasZksFile = Directory.GetFiles(saveGameDir, "*.zks").Any();

            if (!hasJsonFiles)
            {
                Console.WriteLine("JSON save files not found (party.json or player.json). Checking for .zks archives...");
                Console.WriteLine();

                string? zksFileToExtract = null;

                if (hasZksFile)
                {
                    // Use the .zks file in the SavedGame folder
                    zksFileToExtract = Directory.GetFiles(saveGameDir, "*.zks").FirstOrDefault();
                    Console.WriteLine($"Found save archive: {Path.GetFileName(zksFileToExtract)}");
                }
                else
                {
                    // Try to find save from default Pathfinder location
                    var defaultSavePath = config["PathfinderSaveLocation"];
                    if (!string.IsNullOrEmpty(defaultSavePath))
                    {
                        defaultSavePath = Environment.ExpandEnvironmentVariables(defaultSavePath);
                        Console.WriteLine($"Searching for saves in: {defaultSavePath}");

                        // Check if a specific default save file is configured
                        var defaultSaveFile = config["DefaultSaveFile"];
                        if (!string.IsNullOrEmpty(defaultSaveFile))
                        {
                            var specificSavePath = Path.Combine(defaultSavePath, defaultSaveFile);
                            if (File.Exists(specificSavePath))
                            {
                                zksFileToExtract = specificSavePath;
                                Console.WriteLine($"Found configured save: {Path.GetFileName(zksFileToExtract)}");
                                Console.WriteLine($"Modified: {File.GetLastWriteTime(zksFileToExtract)}");
                            }
                            else
                            {
                                Console.WriteLine($"Configured save '{defaultSaveFile}' not found, searching for latest save...");
                                zksFileToExtract = SaveFileExtractor.FindLatestSaveFile(defaultSavePath);
                                
                                if (zksFileToExtract != null)
                                {
                                    Console.WriteLine($"Found latest save: {Path.GetFileName(zksFileToExtract)}");
                                    Console.WriteLine($"Modified: {File.GetLastWriteTime(zksFileToExtract)}");
                                }
                            }
                        }
                        else
                        {
                            // No specific file configured, find latest
                            zksFileToExtract = SaveFileExtractor.FindLatestSaveFile(defaultSavePath);

                            if (zksFileToExtract != null)
                            {
                                Console.WriteLine($"Found latest save: {Path.GetFileName(zksFileToExtract)}");
                                Console.WriteLine($"Modified: {File.GetLastWriteTime(zksFileToExtract)}");
                            }
                        }

                        if (zksFileToExtract == null)
                        {
                            Console.WriteLine("No save files found in default location.");
                        }
                    }
                }

                if (zksFileToExtract != null)
                {
                    Console.WriteLine();
                    if (!SaveFileExtractor.ExtractSaveFile(zksFileToExtract, saveGameDir))
                    {
                        Console.WriteLine("Failed to extract save file.");
                        return;
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("No save files found to process.");
                    Console.WriteLine();
                    Console.WriteLine("Options:");
                    Console.WriteLine("  1. Copy player.json and party.json to the SavedGame folder");
                    Console.WriteLine("  2. Copy a .zks save file to the SavedGame folder");
                    Console.WriteLine($"  3. Configure PathfinderSaveLocation in appsettings.json");
                    Console.WriteLine();
                    Console.WriteLine("Usage: PathfinderSaveParser [path_to_save_folder_or_zks_file]");
                    return;
                }
            }

            // Verify JSON files now exist
            if (!File.Exists(playerJsonPath))
            {
                Console.WriteLine($"Error: player.json not found at {playerJsonPath}");
                return;
            }

            if (!File.Exists(partyJsonPath))
            {
                Console.WriteLine($"Error: party.json not found at {partyJsonPath}");
                return;
            }

            Console.WriteLine($"Loading save files from: {saveGameDir}");
            Console.WriteLine();

            // Inspect files first
            var inspector = new SaveFileInspector();
            
            Console.WriteLine("File Information:");
            var playerStats = inspector.GetFileStats(playerJsonPath);
            foreach (var stat in playerStats)
            {
                Console.WriteLine($"  player.json - {stat.Key}: {stat.Value}");
            }
            
            var partyStats = inspector.GetFileStats(partyJsonPath);
            foreach (var stat in partyStats)
            {
                Console.WriteLine($"  party.json - {stat.Key}: {stat.Value}");
            }
            Console.WriteLine();

            // Load and parse files as JObject for reference resolution
            var playerJsonText = await File.ReadAllTextAsync(playerJsonPath);
            var partyJsonText = await File.ReadAllTextAsync(partyJsonPath);

            var playerJson = JObject.Parse(playerJsonText);
            var partyJson = JObject.Parse(partyJsonText);

            // Also deserialize for kingdom stats
            var playerSave = JsonConvert.DeserializeObject<PlayerSaveFile>(playerJsonText);
            if (playerSave == null)
            {
                Console.WriteLine("Error: Failed to parse player.json.");
                return;
            }

            // Initialize services
            var blueprintLookup = new BlueprintLookupService();
            var kingdomParser = new KingdomStatsParser(blueprintLookup, reportOptions);
            var inventoryParser = new InventoryParser(blueprintLookup);
            
            // Create RefResolver for party.json
            Console.WriteLine("Indexing party.json references...");
            var partyResolver = new RefResolver(partyJson);
            var enhancedParser = new EnhancedCharacterParser(blueprintLookup, partyResolver, reportOptions);
            var jsonBuilder = new JsonOutputBuilder(blueprintLookup, partyResolver, reportOptions);
            Console.WriteLine("Reference indexing complete.");
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();

            // Create output directory
            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Output");
            Directory.CreateDirectory(outputDir);

            // Parse Kingdom Stats
            if (reportOptions.IncludeKingdomStats && playerSave.Kingdom != null)
            {
                var kingdomReport = kingdomParser.ParseKingdomStats(playerSave.Kingdom, playerSave.Money);
                var kingdomOutputPath = Path.Combine(outputDir, "kingdom_stats.txt");
                await File.WriteAllTextAsync(kingdomOutputPath, kingdomReport);
                Console.WriteLine(kingdomReport);
                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
            }
            else if (!reportOptions.IncludeKingdomStats)
            {
                Console.WriteLine("Kingdom stats disabled in configuration.");
            }
            else
            {
                Console.WriteLine("No kingdom data found in save file.");
            }

            Console.WriteLine();

            // Parse Inventory (Personal Chest + Shared Party Inventory)
            if (reportOptions.IncludeInventory)
            {
                var inventoryReport = inventoryParser.ParseBothInventories(playerSave.SharedStash, partyJson);
                var inventoryOutputPath = Path.Combine(outputDir, "inventory.txt");
                await File.WriteAllTextAsync(inventoryOutputPath, inventoryReport);
                Console.WriteLine(inventoryReport);
                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
            }

            Console.WriteLine();

            // Parse All Characters (main + companions)
            var characterReports = enhancedParser.ParseAllCharacters(partyJson);
            
            // Filter out excluded characters
            if (excludeCharacters.Any())
            {
                characterReports = characterReports
                    .Where(cr => !excludeCharacters.Any(pattern => 
                        cr.CharacterName.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            
            // Sort characters based on CharacterDisplayOrder
            if (characterDisplayOrder.Any())
            {
                characterReports = characterReports
                    .OrderBy(cr => 
                    {
                        var orderIndex = characterDisplayOrder.FindIndex(pattern => 
                            cr.CharacterName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
                        return orderIndex == -1 ? int.MaxValue : orderIndex;
                    })
                    .ThenBy(cr => cr.CharacterName)
                    .ToList();
            }
            
            if (characterReports.Any())
            {
                // Save all characters to a single file
                var allCharactersReport = new StringBuilder();
                allCharactersReport.AppendLine("=== ALL CHARACTERS ===");
                allCharactersReport.AppendLine();
                
                foreach (var report in characterReports)
                {
                    allCharactersReport.AppendLine(report.Report);
                    allCharactersReport.AppendLine();
                    allCharactersReport.AppendLine(new string('=', 80));
                    allCharactersReport.AppendLine();
                }
                
                var allCharactersPath = Path.Combine(outputDir, "all_characters.txt");
                await File.WriteAllTextAsync(allCharactersPath, allCharactersReport.ToString());
                
                // Display all characters in console
                Console.WriteLine("=== ALL CHARACTERS ===");
                Console.WriteLine();
                
                foreach (var report in characterReports)
                {
                    Console.WriteLine(report.Report);
                    Console.WriteLine();
                    Console.WriteLine(new string('=', 80));
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No characters found in party data.");
            }

            // Build all characters as JSON
            var allCharactersJson = jsonBuilder.BuildCharactersJson(partyJson);
            
            // Apply the same filtering and ordering as text reports
            if (excludeCharacters.Any())
            {
                allCharactersJson = allCharactersJson
                    .Where(c => !excludeCharacters.Any(pattern => 
                        (c.Name ?? "").Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            
            if (characterDisplayOrder.Any())
            {
                allCharactersJson = allCharactersJson
                    .OrderBy(c => 
                    {
                        var orderIndex = characterDisplayOrder.FindIndex(pattern => 
                            (c.Name ?? "").Contains(pattern, StringComparison.OrdinalIgnoreCase));
                        return orderIndex == -1 ? int.MaxValue : orderIndex;
                    })
                    .ThenBy(c => c.Name)
                    .ToList();
            }
            
            // Build current state data model
            var currentState = new CurrentStateJson
            {
                Kingdom = (reportOptions.IncludeKingdomStats && playerSave.Kingdom != null) 
                    ? jsonBuilder.BuildKingdomJson(playerSave.Kingdom, playerSave.Money, playerSave.GameTime) 
                    : null,
                Inventory = reportOptions.IncludeInventory 
                    ? jsonBuilder.BuildInventoryJson(playerSave.SharedStash, partyJson) 
                    : null,
                Characters = allCharactersJson,
                ExploredLocations = jsonBuilder.BuildExploredLocationsJson(playerSave.GlobalMap),
                Settlements = jsonBuilder.BuildSettlementsJson(playerSave.Kingdom)
            };

            // Generate all JSON files using JsonFileGenerator service
            Console.WriteLine();
            Console.WriteLine("Generating JSON output files...");
            var jsonFileGenerator = new JsonFileGenerator(outputDir);
            await jsonFileGenerator.GenerateAllJsonFilesAsync(currentState);
            Console.WriteLine("JSON files saved successfully.");

            // Generate all text files using TextFileGenerator service
            Console.WriteLine();
            Console.WriteLine("Generating text output files...");
            var textFileGenerator = new TextFileGenerator(outputDir);
            await textFileGenerator.GenerateAllTextFilesAsync(currentState);
            Console.WriteLine("Text files saved successfully.");

            Console.WriteLine();
            Console.WriteLine("=== Parsing Complete! ===");
            Console.WriteLine($"All reports saved to: {outputDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
