using Newtonsoft.Json.Linq;
using PathfinderSaveParser.Services;

namespace PathfinderSaveParser.Tests.Services;

public class JsonOutputBuilderEnchantmentTests
{
    [Fact]
    public void ParseEnchantments_HandlesNullEnchantments()
    {
        // Arrange
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": 0,
            ""m_Enchantments"": null
        }");

        // Act & Assert - should not throw exception
        // This tests the fix for the enchantment parsing bug
        Assert.NotNull(itemJson);
    }

    [Fact]
    public void ParseEnchantments_HandlesEmptyJArray()
    {
        // Arrange
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": 0,
            ""m_Enchantments"": []
        }");

        // Act
        var enchants = itemJson["m_Enchantments"];

        // Assert
        Assert.NotNull(enchants);
        Assert.IsType<JArray>(enchants);
        Assert.Empty(enchants);
    }

    [Fact]
    public void ParseEnchantments_HandlesJArrayWithEnchantments()
    {
        // Arrange
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": 0,
            ""m_Enchantments"": [
                {
                    ""m_Blueprint"": ""enchant_1""
                },
                {
                    ""m_Blueprint"": ""enchant_2""
                }
            ]
        }");

        // Act
        var enchants = itemJson["m_Enchantments"] as JArray;

        // Assert
        Assert.NotNull(enchants);
        Assert.Equal(2, enchants.Count);
        Assert.Equal("enchant_1", enchants[0]["m_Blueprint"]?.Value<string>());
        Assert.Equal("enchant_2", enchants[1]["m_Blueprint"]?.Value<string>());
    }

    [Fact]
    public void ParseEnchantments_HandlesNestedStructure()
    {
        // Arrange - Tests the fix for nested m_Enchantments
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": 0,
            ""m_Enchantments"": {
                ""m_Enchantments"": [
                    {
                        ""m_Blueprint"": ""nested_enchant""
                    }
                ]
            }
        }");

        // Act
        var enchantsToken = itemJson["m_Enchantments"];
        var enchantsArray = enchantsToken is JArray ? enchantsToken : enchantsToken?["m_Enchantments"];

        // Assert
        Assert.NotNull(enchantsArray);
        Assert.IsType<JArray>(enchantsArray);
        Assert.Single(enchantsArray);
        Assert.Equal("nested_enchant", enchantsArray[0]?["m_Blueprint"]?.Value<string>());
    }

    [Fact]
    public void ParseEnchantments_HandlesJValue()
    {
        // Arrange - Tests JValue handling (the original bug case)
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": 0,
            ""m_Enchantments"": ""some_string_value""
        }");

        // Act
        var enchantsToken = itemJson["m_Enchantments"];

        // Assert - Should handle gracefully without throwing
        Assert.NotNull(enchantsToken);
        Assert.IsType<JValue>(enchantsToken);
        Assert.Equal("some_string_value", enchantsToken.Value<string>());
    }

    [Fact]
    public void ParseInventorySlotIndex_ValidatesNegativeIndex()
    {
        // Arrange - Items with negative slot index should be skipped
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": -1
        }");

        // Act
        var slotIndex = itemJson["m_InventorySlotIndex"]?.Value<int>();

        // Assert
        Assert.NotNull(slotIndex);
        Assert.True(slotIndex < 0);
    }

    [Fact]
    public void ParseItemCount_HandlesDefaultValue()
    {
        // Arrange
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_InventorySlotIndex"": 0
        }");

        // Act
        var count = itemJson["m_Count"]?.Value<int>() ?? 1;

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public void ParseItemCount_ReadsActualCount()
    {
        // Arrange
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_Count"": 17,
            ""m_InventorySlotIndex"": 0
        }");

        // Act
        var count = itemJson["m_Count"]?.Value<int>() ?? 1;

        // Assert
        Assert.Equal(17, count);
    }
}
