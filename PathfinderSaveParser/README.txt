================================================================================
  Pathfinder: Kingmaker Save File Parser v1.2.0
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

{
  "PathfinderSaveLocation": "path/to/your/saves",
  "DefaultSaveFile": "Auto_1.zks"
}

- PathfinderSaveLocation: Custom save location (supports environment variables)
- DefaultSaveFile: Specific save to use (e.g., "Auto_1.zks", "Manual_3.zks")
  * If specified and exists → uses that exact save
  * If specified but missing → falls back to most recent save
  * If empty → always uses most recent save


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
- kingdom_stats.txt - Kingdom statistics and BP information
- all_characters.txt - Complete character builds with equipment

All output is also displayed in the console.


================================================================================
  FEATURES
================================================================================

✓ Character builds (race, classes, abilities)
✓ Level-by-level feat and feature progression
✓ Equipment with enchantments (weapons, armor, accessories)
✓ Kingdom statistics (all 10 stats with ranks)
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
