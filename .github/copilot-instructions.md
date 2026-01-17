# Pathfinder: Kingmaker Save File Parser - AI Coding Agent Instructions

## Project Overview
C# .NET 8.0 console application that parses Pathfinder: Kingmaker save files (.zks archives or JSON) and exports character builds, kingdom stats, inventory, and settlements as text or JSON reports.

## Architecture

### Three-Project Solution
- **PathfinderSaveParser**: Main console app with parsers and output generators
- **BlueprintBuilder**: Standalone tool to generate blueprint_database.json from game data files
- **PathfinderSaveParser.Tests**: xUnit tests with Moq

### Core Service Architecture Pattern
Services follow a dual-output pattern where parsers both return structured data AND format text:
- **Data Methods**: `ParseData()` returns typed objects (e.g., `EquipmentJson`)
- **Text Methods**: `Parse()` formats data as strings for text reports
- **Shared Logic**: Both delegate to `ParseData()` to avoid duplication (see [REFACTORING_REPORT.md](../REFACTORING_REPORT.md) for ongoing efforts)

Example from [EquipmentParser.cs](../PathfinderSaveParser/Services/EquipmentParser.cs):
```csharp
public EquipmentJson? ParseData(JToken? descriptor)  // Returns structured data
public string ParseEquipment(JToken? descriptor)     // Formats as text (uses ParseData())
```

### Key Services (PathfinderSaveParser/Services/)
- **RefResolver**: Resolves JSON `$ref` pointers by building an index of all `$id` fields in save files
- **BlueprintLookupService**: Maps blueprint GUIDs → human-readable names from blueprint_database.json (2,362 items + 3,720 spells)
- **ItemCategorizationService**: Centralizes item type detection with 4-level priority: Blueprint Type → JSON $type → Equipment Type → Name-based fallback
- **EquipmentParser/SpellbookParser/CharacterParser**: Parse specific game entities (all use RefResolver + BlueprintLookup)
- **TextFileGenerator/JsonFileGenerator**: Output formatting services

### Data Models (PathfinderSaveParser/Models/)
- **SaveFileModels.cs**: Input JSON models (`PlayerSaveFile`, `Kingdom`, `InventoryItem`)
- **JsonOutputModels.cs**: Output JSON models (`CharacterJson`, `EquipmentJson`, `InventoryCollectionJson`)
- **CharacterModels.cs**: Character-specific data structures
- **ReportOptions.cs**: Configuration from appsettings.json

## Critical Workflows

### Build & Test
```powershell
# Build solution
dotnet build

# Run main parser (processes SavedGame folder)
dotnet run --project PathfinderSaveParser

# Run tests
dotnet test

# Build releases for all platforms (Windows/Linux/macOS)
.\scripts\build-releases.ps1 -Version "2.2.0"
```

### Blueprint Database Generation
BlueprintBuilder must be run whenever Blueprints*.txt files update:
```powershell
dotnet run --project BlueprintBuilder
```
This generates `blueprint_database.json` with structure:
```json
{
  "Names": { "guid": "Item Name" },
  "EquipmentTypes": { "guid": "Longsword" },
  "BlueprintTypes": { "guid": "BlueprintItemWeapon" },
  "Descriptions": { "guid": "Full item description text" }
}
```

### Save File Processing Flow
1. **Extract**: SaveFileExtractor extracts .zks → party.json + player.json
2. **Resolve**: RefResolver indexes all $id fields for $ref lookup
3. **Parse**: Services parse JSON using RefResolver + BlueprintLookup
4. **Output**: Generate text files (Output/*.txt) or JSON (Output/current_state.json)

## Project-Specific Conventions

### JSON Handling
- Use **Newtonsoft.Json** (not System.Text.Json) throughout - JToken/JObject API is critical
- Always check `JToken` types before accessing (can be JArray, JValue, or JObject)
- Use RefResolver to handle `{"$ref": "123"}` pointers in save files
- Example pattern from [JsonOutputBuilderEnchantmentTests.cs](../PathfinderSaveParser.Tests/Services/JsonOutputBuilderEnchantmentTests.cs):
```csharp
if (enchantments is JArray enchantsArray) { ... }
else if (enchantments is JValue) { continue; }  // Skip non-parseable items
```

### Service Dependency Injection Pattern
Services use constructor injection with nullable parameters:
```csharp
public EquipmentParser(BlueprintLookupService blueprintLookup, RefResolver resolver, ReportOptions options)
```
Main program instantiates manually (no DI container used).

### Item Categorization Priority
When adding new item categorization logic, respect the priority order:
1. Blueprint Type from database (most reliable)
2. JSON `$type` field from party.json
3. Equipment Type from database
4. Name-based detection (fallback)

See [ItemCategorizationService.cs](../PathfinderSaveParser/Services/ItemCategorizationService.cs) line 12-42.

### Configuration
appsettings.json controls output verbosity:
- `ReportOptions.*`: Toggle sections (IncludeEquipment, ShowItemDescriptions, etc.)
- `CharacterDisplayOrder`: Custom character ordering in reports
- `PathfinderSaveLocation`: Default save location (supports environment variables)

## Testing Patterns

### Test File Structure
Tests mirror the Services folder structure:
- Models tests: Validate data model initialization
- Service tests: Focus on parsing edge cases (null handling, empty arrays, type mismatches)

### Critical Test Scenarios
When modifying parsers, ensure tests cover:
- Null/empty input handling
- JArray vs JValue vs JObject type variations
- Nested JSON structures (e.g., `m_Enchantments.m_Facts[]`)
- Negative indices and invalid data
- See [JsonOutputBuilderEnchantmentTests.cs](../PathfinderSaveParser.Tests/Services/JsonOutputBuilderEnchantmentTests.cs) for comprehensive examples

## Known Refactoring Areas

REFACTORING_REPORT.md (in Russian) documents code duplication issues:
- ✅ Completed: ItemCategorizationService extraction, EquipmentParser.ParseData()
- ⚠️ Remaining: SpellbookParser/InventoryParser need ParseData() methods to fully eliminate duplication in JsonOutputBuilder

When refactoring parsers:
1. Add `ParseData()` method returning structured objects first
2. Refactor existing `Parse()` to use `ParseData()` + formatting
3. Update JsonOutputBuilder to call `ParseData()` instead of duplicating logic

## File Path Conventions
- Input: SavedGame/ (party.json, player.json, or .zks archives)
- Output: Output/ (text reports and JSON exports)
- Config: appsettings.json (root of PathfinderSaveParser/)
- Blueprint DB: blueprint_database.json (copied to output during build)

## Integration Points
- **Blueprint Database**: External game data (Blueprints2.1.4/Blueprints.txt) processed by BlueprintBuilder
- **Save Files**: JSON format with $ref/$id pointers requiring RefResolver
- **Configuration**: Microsoft.Extensions.Configuration for appsettings.json binding
