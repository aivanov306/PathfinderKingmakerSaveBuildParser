# SavedGame Folder

This folder is where the parser looks for save files.

## Automatic Extraction (Recommended)

The parser automatically finds and extracts save files:
1. Checks for player.json and party.json in this folder
2. If not found, checks for .zks files in this folder and extracts them
3. If still not found, searches your default Pathfinder save location

Simply run the parser and it will handle everything!

## Manual Options

You can also manually place files here:
- Copy .zks save files directly to this folder, or
- Extract .zks archives and copy player.json and party.json here

## Configuration

Edit appsettings.json to:
- Set a custom PathfinderSaveLocation
- Specify a DefaultSaveFile to use a specific save instead of the most recent one
