# PathfinderSaveParser.Tests

Unit tests for the Pathfinder: Kingmaker Save Build Parser project.

## Test Structure

### Models Tests (`Models/JsonOutputModelsTests.cs`)
Tests for the JSON output data models:
- `InventoryItemJson` - inventory item initialization and property validation
- `WeaponSetJson` - weapon set structure
- `EquipmentJson` - equipment with multiple weapon sets
- `CharacterJson` - character data initialization
- `KingdomStatsJson` - kingdom statistics (includes kingdom days since founding)
- `InventoryCollectionJson` - inventory categorization
- `CurrentStateJson` - complete game state structure

### Services Tests

#### `Services/JsonOutputBuilderEnchantmentTests.cs`
Tests for enchantment parsing (fixes for the inventory bug):
- **Null enchantments handling** - prevents exceptions when enchantments are null
- **Empty JArray handling** - handles empty enchantment arrays
- **JArray with enchantments** - properly parses enchantment lists
- **Nested structure handling** - handles m_Enchantments nested within m_Enchantments
- **JValue handling** - gracefully handles JValue types (the original bug scenario)
- **Negative slot index validation** - skips items with invalid slot indices
- **Item count handling** - properly reads item counts with defaults
- **Shared inventory m_Facts parsing** - validates enchantments from party.json using m_Enchantments.m_Facts[] structure
- **Shared inventory null handling** - handles null enchantments in shared inventory
- **Shared inventory empty facts** - handles empty m_Facts arrays
- **Shared inventory JValue skip** - correctly skips non-object enchantment values (potions, etc.)
- **Blueprint field extraction** - uses "Blueprint" (not "m_Blueprint") from m_Facts entries

These tests specifically validate the fixes made for:
1. The bug where 8 items (Acid Flask, Alchemists Fire, wands, accessories) were being silently skipped due to enchantment parsing exceptions
2. The shared inventory (party.json) enchantment parsing that uses m_Enchantments.m_Facts[] structure instead of direct arrays

#### `Services/TextFileGeneratorTests.cs`
Tests for text output formatting:
- **Equipment slot formatting** - handles null slots, names, types, and enchantments
- **Weapon set formatting** - displays all 4 weapon sets with active indicator
- **Active set indication** - marks active weapon set correctly
- **Multiple weapon sets** - handles all 4 possible sets
- **Both hands populated** - formats main hand and off hand
- **Empty off-hand** - handles two-handed weapons
- **Inventory item formatting** - handles enchantments and item counts

#### `Services/BlueprintLookupTests.cs`
Tests for blueprint ID to name resolution:
- **Unknown blueprints** - returns generic name when blueprint not found
- **Null/empty input handling** - returns "Unknown" for null or empty inputs
- **Equipment type lookup** - returns null for unknown equipment types
- **Service initialization** - validates constructor loads database properly
- **Performance optimization** - uses xUnit class fixture to share single database instance across all 6 tests (loads once instead of 6 times)

### Integration Tests (`Integration/CompleteWorkflowTests.cs`)
Comprehensive workflow tests that validate complete scenarios:

#### `CharacterWithMultipleWeaponSets_ShouldTrackActiveSet`
Validates the weapon set feature using Ekun as an example:
- Multiple weapon sets (Devourer Of Metal, Composite Longbow Frost)
- Active weapon set tracking
- Weapon set switching functionality

#### `InventoryWithMixedItems_ShouldCategorizeProperly`
Tests the inventory bug fix scenario:
- Mixed inventory items across all categories
- Proper item counting (e.g., Acid Flask x17, Alchemists Fire x15)
- Category totals (weapons, armor, accessories, usables, other)

#### `CompleteCharacterData_ShouldHaveAllRequiredFields`
Validates complete character data structure:
- All character fields (name, race, classes, attributes, skills)
- Equipment structure with weapon sets
- Level progression and spellbooks

#### `KingdomState_ShouldTrackAllStats`
Tests complete kingdom state:
- Kingdom stats (military, economy, etc.)
- Advisor assignments
- Resources (gold, build points)

#### `CurrentStateJson_ShouldContainCompleteGameState`
Validates the complete game state export:
- Kingdom data
- Multiple characters
- Settlements
- Explored locations
- Shared inventory

#### `WeaponSetSwitching_ShouldUpdateActiveIndex`
Tests weapon set switching logic:
- Switching between 4 weapon sets
- Active index updates
- Weapon availability across sets

#### `SharedInventory_WithEnchantedWeapon_ShouldParseEnchantments`
Tests the Gift Of Death enchantment parsing:
- Validates enchanted weapons from party.json (shared inventory)
- Confirms all 3 enchantments are parsed (Enhancement 2, Unholy, Against Helpless Plus 2)
- Ensures proper structure for m_Enchantments.m_Facts[] parsing

#### `PersonalChest_AndSharedInventory_BothSupportEnchantments`
Validates enchantment support across both inventory types:
- Personal chest items with enchantments
- Shared inventory items with enchantments
- Ensures consistent behavior between both sources

## Running Tests

Run all tests:
```bash
dotnet test
```

Run with detailed output:
```bash
dotnet test --logger "console;verbosity=normal"
```

Run specific test class:
```bash
dotnet test --filter "FullyQualifiedName~JsonOutputBuilderEnchantmentTests"
```

Run tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Coverage

Current test count: **44 tests**

### Coverage Areas:
- ✅ Data Models (JSON output structures)
- ✅ Enchantment Parsing (bug fix validation)
- ✅ Text File Generation (formatting)
- ✅ Blueprint Lookup Service
- ✅ Weapon Set Feature (all 4 sets with active indicator)
- ✅ Inventory Categorization
- ✅ Complete Workflow Integration
- ✅ Shared Inventory Enchantments (party.json m_Facts structure)

### Bug Regression Tests:
- **Inventory Parsing Bug**: Tests validate that items with various enchantment formats (null, JValue, JArray, nested) are properly parsed without exceptions
- **Weapon Set Display**: Tests confirm all 4 weapon sets are displayed with correct active indicator
- **Shared Inventory Enchantments**: Tests ensure party.json items with m_Enchantments.m_Facts[] structure are parsed correctly (Gift Of Death case)

## Dependencies

- xUnit - testing framework
- Moq - mocking library (for future use)
- Newtonsoft.Json - JSON parsing (used in enchantment tests)

## Notes

- Tests use the actual `BlueprintLookupService` which loads the full blueprint database (47,009 blueprints)
- Enchantment tests specifically target the JSON parsing edge cases discovered during the inventory bug investigation
- Integration tests use real-world scenarios (Ekun's equipment, inventory bug items) for validation
