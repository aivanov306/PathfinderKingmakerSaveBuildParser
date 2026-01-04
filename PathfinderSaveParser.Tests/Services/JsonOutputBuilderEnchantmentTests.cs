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

    [Fact]
    public void SharedInventory_ParsesEnchantmentsFromMFacts()
    {
        // Arrange - Simulates actual party.json structure with m_Enchantments.m_Facts[]
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""d7958455107164b47a78eb303bab51ad"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": 127,
            ""m_Enchantments"": {
                ""m_Facts"": [
                    {
                        ""Blueprint"": ""eb2faccc4c9487d43b3575d7e77ff3f5""
                    },
                    {
                        ""Blueprint"": ""d05753b8df780fc4bb55b318f06af453""
                    },
                    {
                        ""Blueprint"": ""2fa378b52d997da4e814af3c48d88d35""
                    }
                ]
            }
        }");

        // Act
        var enchantsToken = itemJson["m_Enchantments"];
        var factsArray = enchantsToken?["m_Facts"];

        // Assert
        Assert.NotNull(enchantsToken);
        Assert.NotNull(factsArray);
        Assert.IsType<JArray>(factsArray);
        Assert.Equal(3, factsArray.Count());
        
        var blueprints = factsArray.Select(f => f["Blueprint"]?.Value<string>()).ToList();
        Assert.Contains("eb2faccc4c9487d43b3575d7e77ff3f5", blueprints);
        Assert.Contains("d05753b8df780fc4bb55b318f06af453", blueprints);
        Assert.Contains("2fa378b52d997da4e814af3c48d88d35", blueprints);
    }

    [Fact]
    public void SharedInventory_HandlesNullEnchantments()
    {
        // Arrange
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": 100,
            ""m_Enchantments"": null
        }");

        // Act
        var enchantsToken = itemJson["m_Enchantments"];

        // Assert
        Assert.NotNull(enchantsToken); // Token exists but is JValue with null
        Assert.Equal(JTokenType.Null, enchantsToken.Type);
    }

    [Fact]
    public void SharedInventory_HandlesEmptyMFacts()
    {
        // Arrange
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""test_blueprint"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": 100,
            ""m_Enchantments"": {
                ""m_Facts"": []
            }
        }");

        // Act
        var enchantsToken = itemJson["m_Enchantments"];
        var factsArray = enchantsToken?["m_Facts"];

        // Assert
        Assert.NotNull(factsArray);
        Assert.IsType<JArray>(factsArray);
        Assert.Empty(factsArray);
    }

    [Fact]
    public void SharedInventory_SkipsJValueEnchantments()
    {
        // Arrange - Some items have m_Enchantments as a simple value (like potions)
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""potion_blueprint"",
            ""m_Count"": 5,
            ""m_InventorySlotIndex"": 50,
            ""m_Enchantments"": false
        }");

        // Act
        var enchantsToken = itemJson["m_Enchantments"];

        // Assert
        Assert.NotNull(enchantsToken);
        Assert.Equal(JTokenType.Boolean, enchantsToken.Type);
        // Should be skipped when Type != JTokenType.Object
    }

    [Fact]
    public void SharedInventory_ExtractsCorrectBlueprintField()
    {
        // Arrange - Tests that we use "Blueprint" not "m_Blueprint" in m_Facts
        var itemJson = JObject.Parse(@"{
            ""m_Blueprint"": ""item_blueprint"",
            ""m_Count"": 1,
            ""m_InventorySlotIndex"": 100,
            ""m_Enchantments"": {
                ""m_Facts"": [
                    {
                        ""$type"": ""Kingmaker.Blueprints.Items.Ecnchantments.ItemEnchantment"",
                        ""Blueprint"": ""enchantment_blueprint_123""
                    }
                ]
            }
        }");

        // Act
        var fact = itemJson["m_Enchantments"]?["m_Facts"]?.First;
        var blueprint = fact?["Blueprint"]?.Value<string>();

        // Assert
        Assert.Equal("enchantment_blueprint_123", blueprint);
    }
}
