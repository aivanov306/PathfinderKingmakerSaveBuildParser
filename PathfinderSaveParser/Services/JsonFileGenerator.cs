using Newtonsoft.Json;
using PathfinderSaveParser.Models;

namespace PathfinderSaveParser.Services;

public class JsonFileGenerator
{
    private readonly string _outputDir;
    private readonly JsonSerializerSettings _jsonSettings;

    public JsonFileGenerator(string outputDir)
    {
        _outputDir = outputDir;
        _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    public async Task GenerateAllJsonFilesAsync(CurrentStateJson currentState)
    {
        // Generate individual JSON files
        await GenerateKingdomJsonAsync(currentState.Kingdom);
        await GenerateCharactersJsonAsync(currentState.Characters);
        await GenerateSettlementsJsonAsync(currentState.Settlements);
        await GenerateExploredLocationsJsonAsync(currentState.ExploredLocations);
        await GenerateInventoryJsonAsync(currentState.Inventory);
        
        // Generate combined CurrentState.json
        await GenerateCurrentStateJsonAsync(currentState);
    }

    private async Task GenerateKingdomJsonAsync(KingdomStatsJson? kingdom)
    {
        if (kingdom == null) return;

        var json = JsonConvert.SerializeObject(kingdom, _jsonSettings);
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "kingdom_stats.json"), json);
    }

    private async Task GenerateCharactersJsonAsync(List<CharacterJson>? characters)
    {
        if (characters == null || !characters.Any()) return;

        var json = JsonConvert.SerializeObject(characters, _jsonSettings);
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "all_characters.json"), json);
    }

    private async Task GenerateSettlementsJsonAsync(List<SettlementJson>? settlements)
    {
        if (settlements == null || !settlements.Any()) return;

        var json = JsonConvert.SerializeObject(settlements, _jsonSettings);
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "settlements.json"), json);
    }

    private async Task GenerateExploredLocationsJsonAsync(List<string>? locations)
    {
        if (locations == null || !locations.Any()) return;

        var json = JsonConvert.SerializeObject(locations, _jsonSettings);
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "explored_locations.json"), json);
    }

    private async Task GenerateInventoryJsonAsync(InventoryJson? inventory)
    {
        if (inventory == null) return;

        var json = JsonConvert.SerializeObject(inventory, _jsonSettings);
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "inventory.json"), json);
    }

    private async Task GenerateCurrentStateJsonAsync(CurrentStateJson currentState)
    {
        var json = JsonConvert.SerializeObject(currentState, _jsonSettings);
        await File.WriteAllTextAsync(Path.Combine(_outputDir, "CurrentState.json"), json);
    }
}
