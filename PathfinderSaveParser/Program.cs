using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PathfinderSaveParser.Models;
using PathfinderSaveParser.Services;
using System.Text;

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

            if (!Directory.Exists(saveGameDir))
            {
                Console.WriteLine($"Error: Directory not found: {saveGameDir}");
                Console.WriteLine();
                Console.WriteLine("Usage: PathfinderSaveParser [path_to_SavedGame_folder]");
                Console.WriteLine("If no path is provided, will look for 'SavedGame' folder in current directory.");
                return;
            }

            var playerJsonPath = Path.Combine(saveGameDir, "player.json");
            var partyJsonPath = Path.Combine(saveGameDir, "party.json");

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
            var kingdomParser = new KingdomStatsParser();
            
            // Create RefResolver for party.json
            Console.WriteLine("Indexing party.json references...");
            var partyResolver = new RefResolver(partyJson);
            var enhancedParser = new EnhancedCharacterParser(blueprintLookup, partyResolver);
            Console.WriteLine("Reference indexing complete.");
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();

            // Create output directory
            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Output");
            Directory.CreateDirectory(outputDir);

            // Parse Kingdom Stats
            if (playerSave.Kingdom != null)
            {
                var kingdomReport = kingdomParser.ParseKingdomStats(playerSave.Kingdom);
                var kingdomOutputPath = Path.Combine(outputDir, "kingdom_stats.txt");
                await File.WriteAllTextAsync(kingdomOutputPath, kingdomReport);
                Console.WriteLine(kingdomReport);
                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
            }
            else
            {
                Console.WriteLine("No kingdom data found in save file.");
            }

            Console.WriteLine();

            // Parse All Characters (main + companions)
            var characterReports = enhancedParser.ParseAllCharacters(partyJson);
            
            if (characterReports.Any())
            {
                // Save all characters to a single file
                var allCharactersReport = new StringBuilder();
                allCharactersReport.AppendLine("=== ALL CHARACTERS ===");
                allCharactersReport.AppendLine();
                
                foreach (var report in characterReports)
                {
                    allCharactersReport.AppendLine(report);
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
                    Console.WriteLine(report);
                    Console.WriteLine();
                    Console.WriteLine(new string('=', 80));
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No characters found in party data.");
            }

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
