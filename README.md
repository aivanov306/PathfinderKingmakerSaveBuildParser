# Pathfinder: Kingmaker Save File Parser

A maintainable C# application designed to parse and export Pathfinder: Kingmaker save files, extracting character build history and kingdom statistics.

## Features

### Character Build Analysis
- **Character Name**: Displays character name (including custom names if set)
- **Race & Class**: Shows racial heritage and all classes with archetypes and levels
- **Base Attributes**: Displays ability scores (Str, Dex, Con, Int, Wis, Cha)
- **Skills**: Shows all character skills with calculated bonuses:
  - Mobility, Athletics, Stealth, Thievery
  - Knowledge (Arcana), Knowledge (World)
  - Lore (Nature), Lore (Religion)
  - Perception, Persuasion, Use Magic Device
- **Equipment**: Shows equipped weapons, armor, and accessories with enchantments
  - All 4 weapon sets displayed (main hand and off-hand for each set)
  - Active weapon set clearly marked with "(Active)" indicator
  - All armor and accessory slots (body, head, neck, belt, cloak, rings, bracers, gloves, boots)
  - Quick slots (potions, scrolls, wands, rods) with slot numbers
  - Equipment types displayed in brackets (e.g., [Greatsword], [Fullplate], [Tower Shield])
  - Item enchantments displayed inline
  - Distinguishes between empty slots, shields, and dual-wielding configurations
- **Spellcasting**: Shows spellcasting information for all spellcaster classes
  - Spell slots per day for each spell level
  - Complete list of known/available spells organized by level
  - Supports both spontaneous casters (Bard, Sorcerer) and prepared casters (Wizard, Cleric, etc.)
  - Displays caster level for each spellbook
- **Level-by-Level Build History**: Detailed progression showing features, feats, and abilities acquired at each level

### Kingdom Statistics
- **Kingdom Name & Alignment**: Basic kingdom information
- **Game Time & Days**: In-game time elapsed (format: "days.hours:minutes:seconds") and calculated day count
- **Gold**: Current party gold amount
- **Build Points**: Current BP and per-turn generation

### Inventory
- **Personal Chest (SharedStash)**: Items stored in player's personal stash
- **Shared Party Inventory**: Items in the party's shared inventory (m_InventorySlotIndex >= 0)
- Both inventories saved to separate file with clear sections
  - Items categorized by type: Weapons, Armor & Shields, Accessories, Usables, Other
  - Equipment types shown in brackets for weapons and armor
  - Accessories include belts, amulets, rings, bracers, headbands, cloaks, helmets, gloves, boots
  - Usables include potions, scrolls, wands, flasks, alchemist's fire
  - Item counts displayed for stackable items (x2, x5, etc.)
  - Total item count and unique item count summary per inventory
  - Sorted alphabetically within each category
  - Excludes equipped items (only shows unequipped inventory)
- **Unrest Level**: Current unrest status
- **Kingdom Stats**: All 10 kingdom statistics with both value and rank:
  - Community, Loyalty, Military, Economy, Relations
  - Divine, Arcane, Stability, Culture, Espionage
- **Kingdom Advisors**: Shows all 10 advisor positions with current assignments:
  - Displays advisor names for filled positions
  - Shows (Vacant) for unlocked positions without advisors
  - Shows (Locked) for positions that haven't met stat requirements
  - Special positions require Rank 3 and 60+ value in specific stats:
    - Grand Diplomat (Community), Warden (Military), Magister (Arcane)
    - Curator (Loyalty), Minister (Relations)

### Settlements & Artisans
- **Settlement Information**: All kingdom settlements with detailed status
  - Settlement name, region, and development level (Village, Town, City)
  - Claimed/Unclaimed status
  - Complete list of finished buildings
  - Building counts per settlement
- **Artisan Details** (for claimed settlements only):
  - Artisan name and type
  - Building unlock status (Yes/No)
  - Tiers unlocked out of 6 maximum
  - Help project events (if active)
  - Production timeline (start day and end day)
  - **Current Production**: Items currently being crafted
    - Item name with equipment type in brackets [Type]
    - Enchantments (when available)
  - **Previous Items**: Complete history of crafted items
    - Item name with equipment type in brackets [Type]
    - Enchantments (when available)
  - Items sorted and categorized for easy tracking

### JSON Export
- **CurrentState.json**: Combined file with complete game state

We can find information:
- Kingdom statistics and advisors
- Personal chest and shared inventory
- All characters with full builds
- Explored map locations
- Kingdom settlements with development levels

Detailed structure:
  - Kingdom: Name, alignment, game time, days, gold, build points, unrest, stats (with ranks), advisor assignments
  - Inventory: Personal chest and shared inventory, categorized by type (weapons, armor, accessories, usables, other)
  - Characters: Full character data including name, race, classes, attributes, equipment, spellbooks, level progression
  - ExploredLocations: List of all explored map locations
  - Settlements: All settlements with level, name, claimed status, finished buildings, and artisan details:
    - Artisan information includes building unlock status, tiers unlocked (count/6), help project events
    - Current production and previous items with full details (name, type, enchantments)
    - Production timeline (start day and end day)
- **Separate JSON files**: Individual files for kingdom, inventory, characters, explored locations, and settlements
- **Structured format**: Properly formatted JSON with indentation for readability
- **Type information**: Equipment includes type data (weapon types, armor types)
- **Categorization**: Items grouped by category with totals and counts

### Blueprint Database
- Comprehensive database with 45,632+ game blueprints
- Automatic name resolution for features, classes, races, and feats
- Equipment type information for 2,400+ weapons, armor, and shields
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

Edit `appsettings.json` to customize save file handling and report output:

#### File Location Settings

```json
{
  "PathfinderSaveLocation": "%USERPROFILE%\\AppData\\LocalLow\\Owlcat Games\\Pathfinder Kingmaker\\Saved Games",
  "DefaultSaveFile": "Auto_1.zks",
  "BPPerTurn": null
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
- **BPPerTurn**: Build Points per turn override (optional)
  - The save file only contains base BP income (excludes building bonuses and trade agreements)
  - Set to `null` (default) → displays "Unknown" in reports
  - Set to a number (e.g., `15`) → displays that value instead
  - Use this to manually specify your actual BP per turn including all modifiers

#### Report Options

Control what information is included in generated reports:

```json
"ReportOptions": {
  "IncludeStats": true,              // Character ability scores (Str, Dex, etc.)
  "IncludeSkills": true,             // Character skills (Persuasion, Stealth, etc.)
  "IncludeRace": true,               // Character race
  "IncludeClass": true,              // Classes, levels, and archetypes
  "IncludeEquipment": true,          // All equipment sections
  "IncludeLevelHistory": true,       // Level-by-level feat progression
  "IncludeKingdomStats": true,       // Kingdom statistics report
  "IncludeKingdomAdvisors": true,    // Kingdom advisor assignments
  "IncludeInventory": true,          // Personal chest inventory
  "IncludeUnclaimedSettlements": false, // Include unclaimed regions in settlements export
  "IncludeActiveWeaponSet": true,    // Currently equipped weapons
  "IncludeArmor": true,              // Equipped armor
  "IncludeAccessories": true,        // Rings, belts, cloaks, etc.
  "ShowEmptySlots": true,            // Display empty equipment slots
  "ShowEnchantments": true,          // Show item enchantments in parentheses
  "ShowFeatParameters": true,        // Show weapon types/schools in feat names
  "IncludeSpellcasting": true        // Show spell slots and available/known spells
}
```

**Examples:**
- Minimal output (classes only): Set `IncludeStats`, `IncludeEquipment`, `IncludeSpellcasting`, `IncludeLevelHistory` to `false`
- Equipment focus: Set `IncludeStats`, `IncludeRace`, `IncludeClass`, `IncludeLevelHistory` to `false`
- Clean view: Set `ShowEmptySlots` and `ShowEnchantments` to `false`

#### Character Display Options

Control which characters appear and in what order:

```json
"CharacterDisplayOrder": [
  "Melaku",
  "Regongar",
  "Harrim",
  "Tristian",
  "Jubilost",
  "Amiri",
  "Smilodon"
],
"ExcludeCharacters": [
  "Tartuccio"
]
```

- **CharacterDisplayOrder**: Array of character name patterns (case-insensitive, partial match)
  - Characters matching these patterns appear first in the specified order
  - Remaining characters appear after, sorted alphabetically
  - Example: "Smilodon" matches "Animal Companion Unit Smilodon"
- **ExcludeCharacters**: Array of character name patterns to exclude from reports
  - Characters matching these patterns will not appear in the output
  - Uses case-insensitive partial matching
  - Example: "Tartuccio" excludes any character with that name

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
│   │   ├── SpellbookParser.cs        # Spellcasting information parser
│   │   ├── SaveFileExtractor.cs      # .zks save file extraction service
│   │   ├── RefResolver.cs            # JSON $ref pointer resolver
│   │   ├── BlueprintLookupService.cs # Blueprint GUID to name resolver
│   │   ├── SaveFileInspector.cs      # Save file metadata inspector
│   │   ├── TextFileGenerator.cs      # Text report file generation service
│   │   └── JsonFileGenerator.cs      # JSON output file generation service
│   ├── SavedGame/                     # Extracted save files location (auto-created)
│   │   └── README.txt                 # Instructions for save file placement
│   └── Output/                        # Generated reports (auto-created on build)
│       ├── kingdom_stats.txt          # Kingdom statistics (text)
│       ├── inventory.txt              # Inventory lists (text)
│       ├── all_characters.txt         # Character builds (text)
│       ├── explored_locations.txt     # Explored map locations (text)
│       ├── settlements.txt            # Kingdom settlements and buildings (text)
│       ├── CurrentState.txt           # Combined text file with all data
├── PathfinderSaveParser.Tests/
│   ├── PathfinderSaveParser.Tests.csproj  # Test project file
│   ├── README.md                      # Test documentation
│   ├── Models/
│   │   └── JsonOutputModelsTests.cs   # Model structure tests
│   ├── Services/
│   │   ├── JsonOutputBuilderEnchantmentTests.cs  # Enchantment parsing tests
│   │   ├── TextFileGeneratorTests.cs  # Text output formatting tests
│   │   └── BlueprintLookupTests.cs    # Blueprint service tests
│   └── Integration/
│       └── CompleteWorkflowTests.cs   # End-to-end workflow tests
│       ├── CurrentState.json          # Combined game state (JSON)
│       ├── kingdom_stats.json         # Kingdom data (JSON)
│       ├── inventory.json             # Inventory data (JSON)
│       ├── all_characters.json        # Character data (JSON)
│       ├── explored_locations.json    # Explored map locations (JSON)
│       └── settlements.json           # Kingdom settlements and buildings (JSON)
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

The application generates the following reports in the `Output/` folder and displays text output in the console:

#### Text Files (Human-readable reports)
1. **kingdom_stats.txt** - Kingdom statistics with values and ranks
2. **inventory.txt** - Personal chest and shared party inventory
3. **all_characters.txt** - Complete build analysis for all characters (main + companions)
4. **explored_locations.txt** - All explored map locations
5. **settlements.txt** - Kingdom settlements and buildings by region
6. **CurrentState.txt** - Combined file containing all text data from the above files

#### JSON Files (Structured data for programmatic access)
1. **CurrentState.json** - Combined file containing all game state data:
   - Kingdom statistics and advisors
   - Personal chest and shared inventory
   - All character builds with attributes, classes, equipment, spells, and progression
   - Explored map locations
   - Kingdom settlements with buildings
2. **kingdom_stats.json** - Kingdom data in JSON format
3. **inventory.json** - Both inventories in JSON format with categorization
4. **all_characters.json** - All character builds in JSON format
5. **explored_locations.json** - All explored map locations in JSON format
6. **settlements.json** - Kingdom settlements and buildings in JSON format

The JSON files provide structured data suitable for external tools, data analysis, or integration with other applications.

## Example Output

### Kingdom Stats
```
=== KINGDOM STATISTICS ===

Kingdom Name: Barony of the Stolen Lands
Alignment: LawfulGood
Gold: 55 468
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

Kingdom Advisors:
------------------------------------------------------------
Position                  Advisor
------------------------------------------------------------
Regent                    Valerie
Counsilor                 Jhod Kavken
General                   Regongar
Treasurer                 Jubilost
Grand Diplomat            (Locked)
High Priest               Harrim
Magister                  (Locked)
Warden                    (Locked)
Curator                   Storyteller
Minister                  (Locked)
------------------------------------------------------------
```

### Character Build
```
=== CHARACTER BUILD ANALYSIS ===

Character Name: Valerie

BUILD SUMMARY
================================================================================
Stats: Str 14, Dex 14, Con 19, Int 9, Wis 10, Cha 16

Skills:
  Mobility: +8
  Persuasion: +16
  Perception: +4
  Knowledge (Arcana): -1
  Stealth: +2
  Thievery: +2
  Use Magic Device: +13

Race - Human Race
Class - Fighter Class 9 (Tower Shield Specialist Archetype), Rogue Class 1 (Thug Archetype)


EQUIPMENT
================================================================================

Weapon Set 1 (Active):
  Main Hand:  Scepter Of Woe [Heavy Mace] (Enhancement 3, Scepter Immunity)
  Off Hand:   Tower Shield [Tower Shield]

Weapon Set 2:
  Main Hand:  (empty)
  Off Hand:   (empty)

Weapon Set 3:
  Main Hand:  (empty)
  Off Hand:   (empty)

Weapon Set 4:
  Main Hand:  (empty)
  Off Hand:   (empty)

Armor & Accessories:
  Body      : Fullplate Standart Plus 2 [Fullplate] (Armor Enhancement Bonus 2)
  Head      : Headband Of Charisma 2 (Charisma 2)
  Neck      : Amulet Of Natural Armor 2 (Natural Armor Enhancement 2)
  Belt      : Belt Of Dexterity Constitution 4 (Dexterity 4, Constitution 4)
  Cloak     : Cloak Wyvern Item (Cloak Of Wyvern Enchantment, Natural Armor Enhancement 2)
  Ring 1    : Ring Of Protection 2 (Deflection 2)
  Ring 2    : (empty)
  Bracers   : (empty)
  Gloves    : Crazy Scientists Gloves Item (Acid Resistance 15 Enchant, Fire Resistance 15 Enchant)
  Boots     : Boots Of Swift Foot (Speed Increase)

Quick Slots (Potions, Scrolls, Rods, Wands):
  Slot 1: Potion Of Cure Light Wounds
  Slot 2: Potion Of Shield Of Faith


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

The application uses a comprehensive blueprint database (`blueprint_database.json`) with 45,632+ blueprint names and 2,400+ equipment types extracted from the game files. This database is automatically loaded at startup.

**Database Structure:**
- `Names`: Dictionary mapping blueprint GUIDs to readable names (45,632 entries)
- `EquipmentTypes`: Dictionary mapping equipment GUIDs to weapon/armor/shield types (2,406 entries)

To regenerate the blueprint database (e.g., after a game update):

1. Obtain the updated blueprint files from the game's blueprint dump
   - The current dump (v2.1.4) was obtained from [Pathfinder-Kingmaker-Blueprints](https://github.com/jaygumji/ArcemiPathfinderKingmakerTools) repository
   - Credit to the repository author for maintaining these blueprint dumps
2. Create a folder starting with `Blueprints` (e.g., `Blueprints2.1.4`, `Blueprints2.1.5`) in the solution root directory
3. Place the `Blueprints.txt` file inside that folder
   - Ensure the folder also contains the blueprint JSON subfolders (e.g., `Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon/`)
4. Run the BlueprintBuilder utility from the solution root:
   ```bash
   dotnet run --project BlueprintBuilder
   ```
5. The utility will:
   - Automatically find any folder starting with `Blueprints*`
   - Extract blueprint names from `Blueprints.txt`
   - Extract equipment types from weapon, armor, and shield blueprint JSON files
   - Generate `blueprint_database.json` in the `PathfinderSaveParser` folder (if it exists)
   - Otherwise save to the solution root directory
6. The file will automatically be copied to the output directory during build

**What BlueprintBuilder Does:**
- Filters out unnecessary entries (dialog, cutscene, AI, debug, temporary entries)
- Normalizes blueprint names (adds spacing, removes redundant suffixes)
- Extracts weapon types from `BlueprintItemWeapon` JSON files
- Extracts armor types from `BlueprintItemArmor` JSON files  
- Extracts shield types from `BlueprintItemShield` JSON files (via armor component references)

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
- **Microsoft.Extensions.Configuration** (v10.0.1): Configuration file support
- **Microsoft.Extensions.Configuration.Json** (v10.0.1): JSON configuration provider
- **System.IO.Compression**: ZIP archive extraction for .zks files
- **.NET 8.0**: Runtime framework

### Architecture
- **Models**: POCOs representing save file structure
- **Services**: 
  - `RefResolver`: Handles JSON `$id` and `$ref` pointer resolution
  - `CharacterParser`: Parses character builds using dynamic JSON traversal
  - `EquipmentParser`: Extracts equipped items, weapons, and armor with enchantments
  - `SpellbookParser`: Parses spellcasting information, spell slots, and known spells
  - `KingdomStatsParser`: Extracts kingdom statistics
  - `SaveFileExtractor`: Automatically extracts `.zks` archives and locates save files
  - `BlueprintLookupService`: Resolves GUIDs to human-readable names
  - `SaveFileInspector`: Provides file metadata
  - `TextFileGenerator`: Generates all text report files (kingdom_stats.txt, all_characters.txt, inventory.txt, settlements.txt, explored_locations.txt, CurrentState.txt)
  - `JsonFileGenerator`: Generates all JSON output files (kingdom_stats.json, all_characters.json, inventory.json, settlements.json, explored_locations.json, CurrentState.json)
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

### Running Tests

The project includes comprehensive unit tests covering models, services, and integration scenarios:

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=normal"

# Run specific test class
dotnet test --filter "FullyQualifiedName~JsonOutputBuilderEnchantmentTests"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Test Coverage**: 37 tests across 4 test suites
- Model structure tests (JSON output data models)
- Enchantment parsing tests (validates bug fixes)
- Text formatting tests (output generation)
- Integration tests (complete workflows)

See [PathfinderSaveParser.Tests/README.md](PathfinderSaveParser.Tests/README.md) for detailed test documentation.

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
