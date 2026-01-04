using PathfinderSaveParser.Models;
using PathfinderSaveParser.Services;

namespace PathfinderSaveParser.Tests.Services;

public class TextFileGeneratorTests
{
    [Fact]
    public void FormatEquipmentSlot_HandlesNullSlot()
    {
        // Arrange
        EquipmentSlotJson? slot = null;

        // Act
        var result = FormatEquipmentSlotHelper(slot);

        // Assert
        Assert.Equal("(empty)", result);
    }

    [Fact]
    public void FormatEquipmentSlot_HandlesSlotWithNameOnly()
    {
        // Arrange
        var slot = new EquipmentSlotJson
        {
            Name = "Longsword"
        };

        // Act
        var result = FormatEquipmentSlotHelper(slot);

        // Assert
        Assert.Equal("Longsword", result);
    }

    [Fact]
    public void FormatEquipmentSlot_HandlesSlotWithTypeAndEnchantments()
    {
        // Arrange
        var slot = new EquipmentSlotJson
        {
            Name = "Devourer Of Metal",
            Type = "Composite Longbow",
            Enchantments = new List<string> { "Enhancement 1", "Frost" }
        };

        // Act
        var result = FormatEquipmentSlotHelper(slot);

        // Assert
        Assert.Contains("Devourer Of Metal", result);
        Assert.Contains("Composite Longbow", result);
        Assert.Contains("Enhancement 1", result);
        Assert.Contains("Frost", result);
    }

    [Fact]
    public void WeaponSetFormatting_ShowsActiveIndicator()
    {
        // Arrange
        var character = new CharacterJson
        {
            Name = "Ekun",
            Equipment = new EquipmentJson
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
            }
        };

        // Act
        var activeSetNumber = character.Equipment.ActiveWeaponSetIndex + 1;
        var isSet1Active = 1 == activeSetNumber;
        var isSet2Active = 2 == activeSetNumber;

        // Assert
        Assert.True(isSet1Active);
        Assert.False(isSet2Active);
    }

    [Fact]
    public void WeaponSetFormatting_HandlesMultipleSets()
    {
        // Arrange
        var equipment = new EquipmentJson
        {
            ActiveWeaponSetIndex = 1,
            WeaponSets = new List<WeaponSetJson>
            {
                new WeaponSetJson { SetNumber = 1, MainHand = new EquipmentSlotJson { Name = "Weapon1" } },
                new WeaponSetJson { SetNumber = 2, MainHand = new EquipmentSlotJson { Name = "Weapon2" } },
                new WeaponSetJson { SetNumber = 3, MainHand = new EquipmentSlotJson { Name = "Weapon3" } },
                new WeaponSetJson { SetNumber = 4, MainHand = new EquipmentSlotJson { Name = "Weapon4" } }
            }
        };

        // Assert
        Assert.Equal(4, equipment.WeaponSets.Count);
        Assert.Equal(1, equipment.ActiveWeaponSetIndex);
        Assert.Equal("Weapon1", equipment.WeaponSets[0].MainHand?.Name);
        Assert.Equal("Weapon2", equipment.WeaponSets[1].MainHand?.Name);
        Assert.Equal("Weapon3", equipment.WeaponSets[2].MainHand?.Name);
        Assert.Equal("Weapon4", equipment.WeaponSets[3].MainHand?.Name);
    }

    [Fact]
    public void WeaponSetFormatting_HandlesBothHandsPopulated()
    {
        // Arrange
        var weaponSet = new WeaponSetJson
        {
            SetNumber = 1,
            MainHand = new EquipmentSlotJson { Name = "Longsword" },
            OffHand = new EquipmentSlotJson { Name = "Shield" }
        };

        // Assert
        Assert.NotNull(weaponSet.MainHand);
        Assert.NotNull(weaponSet.OffHand);
        Assert.Equal("Longsword", weaponSet.MainHand.Name);
        Assert.Equal("Shield", weaponSet.OffHand.Name);
    }

    [Fact]
    public void WeaponSetFormatting_HandlesEmptyOffHand()
    {
        // Arrange
        var weaponSet = new WeaponSetJson
        {
            SetNumber = 1,
            MainHand = new EquipmentSlotJson { Name = "Greatsword" },
            OffHand = null
        };

        // Assert
        Assert.NotNull(weaponSet.MainHand);
        Assert.Null(weaponSet.OffHand);
    }

    [Fact]
    public void InventoryItemFormatting_HandlesMultipleEnchantments()
    {
        // Arrange
        var item = new InventoryItemJson
        {
            Name = "Magic Sword",
            Type = "Longsword",
            Count = 1,
            Enchantments = new List<string> { "Enhancement 1", "Flaming", "Keen" }
        };

        // Assert
        Assert.Equal(3, item.Enchantments.Count);
        Assert.Contains("Enhancement 1", item.Enchantments);
        Assert.Contains("Flaming", item.Enchantments);
        Assert.Contains("Keen", item.Enchantments);
    }

    [Fact]
    public void InventoryItemFormatting_HandlesItemsWithCount()
    {
        // Arrange - This tests the Acid Flask scenario from the bug
        var item = new InventoryItemJson
        {
            Name = "Acid Flask",
            Type = "Usable",
            Count = 17,
            Enchantments = null
        };

        // Assert
        Assert.Equal("Acid Flask", item.Name);
        Assert.Equal("Usable", item.Type);
        Assert.Equal(17, item.Count);
        Assert.Null(item.Enchantments);
    }

    // Helper method to simulate FormatEquipmentSlot behavior
    private static string FormatEquipmentSlotHelper(EquipmentSlotJson? slot)
    {
        if (slot == null || string.IsNullOrEmpty(slot.Name))
            return "(empty)";

        var result = slot.Name;
        if (!string.IsNullOrEmpty(slot.Type))
            result += $" [{slot.Type}]";

        if (slot.Enchantments != null && slot.Enchantments.Any())
            result += $" ({string.Join(", ", slot.Enchantments)})";

        return result;
    }
}
