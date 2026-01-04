using PathfinderSaveParser.Services;

namespace PathfinderSaveParser.Tests.Services;

/// <summary>
/// Fixture to share a single BlueprintLookupService instance across all tests.
/// This prevents loading the 47,009 blueprint database 6 times.
/// </summary>
public class BlueprintLookupFixture
{
    public BlueprintLookupService Service { get; }

    public BlueprintLookupFixture()
    {
        Service = new BlueprintLookupService();
    }
}

public class BlueprintLookupTests : IClassFixture<BlueprintLookupFixture>
{
    private readonly BlueprintLookupService _lookup;

    public BlueprintLookupTests(BlueprintLookupFixture fixture)
    {
        _lookup = fixture.Service;
    }

    [Fact]
    public void GetName_ReturnsGenericName_WhenBlueprintNotFound()
    {
        // Arrange
        var unknownBlueprint = "unknown_blueprint_id_12345678";

        // Act
        var result = _lookup.GetName(unknownBlueprint);

        // Assert - Should return Blueprint_ followed by first 8 chars
        Assert.StartsWith("Blueprint_", result);
        Assert.Equal("Blueprint_unknown_", result);
    }

    [Fact]
    public void GetName_ReturnsUnknown_ForNullInput()
    {
        // Act
        var result = _lookup.GetName(null);

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void GetName_ReturnsUnknown_ForEmptyString()
    {
        // Act
        var result = _lookup.GetName("");

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void GetEquipmentType_ReturnsNull_WhenBlueprintNotFound()
    {
        // Arrange
        var unknownBlueprint = "unknown_blueprint_id";

        // Act
        var result = _lookup.GetEquipmentType(unknownBlueprint);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetEquipmentType_HandlesNullInput()
    {
        // Act
        var result = _lookup.GetEquipmentType(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_InitializesWithoutError()
    {
        // Act & Assert - service is already initialized via fixture
        Assert.NotNull(_lookup);
    }
}
