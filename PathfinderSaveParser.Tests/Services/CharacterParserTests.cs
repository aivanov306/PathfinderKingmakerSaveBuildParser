using Newtonsoft.Json.Linq;
using PathfinderSaveParser.Models;
using PathfinderSaveParser.Services;

namespace PathfinderSaveParser.Tests.Services;

/// <summary>
/// Tests for CharacterParser to ensure spellcasting data is not lost during refactoring
/// </summary>
public class CharacterParserTests
{
    [Fact]
    public void FormatCharacter_WithFormattedSpellcasting_IncludesSpellcastingSection()
    {
        // Arrange
        var options = new ReportOptions { IncludeSpellcasting = true };
        var blueprintLookup = new BlueprintLookupService();
        var emptyRoot = new Newtonsoft.Json.Linq.JObject();
        var resolver = new RefResolver(emptyRoot);
        var parser = new EnhancedCharacterParser(blueprintLookup, resolver, options);

        var character = new CharacterJson
        {
            Name = "Test Cleric",
            Race = "Human",
            Alignment = "Lawful Good",
            Classes = new List<ClassInfoJson>
            {
                new ClassInfoJson { ClassName = "Cleric", Level = 5 }
            },
            Attributes = new AttributesJson
            {
                Strength = 10,
                Dexterity = 10,
                Constitution = 10,
                Intelligence = 10,
                Wisdom = 16,
                Charisma = 12
            },
            FormattedSpellcasting = @"SPELLCASTING
================================================================================

Cleric Spellbook (Caster Level 5)
------------------------------------------------------------
Spell Slots per Day:
  Level 1: 4 slots + 1 domain slot

Known Spells:
  Level 0: Guidance, Light
  Level 1: Bless, Cure Light Wounds, Protection from Law (Domain)"
        };

        // Act
        var result = parser.FormatCharacter(character);

        // Assert
        Assert.Contains("SPELLCASTING", result);
        Assert.Contains("Cleric Spellbook", result);
        Assert.Contains("Caster Level 5", result);
        Assert.Contains("domain slot", result);
        Assert.Contains("Protection from Law (Domain)", result);
    }

    [Fact]
    public void FormatCharacter_WithoutSpellcasting_OmitsSpellcastingSection()
    {
        // Arrange
        var options = new ReportOptions { IncludeSpellcasting = false };
        var blueprintLookup = new BlueprintLookupService();
        var emptyRoot = new Newtonsoft.Json.Linq.JObject();
        var resolver = new RefResolver(emptyRoot);
        var parser = new EnhancedCharacterParser(blueprintLookup, resolver, options);

        var character = new CharacterJson
        {
            Name = "Test Fighter",
            Race = "Human",
            Classes = new List<ClassInfoJson>
            {
                new ClassInfoJson { ClassName = "Fighter", Level = 5 }
            },
            Attributes = new AttributesJson
            {
                Strength = 18,
                Dexterity = 14,
                Constitution = 14,
                Intelligence = 10,
                Wisdom = 10,
                Charisma = 10
            },
            FormattedSpellcasting = null
        };

        // Act
        var result = parser.FormatCharacter(character);

        // Assert
        Assert.DoesNotContain("SPELLCASTING", result);
    }

    [Fact]
    public void FormatCharacter_WithEmptyFormattedSpellcasting_OmitsSpellcastingSection()
    {
        // Arrange
        var options = new ReportOptions { IncludeSpellcasting = true };
        var blueprintLookup = new BlueprintLookupService();
        var emptyRoot = new Newtonsoft.Json.Linq.JObject();
        var resolver = new RefResolver(emptyRoot);
        var parser = new EnhancedCharacterParser(blueprintLookup, resolver, options);

        var character = new CharacterJson
        {
            Name = "Test Fighter",
            Race = "Human",
            Classes = new List<ClassInfoJson>
            {
                new ClassInfoJson { ClassName = "Fighter", Level = 5 }
            },
            Attributes = new AttributesJson
            {
                Strength = 18,
                Dexterity = 14,
                Constitution = 14,
                Intelligence = 10,
                Wisdom = 10,
                Charisma = 10
            },
            FormattedSpellcasting = string.Empty
        };

        // Act
        var result = parser.FormatCharacter(character);

        // Assert
        Assert.DoesNotContain("SPELLCASTING", result);
    }

    [Fact]
    public void CharacterJson_HasFormattedSpellcastingProperty()
    {
        // This test ensures FormattedSpellcasting property exists and is usable
        // Prevents accidental removal during refactoring

        // Arrange & Act
        var character = new CharacterJson
        {
            Name = "Test",
            FormattedSpellcasting = "Test spellcasting text"
        };

        // Assert
        Assert.NotNull(character.FormattedSpellcasting);
        Assert.Equal("Test spellcasting text", character.FormattedSpellcasting);
    }
}
