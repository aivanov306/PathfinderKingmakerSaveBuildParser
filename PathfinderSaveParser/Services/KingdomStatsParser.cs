using PathfinderSaveParser.Models;
using System.Text;

namespace PathfinderSaveParser.Services;

public class KingdomStatsParser
{
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
        
        return sb.ToString();
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
