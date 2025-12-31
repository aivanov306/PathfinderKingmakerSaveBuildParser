================================================================================
  Pathfinder: Kingmaker Save File Parser
================================================================================

A tool to extract and analyze character builds, equipment, and kingdom 
statistics from Pathfinder: Kingmaker save files.

================================================================================
  HOW TO RUN
================================================================================

WINDOWS
-------
Simply double-click: PathfinderSaveParser.exe

Or from command line:
    PathfinderSaveParser.exe

LINUX / macOS
-------------
Make the application executable (first time only):
    chmod +x PathfinderSaveParser

Then run:
    ./PathfinderSaveParser

Or use the .NET runtime directly:
    dotnet PathfinderSaveParser.dll


================================================================================
  AUTOMATIC SAVE FILE DETECTION
================================================================================

The parser automatically finds your save files in three ways:

1. Checks SavedGame/ folder for player.json and party.json
2. Checks SavedGame/ folder for .zks archives and extracts them
3. Searches your default Pathfinder save location:
   
   Windows:  %USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games
   Linux:    ~/.config/unity3d/Owlcat Games/Pathfinder Kingmaker/Saved Games
   macOS:    ~/Library/Application Support/unity3d/Owlcat Games/Pathfinder Kingmaker/Saved Games

No manual extraction needed - just run the parser!


================================================================================
  CONFIGURATION (OPTIONAL)
================================================================================

Edit appsettings.json to customize behavior:

FILE LOCATION SETTINGS
----------------------
{
  "PathfinderSaveLocation": "path/to/your/saves",
  "DefaultSaveFile": "Auto_1.zks"
}

- PathfinderSaveLocation: Custom save location (supports environment variables)
- DefaultSaveFile: Specific save to use (e.g., "Auto_1.zks", "Manual_3.zks")
  * If specified and exists → uses that exact save
  * If specified but missing → falls back to most recent save
  * If empty → always uses most recent save


REPORT OPTIONS
--------------
Control what information appears in generated reports:

"ReportOptions": {
  "IncludeStats": true,              // Character ability scores
  "IncludeRace": true,               // Character race
  "IncludeClass": true,              // Classes and archetypes
  "IncludeEquipment": true,          // All equipment sections
  "IncludeLevelHistory": true,       // Level-by-level progression
  "IncludeKingdomStats": true,       // Kingdom statistics
  "IncludeKingdomAdvisors": true,    // Kingdom advisor assignments
  "IncludeActiveWeaponSet": true,    // Current weapon set
  "IncludeArmor": true,              // Equipped armor
  "IncludeAccessories": true,        // Rings, belts, etc.
  "ShowEmptySlots": true,            // Display empty equipment slots
  "ShowEnchantments": true,          // Show item enchantments
  "ShowFeatParameters": true,        // Show weapon types in feats
  "IncludeSpellcasting": true        // Spell slots and available spells
}

EXAMPLE: Minimal output (just class info):
  Set IncludeStats, IncludeEquipment, IncludeSpellcasting, IncludeLevelHistory to false

EXAMPLE: Clean equipment view:
  Set ShowEmptySlots and ShowEnchantments to false


CHARACTER OPTIONS
-----------------
Control character display order and visibility:

"CharacterDisplayOrder": ["Melaku", "Regongar", "Harrim", ...]
"ExcludeCharacters": ["Tartuccio"]

- CharacterDisplayOrder: Characters matching these patterns appear first
  * Uses case-insensitive partial matching
  * Remaining characters appear after, sorted alphabetically
  * Example: "Smilodon" matches "Animal Companion Unit Smilodon"

- ExcludeCharacters: Characters matching these patterns are hidden
  * Useful for hiding temporary companions or unwanted characters
  * Example: "Tartuccio" excludes characters with that name


================================================================================
  MANUAL OPTIONS
================================================================================

You can also manually place files in the SavedGame/ folder:
- Copy .zks save archives directly, or
- Extract .zks files and copy player.json and party.json


================================================================================
  OUTPUT
================================================================================

Results are saved to the Output/ folder:
- kingdom_stats.txt - Kingdom statistics, gold, BP, and advisor assignments
- all_characters.txt - Complete character builds with equipment

All output is also displayed in the console.


================================================================================
  FEATURES
================================================================================

✓ Character builds (race, classes, abilities)
✓ Level-by-level feat and feature progression
✓ Equipment with enchantments (weapons, armor, accessories)
✓ Spellcasting information (spell slots per day and known spells)
✓ Kingdom statistics (all 10 stats with ranks)
✓ Kingdom advisors (shows assignments and availability)
✓ Gold tracking (displays current party gold)
✓ Character ordering (customize output order)
✓ Character exclusion (hide specific characters)
✓ Support for main character + all companions
✓ Automatic blueprint name resolution (45,632+ entries)


================================================================================
  TROUBLESHOOTING
================================================================================

Save files not found:
- Check PathfinderSaveLocation in appsettings.json matches your game location
- Ensure you have read permissions for the save folder
- Check console output for detailed error messages

Unknown blueprint names:
- Database includes blueprints from game version 2.1.4
- Regenerate blueprint database if using a different game version

For more information, see the main README.md in the repository.


================================================================================
  LICENSE & CREDITS
================================================================================

MIT License - Free for personal use
Pathfinder: Kingmaker is owned by Owlcat Games and Paizo Inc.

This is an unofficial fan tool.
