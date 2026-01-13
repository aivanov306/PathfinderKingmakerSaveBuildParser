using PathfinderSaveParser.Services;

namespace PathfinderSaveParser.Tests.Services;

public class ItemCategorizationTests
{
    private readonly ItemCategorizationService _service;

    public ItemCategorizationTests()
    {
        _service = new ItemCategorizationService();
    }

    [Theory]
    [InlineData("Kingmaker.Items.ItemEntityWeapon, Assembly-CSharp", "Weapon")]
    [InlineData("Kingmaker.Items.ItemEntityArmor, Assembly-CSharp", "Armor")]
    [InlineData("Kingmaker.Items.ItemEntityShield, Assembly-CSharp", "Armor")]
    [InlineData("Kingmaker.Items.ItemEntityUsable, Assembly-CSharp", "Usable")]
    [InlineData("Kingmaker.Items.ItemEntitySimple, Assembly-CSharp", null)]
    public void GetCategoryFromJsonType_CategorizesCorrectly(string jsonType, string? expectedCategory)
    {
        // Act
        var category = _service.GetCategoryFromJsonType(jsonType);

        // Assert
        Assert.Equal(expectedCategory, category);
    }

    [Fact]
    public void GetCategoryFromJsonType_HandlesNullInput()
    {
        // Act
        var category = _service.GetCategoryFromJsonType(null);

        // Assert
        Assert.Null(category);
    }

    [Fact]
    public void GetCategoryFromJsonType_HandlesEmptyString()
    {
        // Act
        var category = _service.GetCategoryFromJsonType("");

        // Assert
        Assert.Null(category);
    }

    [Fact]
    public void GetCategoryFromJsonType_IsCaseInsensitive()
    {
        // Act
        var category = _service.GetCategoryFromJsonType("kingmaker.items.itementityweapon, assembly-csharp");

        // Assert
        Assert.Equal("Weapon", category);
    }

    [Fact]
    public void GetCategoryFromJsonType_HandlesUnknownType()
    {
        // Act
        var category = _service.GetCategoryFromJsonType("Unknown.Type, Assembly");

        // Assert
        Assert.Null(category);
    }

    [Theory]
    [InlineData("Longsword", true)]
    [InlineData("Dwarven Waraxe", true)]
    [InlineData("Flail", true)]
    [InlineData("Shortbow", true)]
    [InlineData("Potion", false)]
    [InlineData("Ring of Protection", false)]
    public void IsWeaponByName_CategorizesWeaponsCorrectly(string name, bool expected)
    {
        // Act
        var result = _service.IsWeaponByName(name);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Chainmail +1", true)]
    [InlineData("Fullplate Armor", true)]
    [InlineData("Halfplate", true)]
    [InlineData("Breastplate", true)]
    [InlineData("Potion", false)]
    [InlineData("Ring", false)]
    public void IsArmorByName_CategorizesArmorCorrectly(string name, bool expected)
    {
        // Act
        var result = _service.IsArmorByName(name);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Ring of Protection", true)]
    [InlineData("Belt of Giant Strength", true)]
    [InlineData("Hat of Disguise", true)]
    [InlineData("Cap of Wisdom", true)]
    [InlineData("Black Rattlecap", false)] // Should NOT match "cap" in the middle
    [InlineData("Handicap", false)] // Should NOT match "cap" at the end
    [InlineData("Amulet", true)]
    [InlineData("Cloak", true)]
    public void IsAccessory_UsesWordBoundaries(string name, bool expected)
    {
        // Act
        var result = _service.IsAccessory(name);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Potion of Cure Light Wounds", true)]
    [InlineData("Scroll of Fireball", true)]
    [InlineData("Wand of Magic Missile", true)]
    [InlineData("Longsword", false)]
    [InlineData("Ring", false)]
    public void IsUsable_CategorizesUsablesCorrectly(string name, bool expected)
    {
        // Act
        var result = _service.IsUsable(name);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Longsword", true)]
    [InlineData("Dwarven Waraxe +1", true)]
    [InlineData("Heavy Flail", true)]
    [InlineData("Composite Longbow", true)]
    [InlineData("Potion", false)]
    public void IsWeapon_ChecksEquipmentType(string equipmentType, bool expected)
    {
        // Act
        var result = _service.IsWeapon(equipmentType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Light Armor", true)]
    [InlineData("Medium Armor", true)]
    [InlineData("Heavy Armor", true)]
    [InlineData("Buckler", true)]
    [InlineData("Light Shield", true)]
    [InlineData("Longsword", false)]
    public void IsArmor_ChecksEquipmentType(string equipmentType, bool expected)
    {
        // Act
        var result = _service.IsArmor(equipmentType);

        // Assert
        Assert.Equal(expected, result);
    }
}
