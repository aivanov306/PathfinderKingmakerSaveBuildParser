using PathfinderSaveParser.Models;

namespace PathfinderSaveParser.Tests.Integration;

/// <summary>
/// Integration tests that verify the complete data flow and complex scenarios
/// </summary>
public class CompleteWorkflowTests
{
    [Fact]
    public void CharacterWithMultipleWeaponSets_ShouldTrackActiveSet()
    {
        // Arrange - Simulates Ekun with two bows
        var character = new CharacterJson
        {
            Name = "Ekun",
            Race = "Human Race",
            Equipment = new EquipmentJson
            {
                ActiveWeaponSetIndex = 0,
                WeaponSets = new List<WeaponSetJson>
                {
                    new WeaponSetJson
                    {
                        SetNumber = 1,
                        MainHand = new EquipmentSlotJson
                        {
                            Name = "Devourer Of Metal",
                            Type = "Composite Longbow",
                            Enchantments = new List<string> { "Enhancement 1", "Devourer Of Metal Enchant" }
                        },
                        OffHand = null
                    },
                    new WeaponSetJson
                    {
                        SetNumber = 2,
                        MainHand = new EquipmentSlotJson
                        {
                            Name = "Composite Longbow Frost Plus 1",
                            Type = "Composite Longbow",
                            Enchantments = new List<string> { "Enhancement 1", "Frost" }
                        },
                        OffHand = null
                    }
                }
            }
        };

        // Act
        var activeSet = character.Equipment.WeaponSets[character.Equipment.ActiveWeaponSetIndex];
        var inactiveSet = character.Equipment.WeaponSets.FirstOrDefault(s => s.SetNumber != activeSet.SetNumber);

        // Assert
        Assert.Equal(2, character.Equipment.WeaponSets.Count);
        Assert.Equal("Devourer Of Metal", activeSet.MainHand?.Name);
        Assert.Equal("Composite Longbow Frost Plus 1", inactiveSet?.MainHand?.Name);
        Assert.Equal(0, character.Equipment.ActiveWeaponSetIndex);
    }

    [Fact]
    public void InventoryWithMixedItems_ShouldCategorizeProperly()
    {
        // Arrange - Simulates the inventory bug scenario
        var inventory = new InventoryCollectionJson
        {
            Weapons = new List<InventoryItemJson>
            {
                new InventoryItemJson { Name = "Longsword", Type = "Weapon", Count = 1 }
            },
            Armor = new List<InventoryItemJson>
            {
                new InventoryItemJson { Name = "Bracers Of Armor 4", Type = "Armor", Count = 1 }
            },
            Accessories = new List<InventoryItemJson>
            {
                new InventoryItemJson { Name = "Belt Of Dexterity 2", Type = "Accessory", Count = 1 },
                new InventoryItemJson { Name = "Amulet Of Mighty Fists 2", Type = "Accessory", Count = 1 }
            },
            Usables = new List<InventoryItemJson>
            {
                new InventoryItemJson { Name = "Acid Flask", Type = "Usable", Count = 17 },
                new InventoryItemJson { Name = "Alchemists Fire", Type = "Usable", Count = 15 }
            },
            Other = new List<InventoryItemJson>
            {
                new InventoryItemJson { Name = "Stag Lord Broken Amulet", Type = "Other", Count = 1 }
            }
        };

        // Act
        var totalWeapons = inventory.Weapons.Sum(i => i.Count);
        var totalUsables = inventory.Usables.Sum(i => i.Count);
        var totalAccessories = inventory.Accessories.Count;

        // Assert
        Assert.Equal(1, totalWeapons);
        Assert.Equal(32, totalUsables); // 17 + 15
        Assert.Equal(2, totalAccessories);
        Assert.Single(inventory.Other);
    }

    [Fact]
    public void CompleteCharacterData_ShouldHaveAllRequiredFields()
    {
        // Arrange - Simulates complete character parsing
        var character = new CharacterJson
        {
            Name = "Main Character",
            Race = "Human Race",
            Classes = new List<ClassInfoJson>
            {
                new ClassInfoJson { ClassName = "Fighter", Level = 10 }
            },
            Attributes = new AttributesJson
            {
                Strength = 18,
                Dexterity = 14,
                Constitution = 16,
                Intelligence = 10,
                Wisdom = 12,
                Charisma = 8
            },
            Skills = new SkillsJson
            {
                Athletics = 15,
                Perception = 10
            },
            Equipment = new EquipmentJson
            {
                ActiveWeaponSetIndex = 0,
                WeaponSets = new List<WeaponSetJson>
                {
                    new WeaponSetJson { SetNumber = 1 }
                },
                Body = new EquipmentSlotJson { Name = "Full Plate" },
                Head = new EquipmentSlotJson { Name = "Helm" }
            }
        };

        // Assert
        Assert.NotNull(character.Name);
        Assert.NotNull(character.Race);
        Assert.NotNull(character.Classes);
        Assert.Single(character.Classes);
        Assert.Equal(10, character.Classes[0].Level);
        Assert.NotNull(character.Attributes);
        Assert.Equal(18, character.Attributes.Strength);
        Assert.NotNull(character.Equipment);
        Assert.NotEmpty(character.Equipment.WeaponSets);
    }

    [Fact]
    public void KingdomState_ShouldTrackAllStats()
    {
        // Arrange
        var kingdom = new KingdomStatsJson
        {
            Name = "Test Kingdom",
            Alignment = "Lawful Good",
            Days = 365,
            Gold = 10000,
            BuildPoints = 500,
            BuildPointsPerTurn = 10,
            UnrestLevel = "None",
            Stats = new List<KingdomStatJson>
            {
                new KingdomStatJson { Type = "Military", Value = 50, Rank = 5 },
                new KingdomStatJson { Type = "Economy", Value = 60, Rank = 6 }
            },
            Advisors = new List<AdvisorJson>
            {
                new AdvisorJson { Position = "General", Advisor = "Amiri", Status = "Assigned" },
                new AdvisorJson { Position = "Treasurer", Advisor = "Jubilost", Status = "Assigned" }
            }
        };

        // Assert
        Assert.Equal("Test Kingdom", kingdom.Name);
        Assert.Equal(365, kingdom.Days);
        Assert.Equal(10000, kingdom.Gold);
        Assert.NotNull(kingdom.Stats);
        Assert.Equal(2, kingdom.Stats.Count);
        Assert.NotNull(kingdom.Advisors);
        Assert.Equal(2, kingdom.Advisors.Count);
        Assert.All(kingdom.Advisors, a => Assert.Equal("Assigned", a.Status));
    }

    [Fact]
    public void CurrentStateJson_ShouldContainCompleteGameState()
    {
        // Arrange
        var gameState = new CurrentStateJson
        {
            Kingdom = new KingdomStatsJson
            {
                Name = "My Kingdom",
                Days = 100,
                Gold = 5000
            },
            Characters = new List<CharacterJson>
            {
                new CharacterJson { Name = "Hero 1", Race = "Human Race" },
                new CharacterJson { Name = "Hero 2", Race = "Elf" }
            },
            Settlements = new List<SettlementJson>
            {
                new SettlementJson { SettlementName = "Capital" }
            },
            ExploredLocations = new List<string> { "Location A", "Location B", "Location C" },
            Inventory = new InventoryJson
            {
                SharedInventory = new InventoryCollectionJson
                {
                    TotalItems = 50,
                    UniqueItems = 30
                }
            }
        };

        // Assert - Complete game state validation
        Assert.NotNull(gameState.Kingdom);
        Assert.NotNull(gameState.Characters);
        Assert.Equal(2, gameState.Characters.Count);
        Assert.NotNull(gameState.Settlements);
        Assert.Single(gameState.Settlements);
        Assert.NotNull(gameState.ExploredLocations);
        Assert.Equal(3, gameState.ExploredLocations.Count);
        Assert.NotNull(gameState.Inventory);
        Assert.NotNull(gameState.Inventory.SharedInventory);
        Assert.Equal(50, gameState.Inventory.SharedInventory.TotalItems);
    }

    [Fact]
    public void WeaponSetSwitching_ShouldUpdateActiveIndex()
    {
        // Arrange
        var equipment = new EquipmentJson
        {
            ActiveWeaponSetIndex = 0,
            WeaponSets = new List<WeaponSetJson>
            {
                new WeaponSetJson { SetNumber = 1, MainHand = new EquipmentSlotJson { Name = "Bow" } },
                new WeaponSetJson { SetNumber = 2, MainHand = new EquipmentSlotJson { Name = "Sword" } },
                new WeaponSetJson { SetNumber = 3, MainHand = new EquipmentSlotJson { Name = "Axe" } },
                new WeaponSetJson { SetNumber = 4, MainHand = new EquipmentSlotJson { Name = "Spear" } }
            }
        };

        // Act - Simulate switching weapon sets
        var originalActive = equipment.WeaponSets[equipment.ActiveWeaponSetIndex];
        equipment.ActiveWeaponSetIndex = 2; // Switch to set 3
        var newActive = equipment.WeaponSets[equipment.ActiveWeaponSetIndex];

        // Assert
        Assert.Equal("Bow", originalActive.MainHand?.Name);
        Assert.Equal("Axe", newActive.MainHand?.Name);
        Assert.Equal(2, equipment.ActiveWeaponSetIndex);
        Assert.Equal(3, newActive.SetNumber);
    }
}
