using Newtonsoft.Json;

namespace PathfinderSaveParser.Models;

public class PlayerSaveFile
{
    [JsonProperty("Kingdom")]
    public Kingdom? Kingdom { get; set; }

    [JsonProperty("PartyCharacters")]
    public List<CharacterReference>? PartyCharacters { get; set; }

    [JsonProperty("RemoteCompanions")]
    public List<CharacterReference>? RemoteCompanions { get; set; }

    [JsonProperty("m_MainCharacter")]
    public CharacterReference? MainCharacter { get; set; }

    [JsonProperty("Money")]
    public int Money { get; set; }

    [JsonProperty("SharedStash")]
    public ItemCollection? SharedStash { get; set; }

    [JsonProperty("m_GlobalMap")]
    public GlobalMap? GlobalMap { get; set; }
}

public class ItemCollection
{
    [JsonProperty("m_Items")]
    public List<InventoryItem>? Items { get; set; }
}

public class InventoryItem
{
    [JsonProperty("m_Blueprint")]
    public string? Blueprint { get; set; }

    [JsonProperty("m_Count")]
    public int Count { get; set; }

    [JsonProperty("m_Enchantments")]
    public EnchantmentCollection? Enchantments { get; set; }
}

public class EnchantmentCollection
{
    [JsonProperty("m_Facts")]
    public List<EnchantmentReference>? Facts { get; set; }
}

public class EnchantmentReference
{
    [JsonProperty("Blueprint")]
    public string? Blueprint { get; set; }
}

public class CharacterReference
{
    [JsonProperty("m_UniqueId")]
    public string? UniqueId { get; set; }
}

public class Kingdom
{
    [JsonProperty("KingdomName")]
    public string? KingdomName { get; set; }

    [JsonProperty("Stats")]
    public KingdomStatsContainer? Stats { get; set; }

    [JsonProperty("BP")]
    public int BuildPoints { get; set; }

    [JsonProperty("BPPerTurn")]
    public int BuildPointsPerTurn { get; set; }

    [JsonProperty("Alignment")]
    public string? Alignment { get; set; }

    [JsonProperty("Unrest")]
    public string? Unrest { get; set; }

    [JsonProperty("CurrentTurn")]
    public int CurrentTurn { get; set; }

    [JsonProperty("Leaders")]
    public List<KingdomLeader>? Leaders { get; set; }

    [JsonProperty("Regions")]
    public List<KingdomRegion>? Regions { get; set; }
}

public class KingdomLeader
{
    [JsonProperty("Type")]
    public string? Type { get; set; }

    [JsonProperty("LeaderSelection")]
    public LeaderSelection? LeaderSelection { get; set; }

    [JsonProperty("PossibleLeaders")]
    public List<string>? PossibleLeaders { get; set; }
}

public class LeaderSelection
{
    [JsonProperty("m_Blueprint")]
    public string? Blueprint { get; set; }
}

public class KingdomStatsContainer
{
    [JsonProperty("m_Stats")]
    public List<KingdomStat>? Stats { get; set; }
}

public class KingdomStat
{
    [JsonProperty("Type")]
    public string? Type { get; set; }

    [JsonProperty("Rank")]
    public int Rank { get; set; }

    [JsonProperty("Value")]
    public int Value { get; set; }
}

public class GlobalMap
{
    [JsonProperty("Locations")]
    public List<LocationEntry>? Locations { get; set; }
}

public class LocationEntry
{
    [JsonProperty("Key")]
    public string? Key { get; set; }

    [JsonProperty("Value")]
    public LocationData? Value { get; set; }
}

public class LocationData
{
    [JsonProperty("Blueprint")]
    public string? Blueprint { get; set; }

    [JsonProperty("IsExplored")]
    public bool IsExplored { get; set; }

    [JsonProperty("IsSeen")]
    public bool IsSeen { get; set; }

    [JsonProperty("IsRevealed")]
    public bool IsRevealed { get; set; }
}

public class KingdomRegion
{
    [JsonProperty("Blueprint")]
    public string? Blueprint { get; set; }

    [JsonProperty("Settlement")]
    public Settlement? Settlement { get; set; }

    [JsonProperty("IsClaimed")]
    public bool IsClaimed { get; set; }
}

public class Settlement
{
    [JsonProperty("m_Buildings")]
    public BuildingsContainer? Buildings { get; set; }
    public string? Level { get; set; }
    public string? Name { get; set; }
}

public class BuildingsContainer
{
    [JsonProperty("m_Facts")]
    public List<SettlementBuilding>? Facts { get; set; }
}

public class SettlementBuilding
{
    [JsonProperty("Blueprint")]
    public string? Blueprint { get; set; }

    [JsonProperty("IsFinished")]
    public bool IsFinished { get; set; }
}
