using PathfinderSaveParser.Models;
using System.Text;

namespace PathfinderSaveParser.Services;

public class KingdomStatsParser
{
    private readonly BlueprintLookupService _blueprintLookup;
    private readonly ReportOptions _options;

    public KingdomStatsParser(BlueprintLookupService blueprintLookup, ReportOptions options)
    {
        _blueprintLookup = blueprintLookup;
        _options = options;
    }

    public string ParseKingdomStats(Kingdom kingdom)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("=== KINGDOM STATISTICS ===");
        sb.AppendLine();
        sb.AppendLine($"Kingdom Name: {kingdom.KingdomName}");
        sb.AppendLine($"Alignment: {kingdom.Alignment}");
        sb.AppendLine($"Unrest Level: {kingdom.Unrest}");
        sb.AppendLine($"Build Points: {kingdom.BuildPoints} (Per Turn: {kingdom.CurrentTurn})");
        sb.AppendLine();
        sb.AppendLine("Kingdom Stats:");
        sb.AppendLine("".PadRight(60, '-'));
        sb.AppendLine($"{"Stat",-20} {"Value",-15} {"Rank",-10}");
        sb.AppendLine("".PadRight(60, '-'));

        if (kingdom.Stats?.Stats != null)
        {
            foreach (var stat in kingdom.Stats.Stats)
            {
                sb.AppendLine($"{stat.Type,-20} {stat.Value,-15} {stat.Rank,-10}");
            }
        }

        sb.AppendLine("".PadRight(60, '-'));
        sb.AppendLine();

        // Kingdom Advisors
        if (_options.IncludeKingdomAdvisors && kingdom.Leaders != null && kingdom.Leaders.Any())
        {
            sb.AppendLine("Kingdom Advisors:");
            sb.AppendLine("".PadRight(60, '-'));
            sb.AppendLine($"{"Position",-25} {"Advisor",-35}");
            sb.AppendLine("".PadRight(60, '-'));

            foreach (var leader in kingdom.Leaders)
            {
                // Add spaces to position names (e.g., GrandDiplomat -> Grand Diplomat)
                string position = leader.Type ?? "Unknown";
                position = System.Text.RegularExpressions.Regex.Replace(position, "([a-z])([A-Z])", "$1 $2");
                
                // Rename Spymaster to Minister
                if (position == "Spymaster") position = "Minister";
                
                string advisor;
                
                if (leader.LeaderSelection?.Blueprint != null)
                {
                    // Position is filled
                    advisor = _blueprintLookup.GetName(leader.LeaderSelection.Blueprint);
                }
                else if (leader.PossibleLeaders != null && leader.PossibleLeaders.Any() && IsPositionAvailable(leader.Type, kingdom))
                {
                    // Position is unlocked and requirements are met
                    advisor = "(Vacant)";
                }
                else
                {
                    // Position is locked (requirements not met)
                    advisor = "(Locked)";
                }

                sb.AppendLine($"{position,-25} {advisor,-35}");
            }

            sb.AppendLine("".PadRight(60, '-'));
        }
        
        return sb.ToString();
    }

    private bool IsPositionAvailable(string? positionType, Kingdom kingdom)
    {
        if (positionType == null || kingdom.Stats?.Stats == null)
            return false;

        // Get the required stat for each position
        var requirements = new Dictionary<string, (string StatType, int RequiredRank, int RequiredValue)>
        {
            { "GrandDiplomat", ("Community", 3, 60) },
            { "Warden", ("Military", 3, 60) },
            { "Magister", ("Arcane", 3, 60) },
            { "Curator", ("Loyalty", 3, 60) },
            { "Spymaster", ("Relations", 3, 60) }
        };

        // Always-available positions
        if (!requirements.ContainsKey(positionType))
            return true;

        var (statType, requiredRank, requiredValue) = requirements[positionType];
        var stat = kingdom.Stats.Stats.FirstOrDefault(s => s.Type == statType);

        if (stat == null)
            return false;

        return stat.Rank >= requiredRank && stat.Value >= requiredValue;
    }

    public Dictionary<string, (int Value, int Rank)> GetKingdomStatsDict(Kingdom kingdom)
    {
        var stats = new Dictionary<string, (int Value, int Rank)>();
        
        if (kingdom.Stats?.Stats != null)
        {
            foreach (var stat in kingdom.Stats.Stats)
            {
                if (stat.Type != null)
                {
                    stats[stat.Type] = (stat.Value, stat.Rank);
                }
            }
        }

        return stats;
    }
}
