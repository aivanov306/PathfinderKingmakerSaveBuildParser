using Newtonsoft.Json.Linq;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Utility to help diagnose and explore save file structure
/// </summary>
public class SaveFileInspector
{
    public void InspectJsonStructure(string jsonPath, string outputPath, int maxDepth = 3)
    {
        try
        {
            var json = File.ReadAllText(jsonPath);
            var jObject = JToken.Parse(json);
            
            using var writer = new StreamWriter(outputPath);
            writer.WriteLine($"=== JSON Structure Analysis: {Path.GetFileName(jsonPath)} ===");
            writer.WriteLine();
            
            InspectToken(jObject, writer, 0, maxDepth);
            
            writer.WriteLine();
            writer.WriteLine("=== Analysis Complete ===");
        }
        catch (Exception ex)
        {
            using var writer = new StreamWriter(outputPath);
            writer.WriteLine($"Error inspecting file: {ex.Message}");
        }
    }

    private void InspectToken(JToken token, StreamWriter writer, int depth, int maxDepth)
    {
        var indent = new string(' ', depth * 2);
        
        if (depth > maxDepth)
        {
            writer.WriteLine($"{indent}[... max depth reached ...]");
            return;
        }

        switch (token.Type)
        {
            case JTokenType.Object:
                var obj = (JObject)token;
                writer.WriteLine($"{indent}Object with {obj.Count} properties:");
                foreach (var prop in obj.Properties().Take(20)) // Limit to first 20 properties
                {
                    writer.WriteLine($"{indent}  - {prop.Name} ({prop.Value.Type})");
                    
                    if (prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array)
                    {
                        InspectToken(prop.Value, writer, depth + 2, maxDepth);
                    }
                }
                if (obj.Count > 20)
                {
                    writer.WriteLine($"{indent}  ... and {obj.Count - 20} more properties");
                }
                break;

            case JTokenType.Array:
                var arr = (JArray)token;
                writer.WriteLine($"{indent}Array with {arr.Count} items");
                if (arr.Count > 0 && depth < maxDepth)
                {
                    writer.WriteLine($"{indent}  First item:");
                    InspectToken(arr[0], writer, depth + 2, maxDepth);
                }
                break;

            default:
                var preview = token.ToString();
                if (preview.Length > 100)
                    preview = preview.Substring(0, 100) + "...";
                writer.WriteLine($"{indent}Value: {preview}");
                break;
        }
    }

    public Dictionary<string, object> GetFileStats(string jsonPath)
    {
        var stats = new Dictionary<string, object>();
        
        try
        {
            var fileInfo = new FileInfo(jsonPath);
            stats["FilePath"] = jsonPath;
            stats["FileSize"] = $"{fileInfo.Length / 1024.0:F2} KB";
            stats["Exists"] = fileInfo.Exists;
            
            if (fileInfo.Exists && fileInfo.Length > 0)
            {
                var json = File.ReadAllText(jsonPath);
                var jObject = JToken.Parse(json);
                
                stats["TokenType"] = jObject.Type.ToString();
                
                if (jObject is JObject obj)
                {
                    stats["PropertyCount"] = obj.Count;
                    stats["Properties"] = string.Join(", ", obj.Properties().Select(p => p.Name).Take(10));
                }
                else if (jObject is JArray arr)
                {
                    stats["ArrayLength"] = arr.Count;
                }
            }
        }
        catch (Exception ex)
        {
            stats["Error"] = ex.Message;
        }
        
        return stats;
    }
}
