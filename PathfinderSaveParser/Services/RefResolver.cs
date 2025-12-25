using Newtonsoft.Json.Linq;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Resolves $ref pointers in Pathfinder save files by indexing all $id fields
/// </summary>
public class RefResolver
{
    private readonly Dictionary<string, JToken> _index = new();

    public RefResolver(JToken root)
    {
        IndexToken(root);
    }

    private void IndexToken(JToken token)
    {
        if (token is JObject obj)
        {
            if (obj.ContainsKey("$id"))
            {
                string id = obj["$id"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(id) && !_index.ContainsKey(id))
                {
                    _index[id] = obj;
                }
            }
            foreach (var property in obj.Properties())
            {
                IndexToken(property.Value);
            }
        }
        else if (token is JArray arr)
        {
            foreach (var item in arr)
            {
                IndexToken(item);
            }
        }
    }

    public JToken? Resolve(JToken? token)
    {
        if (token is JObject obj && obj.ContainsKey("$ref"))
        {
            string refId = obj["$ref"]?.ToString() ?? "";
            return _index.ContainsKey(refId) ? _index[refId] : null;
        }
        return token;
    }

    public T? GetValue<T>(JToken? token, string propertyName)
    {
        var resolved = Resolve(token);
        if (resolved == null) return default;
        
        var property = resolved[propertyName];
        if (property == null) return default;
        
        return property.ToObject<T>();
    }

    public JToken? GetProperty(JToken? token, string propertyName)
    {
        var resolved = Resolve(token);
        return resolved?[propertyName];
    }
}
