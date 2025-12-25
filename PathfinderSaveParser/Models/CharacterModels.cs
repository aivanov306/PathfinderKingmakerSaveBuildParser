using Newtonsoft.Json;

namespace PathfinderSaveParser.Models;

public class PartySaveFile
{
    [JsonProperty("m_EntityData")]
    public List<Character>? Characters { get; set; }
}

public class Character
{
    [JsonProperty("UniqueId")]
    public string? UniqueId { get; set; }

    [JsonProperty("Descriptor")]
    public CharacterDescriptor? Descriptor { get; set; }
}

public class CharacterDescriptor
{
    [JsonProperty("CustomName")]
    public string? CustomName { get; set; }

    [JsonProperty("Blueprint")]
    public string? Blueprint { get; set; }

    [JsonProperty("Progression")]
    public CharacterProgression? Progression { get; set; }

    [JsonProperty("Stats")]
    public StatsContainer? Stats { get; set; }

    [JsonProperty("m_Facts")]
    public FactsCollection? Facts { get; set; }
}

public class CharacterProgression
{
    [JsonProperty("CharacterLevel")]
    public int CharacterLevel { get; set; }

    [JsonProperty("Classes")]
    public List<ClassData>? Classes { get; set; }

    [JsonProperty("m_LevelPlans")]
    public List<LevelUpPlan>? LevelPlans { get; set; }

    [JsonProperty("m_Selections")]
    public List<SelectionEntry>? Selections { get; set; }

    [JsonProperty("Race")]
    public string? Race { get; set; }

    [JsonProperty("Features")]
    public FeaturesCollection? Features { get; set; }
}

public class ClassData
{
    [JsonProperty("CharacterClass")]
    public string? CharacterClass { get; set; }

    [JsonProperty("Level")]
    public int Level { get; set; }

    [JsonProperty("Archetypes")]
    public List<string>? Archetypes { get; set; }
}

public class SelectionEntry
{
    [JsonProperty("Key")]
    public string? SelectionType { get; set; }

    [JsonProperty("Value")]
    public SelectionValue? Value { get; set; }
}

public class SelectionValue
{
    [JsonProperty("m_SelectionsByLevel")]
    public List<SelectionByLevel>? SelectionsByLevel { get; set; }
}

public class LevelUpPlan
{
    [JsonProperty("CharacterLevel")]
    public int CharacterLevel { get; set; }

    [JsonProperty("Class")]
    public string? Class { get; set; }

    [JsonProperty("SelectedSpells")]
    public List<string>? SelectedSpells { get; set; }

    [JsonProperty("m_Selections")]
    public List<FeatureSelection>? Selections { get; set; }

    [JsonProperty("StatsDistribution")]
    public Dictionary<string, int>? StatsDistribution { get; set; }

    [JsonProperty("SkillPoints")]
    public List<SkillPoint>? SkillPoints { get; set; }
}

public class FeatureSelection
{
    [JsonProperty("m_Selection")]
    public string? Selection { get; set; }

    [JsonProperty("m_Features")]
    public List<string>? Features { get; set; }

    [JsonProperty("m_SelectionsByLevel")]
    public List<SelectionByLevel>? SelectionsByLevel { get; set; }
}

public class SelectionByLevel
{
    [JsonProperty("Key")]
    public int Level { get; set; }

    [JsonProperty("Value")]
    public List<string>? FeatureGuids { get; set; }
}

public class SkillPoint
{
    [JsonProperty("Key")]
    public string? Skill { get; set; }

    [JsonProperty("Value")]
    public int Points { get; set; }
}

public class StatsContainer
{
    [JsonProperty("m_Stats")]
    public List<StatData>? Stats { get; set; }
}

public class StatData
{
    [JsonProperty("Type")]
    public string? Type { get; set; }

    [JsonProperty("BaseValue")]
    public int BaseValue { get; set; }

    [JsonProperty("m_BaseValue")]
    public int? AlternateBaseValue { get; set; }
}

public class FactsCollection
{
    [JsonProperty("m_Facts")]
    public List<Fact>? Facts { get; set; }
}

public class Fact
{
    [JsonProperty("Blueprint")]
    public string? Blueprint { get; set; }

    [JsonProperty("$type")]
    public string? Type { get; set; }

    [JsonProperty("Rank")]
    public int? Rank { get; set; }
}

public class FeaturesCollection
{
    [JsonProperty("m_Facts")]
    public List<Fact>? Facts { get; set; }
}
