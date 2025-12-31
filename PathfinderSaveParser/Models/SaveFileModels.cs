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
