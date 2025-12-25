# Pathfinder: Kingmaker Save File Parser

A maintainable C# application designed to parse and export Pathfinder: Kingmaker save files, extracting character build history and kingdom statistics.

## Features

### Character Build Analysis
- **Character Name**: Displays character name (including custom names if set)
- **Race & Class**: Shows racial heritage and all classes with archetypes and levels
- **Base Attributes**: Displays ability scores (Str, Dex, Con, Int, Wis, Cha)
- **Equipment**: Shows equipped weapons, armor, and accessories with enchantments
  - Active weapon set (main hand and off-hand)
  - All armor and accessory slots (body, head, neck, belt, cloak, rings, bracers, gloves, boots)
  - Item enchantments displayed inline
  - Distinguishes between empty slots, shields, and dual-wielding configurations
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

1. **Run the parser:**
   ```bash
   dotnet run --project PathfinderSaveParser
   ```

2. **The parser will automatically find your save files:**
   - **Scenario 1**: If `player.json` and `party.json` exist in the `SavedGame/` folder → processes them directly
   - **Scenario 2**: If `.zks` save files are in the `SavedGame/` folder → extracts and processes them
   - **Scenario 3**: If `SavedGame/` is empty → searches your default Pathfinder save location and extracts the most recent save

3. **View results:**
   - Check console output for full reports
   - Find saved reports in `Output/kingdom_stats.txt` and `Output/all_characters.txt`

### Configuration (Optional)

Edit `appsettings.json` to customize save file handling:

```json
{
  "PathfinderSaveLocation": "%USERPROFILE%\\AppData\\LocalLow\\Owlcat Games\\Pathfinder Kingmaker\\Saved Games",
  "DefaultSaveFile": "Auto_1.zks"
}
```

- **PathfinderSaveLocation**: Default location to search for save files (supports environment variables)
  - Windows: `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games`
  - Linux: `~/.config/unity3d/Owlcat Games/Pathfinder Kingmaker/Saved Games`
  - macOS: `~/Library/Application Support/unity3d/Owlcat Games/Pathfinder Kingmaker/Saved Games`
- **DefaultSaveFile**: Specific save file to use (e.g., "Auto_1.zks", "Manual_3.zks")
  - If specified and exists → uses that exact save file
  - If specified but doesn't exist → falls back to the most recently modified save
  - If empty or not specified → always uses the most recently modified save

**Manual Options:**
- Copy extracted `player.json` and `party.json` to `SavedGame/` folder, or
- Copy a `.zks` save file to `SavedGame/` folder

## Project Structure

```
PathfinderKingmakerSaveBuildParser/
├── PathfinderSaveParser.sln          # Solution file
├── PathfinderSaveParser/
│   ├── PathfinderSaveParser.csproj   # Main project file
│   ├── appsettings.json              # Configuration (default save location)
│   ├── blueprint_database.json       # Blueprint name database (45,632+ entries)
│   ├── README.txt                     # End-user instructions (copied to output)
│   ├── Program.cs                     # Main entry point
│   ├── Models/
│   │   ├── SaveFileModels.cs         # Kingdom & player save models
│   │   └── CharacterModels.cs        # Character & progression models
│   ├── Services/
│   │   ├── KingdomStatsParser.cs     # Kingdom statistics parser
│   │   ├── CharacterParser.cs        # Character build parser with $ref resolution
│   │   ├── EquipmentParser.cs        # Equipment and item parser
│   │   ├── SaveFileExtractor.cs      # .zks save file extraction service
│   │   ├── RefResolver.cs            # JSON $ref pointer resolver
│   │   ├── BlueprintLookupService.cs # Blueprint GUID to name resolver
│   │   └── SaveFileInspector.cs      # Save file metadata inspector
│   ├── SavedGame/                     # Extracted save files location (auto-created)
│   │   └── README.txt                 # Instructions for save file placement
│   └── Output/                        # Generated reports (auto-created on build)
│       ├── kingdom_stats.txt
│       └── all_characters.txt
├── BlueprintBuilder/
│   └── Program.cs                     # Utility to rebuild blueprint database
└── Blueprints2.1.4/                   # Blueprint dump folder (not in repository)
    └── Blueprints.txt                 # Game blueprint definitions
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

3. **Run the parser:**
   - The application automatically finds and extracts `.zks` save files
   - No manual extraction needed - just run the parser and it will handle everything
   - Optionally configure `DefaultSaveFile` in `appsettings.json` to use a specific save file

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
cd PathfinderSaveParser/bin/Release/net8.0

# Windows:
PathfinderSaveParser.exe

# Linux/macOS:
chmod +x PathfinderSaveParser  # First time only
./PathfinderSaveParser

# Or use .NET directly on any platform:
dotnet PathfinderSaveParser.dll
```

### Output

The application generates the following reports in the `Output/` folder and displays them in the console:

1. **kingdom_stats.txt** - Kingdom statistics with values and ranks
2. **all_characters.txt** - Complete build analysis for all characters (main + companions)

## Example Output

### Kingdom Stats
```
=== KINGDOM STATISTICS ===

Kingdom Name: Barony of the Stolen Lands
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

Character Name: Valerie

BUILD SUMMARY
================================================================================
Stats: Str 14, Dex 14, Con 19, Int 9, Wis 10, Cha 16

Race - Human Race
Class - Fighter Class 9 (Tower Shield Specialist Archetype), Rogue Class 1 (Thug Archetype)


EQUIPMENT
================================================================================

Active Weapon Set (Set 1):
  Main Hand:  Scepter Of Woe (Enhancement 3, Scepter Immunity)
  Off Hand:   Tower Shield

Armor & Accessories:
  Body      : Fullplate Standart Plus 2 (Armor Enhancement Bonus 2)
  Head      : Headband Of Charisma 2 (Charisma 2)
  Neck      : Amulet Of Natural Armor 2 (Natural Armor Enhancement 2)
  Belt      : Belt Of Dexterity Constitution 4 (Dexterity 4, Constitution 4)
  Cloak     : Cloak Wyvern Item (Cloak Of Wyvern Enchantment, Natural Armor Enhancement 2)
  Ring 1    : Ring Of Protection 2 (Deflection 2)
  Ring 2    : (empty)
  Bracers   : (empty)
  Gloves    : Crazy Scientists Gloves Item (Acid Resistance 15 Enchant, Fire Resistance 15 Enchant)
  Boots     : Boots Of Swift Foot (Speed Increase)


LEVEL-BY-LEVEL BUILD HISTORY
================================================================================
Level 0: Toughness
Level 1: Dodge, Exotic Weapon Proficiency Selection, Bastard Sword Proficiency
Level 2: Weapon Focus
Level 3: Dazzling Display Feature
Level 4: Outflank
Level 5: Power Attack Feature
Level 6: Shatter Defenses
Level 7: Cornugon Smash
Level 8: Blind Fight
Level 9: Diehard
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
- The application automatically extracts `.zks` archives - no manual extraction needed
- If automatic extraction fails:
  - Verify the `PathfinderSaveLocation` path in `appsettings.json` is correct
  - Ensure the `.zks` file exists in either the `SavedGame/` folder or default Pathfinder location
  - Check that you have read permissions for the save files and folders
- If `DefaultSaveFile` is configured but not found, the parser falls back to the most recent save
- Check the console output for detailed extraction status and error messages

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
- **Newtonsoft.Json** (v13.0.4): JSON serialization/deserialization
- **Microsoft.Extensions.Configuration** (v8.0.0): Configuration file support
- **Microsoft.Extensions.Configuration.Json** (v8.0.0): JSON configuration provider
- **System.IO.Compression**: ZIP archive extraction for .zks files
- **.NET 8.0**: Runtime framework

### Architecture
- **Models**: POCOs representing save file structure
- **Services**: 
  - `RefResolver`: Handles JSON `$id` and `$ref` pointer resolution
  - `CharacterParser`: Parses character builds using dynamic JSON traversal
  - `EquipmentParser`: Extracts equipped items, weapons, and armor with enchantments
  - `KingdomStatsParser`: Extracts kingdom statistics
  - `SaveFileExtractor`: Automatically extracts `.zks` archives and locates save files
  - `BlueprintLookupService`: Resolves GUIDs to human-readable names
  - `SaveFileInspector`: Provides file metadata
- **Program**: Entry point and orchestration with three-tier automatic save file loading

### Key Features
- **Automatic Save File Loading**: Three-tier system automatically finds and extracts save files:
  1. Checks for JSON files in SavedGame folder
  2. Extracts .zks archives from SavedGame folder
  3. Searches default Pathfinder location and extracts the most recent save (or configured default)
- **Reference Resolution**: The parser handles Pathfinder's complex `$id`/`$ref` pattern, which is essential for parsing companion data
- **Blueprint Database**: 45,632+ entries provide comprehensive name resolution
- **Name Normalization**: Automatic PascalCase to readable format conversion (e.g., "PowerAttackFeature" → "Power Attack Feature")
- **Custom Names**: Displays custom character names alongside blueprint names
- **Filtering**: Removes 34,000+ unnecessary dialog/cutscene entries from the database
- **Configurable Defaults**: Optionally specify a preferred save file in appsettings.json

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
