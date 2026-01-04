using PathfinderSaveParser.Models;

namespace PathfinderSaveParser.Tests.Models;

public class JsonOutputModelsTests
{
    [Fact]
    public void InventoryItemJson_InitializesWithDefaults()
    {
        // Arrange & Act
        var item = new InventoryItemJson();

        // Assert
        Assert.Null(item.Name);
        Assert.Null(item.Type);
        Assert.Equal(0, item.Count);
        Assert.Null(item.Enchantments);
    }

    [Fact]
    public void InventoryItemJson_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var item = new InventoryItemJson
        {
            Name = "Acid Flask",
            Type = "Usable",
            Count = 17,
            Enchantments = new List<string> { "Enhancement 1" }
        };

        // Assert
        Assert.Equal("Acid Flask", item.Name);
        Assert.Equal("Usable", item.Type);
        Assert.Equal(17, item.Count);
        Assert.Single(item.Enchantments);
        Assert.Equal("Enhancement 1", item.Enchantments[0]);
    }

    [Fact]
    public void WeaponSetJson_InitializesCorrectly()
    {
        // Arrange & Act
        var weaponSet = new WeaponSetJson
        {
            SetNumber = 1,
            MainHand = new EquipmentSlotJson { Name = "Longbow" },
            OffHand = null
        };

        // Assert
        Assert.Equal(1, weaponSet.SetNumber);
        Assert.NotNull(weaponSet.MainHand);
        Assert.Equal("Longbow", weaponSet.MainHand.Name);
        Assert.Null(weaponSet.OffHand);
    }

    [Fact]
    public void EquipmentJson_WeaponSets_CanStoreMultipleSets()
    {
        // Arrange & Act
        var equipment = new EquipmentJson
        {
            ActiveWeaponSetIndex = 0,
            WeaponSets = new List<WeaponSetJson>
            {
                new WeaponSetJson 
                { 
                    SetNumber = 1, 
                    MainHand = new EquipmentSlotJson { Name = "Devourer Of Metal" } 
                },
                new WeaponSetJson 
                { 
                    SetNumber = 2, 
                    MainHand = new EquipmentSlotJson { Name = "Composite Longbow" } 
                }
            }
        };

        // Assert
        Assert.Equal(0, equipment.ActiveWeaponSetIndex);
        Assert.NotNull(equipment.WeaponSets);
        Assert.Equal(2, equipment.WeaponSets.Count);
        Assert.Equal("Devourer Of Metal", equipment.WeaponSets[0].MainHand?.Name);
        Assert.Equal("Composite Longbow", equipment.WeaponSets[1].MainHand?.Name);
    }

    [Fact]
    public void CharacterJson_InitializesWithDefaults()
    {
        // Arrange & Act
        var character = new CharacterJson();

        // Assert
        Assert.Null(character.Name);
        Assert.Null(character.Race);
        Assert.Null(character.Classes);
        Assert.Null(character.Attributes);
        Assert.Null(character.Skills);
        Assert.Null(character.Equipment);
    }

    [Fact]
    public void KingdomStatsJson_InitializesCorrectly()
    {
        // Arrange & Act
        var kingdom = new KingdomStatsJson
        {
            Name = "Test Kingdom",
            Alignment = "Lawful Good",
            Days = 100,
            Gold = 5000,
            BuildPoints = 200
        };

        // Assert
        Assert.Equal("Test Kingdom", kingdom.Name);
        Assert.Equal("Lawful Good", kingdom.Alignment);
        Assert.Equal(100, kingdom.Days);
        Assert.Equal(5000, kingdom.Gold);
        Assert.Equal(200, kingdom.BuildPoints);
    }

    [Fact]
    public void InventoryCollectionJson_CountsItemsCorrectly()
    {
        // Arrange & Act
        var collection = new InventoryCollectionJson
        {
            Weapons = new List<InventoryItemJson>
            {
                new InventoryItemJson { Name = "Sword", Count = 1 },
                new InventoryItemJson { Name = "Bow", Count = 2 }
            },
            Armor = new List<InventoryItemJson>
            {
                new InventoryItemJson { Name = "Chain Mail", Count = 1 }
            },
            TotalItems = 4,
            UniqueItems = 3
        };

        // Assert
        Assert.Equal(2, collection.Weapons.Count);
        Assert.Single(collection.Armor);
        Assert.Equal(4, collection.TotalItems);
        Assert.Equal(3, collection.UniqueItems);
    }

    [Fact]
    public void CurrentStateJson_CanContainAllGameData()
    {
        // Arrange & Act
        var state = new CurrentStateJson
        {
            Kingdom = new KingdomStatsJson { Name = "Test Kingdom" },
            Characters = new List<CharacterJson> { new CharacterJson { Name = "Hero" } },
            Settlements = new List<SettlementJson>(),
            ExploredLocations = new List<string> { "Location1", "Location2" },
            Inventory = new InventoryJson()
        };

        // Assert
        Assert.NotNull(state.Kingdom);
        Assert.NotNull(state.Characters);
        Assert.Single(state.Characters);
        Assert.Equal("Hero", state.Characters[0].Name);
        Assert.NotNull(state.ExploredLocations);
        Assert.Equal(2, state.ExploredLocations.Count);
    }
}
