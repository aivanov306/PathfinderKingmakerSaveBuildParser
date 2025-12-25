using System.IO.Compression;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Handles extraction of .zks save files (which are ZIP archives)
/// </summary>
public class SaveFileExtractor
{
    public static bool ExtractSaveFile(string zksFilePath, string extractToFolder)
    {
        try
        {
            if (!File.Exists(zksFilePath))
            {
                Console.WriteLine($"Error: Save file not found: {zksFilePath}");
                return false;
            }

            // Create extraction folder if it doesn't exist
            if (!Directory.Exists(extractToFolder))
            {
                Directory.CreateDirectory(extractToFolder);
            }

            Console.WriteLine($"Extracting save file: {Path.GetFileName(zksFilePath)}");
            Console.WriteLine($"Destination: {extractToFolder}");

            // Extract the .zks file (it's a ZIP archive)
            ZipFile.ExtractToDirectory(zksFilePath, extractToFolder, overwriteFiles: true);

            // Verify that party.json and player.json were extracted
            var partyJson = Path.Combine(extractToFolder, "party.json");
            var playerJson = Path.Combine(extractToFolder, "player.json");

            if (File.Exists(partyJson) && File.Exists(playerJson))
            {
                Console.WriteLine("✓ Successfully extracted save files");
                return true;
            }
            else
            {
                Console.WriteLine("Warning: Extraction completed but party.json or player.json not found");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting save file: {ex.Message}");
            return false;
        }
    }

    public static string? FindLatestSaveFile(string saveGamesFolder)
    {
        try
        {
            if (!Directory.Exists(saveGamesFolder))
            {
                return null;
            }

            // Search for .zks files in the root directory and all subdirectories
            var zksFiles = Directory.GetFiles(saveGamesFolder, "*.zks", SearchOption.AllDirectories);

            if (zksFiles.Length == 0)
            {
                return null;
            }

            // Find the most recently modified .zks file
            string? latestZks = null;
            DateTime latestTime = DateTime.MinValue;

            foreach (var zks in zksFiles)
            {
                var fileInfo = new FileInfo(zks);
                if (fileInfo.LastWriteTime > latestTime)
                {
                    latestTime = fileInfo.LastWriteTime;
                    latestZks = zks;
                }
            }

            return latestZks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching for save files: {ex.Message}");
            return null;
        }
    }
}
