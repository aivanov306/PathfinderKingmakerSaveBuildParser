# Pathfinder: Kingmaker Save File Parser

A maintainable C# application designed to parse and export Pathfinder: Kingmaker save files, extracting character build history and kingdom statistics.

## Features

### Character Build Analysis
- **Character Name**: Displays character name (including custom names if set)
- **Race & Class**: Shows racial heritage and all classes with archetypes and levels
- **Base Attributes**: Displays ability scores (Str, Dex, Con, Int, Wis, Cha)
- **Level-by-Level Build History**: Detailed progression showing features, feats, and abilities acquired at each level

### Kingdom Statistics
- **Kingdom Name & Alignment**: Basic kingdom information
- **Build Points**: Current BP and per-turn generation
- **Unrest Level**: Current unrest status
- **Kingdom Stats**: All 10 kingdom statistics with both value and rank:
  - Community, Loyalty, Military, Economy, Relations
  - Divine, Arcane, Stability, Culture, Espionage

### Blueprint Database
- Comprehensive database with 45,632+ game blueprints
- Automatic name resolution for features, classes, races, and feats
- Clean, human-readable output with proper spacing

## Quick Start

1. **Extract your save files:**
   - Go to `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games\[YourSave]`
   - Extract your `.zks` save file (e.g., `Auto_1.zks`) using 7-Zip or WinRAR
   - Copy `player.json` and `party.json` to the `SavedGame/` folder

2. **Run the parser:**
   ```bash
   cd SaveFeatParser
   dotnet run --project PathfinderSaveParser
   ```

3. **View results:**
   - Check console output for full reports
   - Find saved reports in `Output/kingdom_stats.txt` and `Output/all_characters.txt`

## Project Structure

```
PathfinderKingmakerSaveBuildParser/
├── PathfinderSaveParser.sln          # Solution file
├── PathfinderSaveParser/
│   ├── PathfinderSaveParser.csproj   # Main project file
│   ├── blueprint_database.json       # Blueprint name database (45,632+ entries)
│   ├── Program.cs                     # Main entry point
│   ├── Models/
│   │   ├── SaveFileModels.cs         # Kingdom & player save models
│   │   └── CharacterModels.cs        # Character & progression models
│   └── Services/
│       ├── KingdomStatsParser.cs     # Kingdom statistics parser
│       ├── EnhancedCharacterParser.cs # Character build parser with $ref resolution
│       ├── RefResolver.cs            # JSON $ref pointer resolver
│       ├── BlueprintLookupService.cs # Blueprint GUID to name resolver
│       └── SaveFileInspector.cs      # Save file metadata inspector
├── BlueprintBuilder/
│   └── Program.cs                     # Utility to rebuild blueprint database
├── Blueprints2.1.4/                   # Blueprint dump folder (not in repository)
│   └── Blueprints.txt                 # Game blueprint definitions
├── SavedGame/                         # Place extracted save files here (not in repository)
│   ├── player.json
│   └── party.json
└── Output/                            # Generated reports appear here (not in repository)
    ├── kingdom_stats.txt
    └── all_characters.txt
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022, Visual Studio Code, or Rider

### Installation

1. **Clone or download this project**

2. **Restore dependencies:**
   ```bash
   cd SaveFeatParser
   dotnet restore
   ```

3. **Extract your save files:**
   - Navigate to your Pathfinder: Kingmaker save folder, typically located at:
     - Windows: `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games\[YourSave]`
   - **Important**: Save files are stored as `.zks` archives (e.g., `Auto_1.zks`, `Manual_1.zks`)
   - Extract the `.zks` archive using a tool like 7-Zip, WinRAR, or any ZIP-compatible archiver
   - Copy the extracted `player.json` and `party.json` files to the `SavedGame/` folder in this project

### Usage

#### Option 1: Run with default location (SavedGame folder)
```bash
dotnet run --project PathfinderSaveParser
```

#### Option 2: Specify custom save location
```bash
dotnet run --project PathfinderSaveParser -- "C:\Path\To\Extracted\SavedGame"
```

#### Option 3: Build and run executable
```bash
dotnet build -c Release
cd PathfinderSaveParser\bin\Release\net8.0
PathfinderSaveParser.exe
```

### Output

The application generates the following reports in the `Output/` folder and displays them in the console:

1. **kingdom_stats.txt** - Kingdom statistics with values and ranks
2. **all_characters.txt** - Complete build analysis for all characters (main + companions)

## Example Output

### Kingdom Stats
```
=== KINGDOM STATISTICS ===

Kingdom Name: The Cunning Lands
Alignment: LawfulGood
Unrest Level: Worried
Build Points: 39 (Per Turn: 8)

Kingdom Stats:
------------------------------------------------------------
Stat                 Value           Rank      
------------------------------------------------------------
Community            50              3         
Loyalty              67              3         
Military             45              3         
Economy              74              3         
Relations            14              0         
Divine               13              1         
Arcane               17              0         
Stability            20              0         
Culture              9               1         
Espionage            0               0         
------------------------------------------------------------
```

### Character Build
```
=== CHARACTER BUILD ANALYSIS ===

Character Name: Melaku (Start Game Pregen Paladin Unit)

BUILD SUMMARY
================================================================================
Stats: Str 18, Dex 12, Con 14, Int 10, Wis 8, Cha 18

Race - Human Race
Class - Paladin Class 10


LEVEL-BY-LEVEL BUILD HISTORY
================================================================================
Level 0: Power Attack Feature
Level 1: Toughness, Iomedae Feature
Level 3: Intimidating Prowess, Mercy Fatigued
Level 5: Selective Channel
Level 6: Mercy Diseased
Level 7: Weapon Focus
Level 9: Dazzling Display Feature, Mercy Cursed
```

### All Characters Output
The parser extracts complete builds for:
- Your main character (with custom name if set)
- All default companions: Linzi, Tartuccio, Jaethal, Harrim, Valerie, Amiri, Regongar, Octavia, Tristian, Ekun, Jubilost, Kalikke, Kanerah, Nok-Nok
- Animal companions (if present)

**Note**: Mercenaries have not been tested and may not parse correctly.

## Customization

### Updating the Blueprint Database

The application uses a comprehensive blueprint database (`blueprint_database.json`) with 45,632+ entries extracted from the game files. This database is automatically loaded at startup.

To regenerate the blueprint database (e.g., after a game update):

1. Obtain the updated `Blueprints.txt` file from the game's blueprint dump
   - The current dump (v2.1.4) was obtained from [Pathfinder-Kingmaker-Blueprints](https://github.com/jaygumji/ArcemiPathfinderKingmakerTools) repository
   - Credit to the repository author for maintaining these blueprint dumps
2. Create a folder starting with `Blueprints` (e.g., `Blueprints2.1.4`, `Blueprints2.1.5`) in the solution root directory
3. Place the `Blueprints.txt` file inside that folder
4. Run the BlueprintBuilder utility from the solution root:
   ```bash
   dotnet run --project BlueprintBuilder
   ```
5. The utility will:
   - Automatically find any folder starting with `Blueprints*`
   - Generate `blueprint_database.json` in the `PathfinderSaveParser` folder (if it exists)
   - Otherwise save to the solution root directory
6. The file will automatically be copied to the output directory during build

The BlueprintBuilder filters out unnecessary entries like:
- Dialog/cutscene system entries
- AI actions and conditions
- Internal debug entries
- Temporary and deprecated entries

### Extending the Parser

The application is designed to be maintainable and extensible:

1. **Add new models**: Create new classes in the `Models/` folder
2. **Add new parsers**: Create new services in the `Services/` folder
3. **Modify output format**: Edit the parser services to change report formatting
4. **Handle new JSON structures**: The `RefResolver` service handles `$id` and `$ref` patterns automatically

## Troubleshooting

### Save Files Not Loading
- Ensure you've **extracted** the `.zks` archive first (save files are compressed)
- Verify `player.json` and `party.json` are in the `SavedGame/` folder
- Check that the files are valid JSON (not corrupted)
- Verify you have read permissions for the files

### Missing Character Data
- The parser uses reference resolution to handle `$id` and `$ref` patterns
- All companions should be found automatically, including those not in the active party
- Check the console output for parsing errors
- Some data may only be available at certain game stages

### Unknown Blueprints
- The database includes 45,632+ blueprints from game version 2.1.4
- If you see `Blueprint_[GUID]` in output, the blueprint may be from a different game version
- Regenerate the blueprint database using the BlueprintBuilder utility
- Unknown blueprints are displayed with their GUID for reference

## Technical Details

### Dependencies
- **Newtonsoft.Json** (v13.0.3): JSON serialization/deserialization
- **.NET 8.0**: Runtime framework

### Architecture
- **Models**: POCOs representing save file structure
- **Services**: 
  - `RefResolver`: Handles JSON `$id` and `$ref` pointer resolution
  - `EnhancedCharacterParser`: Parses character builds using dynamic JSON traversal
  - `KingdomStatsParser`: Extracts kingdom statistics
  - `BlueprintLookupService`: Resolves GUIDs to human-readable names
  - `SaveFileInspector`: Provides file metadata
- **Program**: Entry point and orchestration

### Key Features
- **Reference Resolution**: The parser handles Pathfinder's complex `$id`/`$ref` pattern, which is essential for parsing companion data
- **Blueprint Database**: 45,632+ entries provide comprehensive name resolution
- **Name Normalization**: Automatic PascalCase to readable format conversion (e.g., "PowerAttackFeature" → "Power Attack Feature")
- **Custom Names**: Displays custom character names alongside blueprint names
- **Filtering**: Removes 34,000+ unnecessary dialog/cutscene entries from the database

### Save File Structure
Pathfinder: Kingmaker uses a unique save format:
- Save files are compressed as `.zks` archives (ZIP format)
- Contains `player.json` (kingdom data, ~1MB) and `party.json` (character data, ~11MB)
- Uses JSON `$id` and `$ref` for memory-efficient object referencing
- Companion data is always stored as references, requiring special handling

## Contributing

To extend this parser:

1. Study your save file structure (extract `.zks` and examine JSON)
2. Add corresponding models to `Models/`
3. Create parsers in `Services/`
4. Update `Program.cs` to call new parsers
5. Use `BlueprintBuilder` to regenerate the database if needed

### Blueprint Database Generation
The `BlueprintBuilder` utility processes the official game blueprint dump:
- Input: Any folder starting with `Blueprints*` in solution root (e.g., `Blueprints2.1.4/Blueprints.txt`)
- Output: `blueprint_database.json` (45,632 filtered entries from 81,296 total)
- Location: Automatically saves to `PathfinderSaveParser` folder if it exists, otherwise to solution root
- Filtering: Removes dialog, cutscene, AI, and other non-gameplay entries
- Normalization: Converts names to human-readable format with proper spacing

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

This is an unofficial tool for personal use. Pathfinder: Kingmaker is owned by Owlcat Games and Paizo Inc.

## Acknowledgments

- Owlcat Games for Pathfinder: Kingmaker
- The Pathfinder community for game knowledge and mechanics
- [Pathfinder-Kingmaker-Blueprints](https://github.com/jaygumji/ArcemiPathfinderKingmakerTools) repository for providing comprehensive blueprint dumps

---

**Note**: This parser is designed for Pathfinder: Kingmaker save files. The structure may differ for Pathfinder: Wrath of the Righteous or other games.
