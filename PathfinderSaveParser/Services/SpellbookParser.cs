using Newtonsoft.Json.Linq;
using System.Text;
using PathfinderSaveParser.Models;

namespace PathfinderSaveParser.Services;

/// <summary>
/// Parses spellbook information from character data
/// </summary>
public class SpellbookParser
{
    private readonly BlueprintLookupService _blueprintLookup;
    private readonly RefResolver _resolver;
    private readonly ReportOptions _options;

    public SpellbookParser(BlueprintLookupService blueprintLookup, RefResolver resolver, ReportOptions options)
    {
        _blueprintLookup = blueprintLookup;
        _resolver = resolver;
        _options = options;
    }

    public string ParseSpellbooks(JToken? descriptor)
    {
        if (descriptor == null) return "";

        var spellbooks = descriptor["m_Spellbooks"];
        if (spellbooks == null || !spellbooks.HasValues) return "";

        var sb = new StringBuilder();
        sb.AppendLine("SPELLCASTING");
        sb.AppendLine(new string('=', 80));

        foreach (var spellbookEntry in spellbooks)
        {
            var spellbookData = spellbookEntry["Value"];
            if (spellbookData == null) continue;

            var resolved = _resolver.Resolve(spellbookData);
            if (resolved == null) continue;

            ParseSingleSpellbook(resolved, sb);
        }

        return sb.ToString();
    }

    private void ParseSingleSpellbook(JToken spellbook, StringBuilder sb)
    {
        var blueprintId = spellbook["Blueprint"]?.ToString();
        if (string.IsNullOrEmpty(blueprintId)) return;

        var spellbookName = _blueprintLookup.GetName(blueprintId);
        var casterLevel = spellbook["m_CasterLevelInternal"]?.Value<int>() ?? 0;

        sb.AppendLine();
        sb.AppendLine($"{spellbookName} (Caster Level {casterLevel})");
        sb.AppendLine(new string('-', 60));

        // Parse spell slots
        var isSpontaneous = HasSpontaneousSlots(spellbook);
        
        if (isSpontaneous)
        {
            ParseSpontaneousSlots(spellbook, sb);
        }
        else
        {
            ParsePreparedSlots(spellbook, sb);
        }

        // Parse known spells
        ParseKnownSpells(spellbook, sb);
    }

    private bool HasSpontaneousSlots(JToken spellbook)
    {
        var spontaneousSlots = spellbook["m_SpontaneousSlots"];
        if (spontaneousSlots == null || !spontaneousSlots.HasValues) return false;

        // Check if any spontaneous slot count is > 0
        foreach (var slot in spontaneousSlots)
        {
            if (slot.Value<int>() > 0) return true;
        }

        return false;
    }

    private void ParseSpontaneousSlots(JToken spellbook, StringBuilder sb)
    {
        var spontaneousSlots = spellbook["m_SpontaneousSlots"];
        if (spontaneousSlots == null) return;

        sb.AppendLine("Spell Slots per Day:");
        
        var slots = spontaneousSlots.Select((s, i) => new { Level = i, Count = s.Value<int>() })
                                    .Where(x => x.Count > 0)
                                    .ToList();

        if (slots.Any())
        {
            foreach (var slot in slots)
            {
                sb.AppendLine($"  Level {slot.Level}: {slot.Count} slot{(slot.Count != 1 ? "s" : "")}");
            }
        }
        else
        {
            sb.AppendLine("  (No spell slots available)");
        }
    }

    private void ParsePreparedSlots(JToken spellbook, StringBuilder sb)
    {
        var memorizedSpells = spellbook["m_MemorizedSpells"];
        if (memorizedSpells == null) return;

        sb.AppendLine("Spell Slots per Day:");

        var slotCounts = new List<(int Level, int Count)>();
        
        int level = 0;
        foreach (var levelSlots in memorizedSpells)
        {
            if (levelSlots.HasValues)
            {
                int count = levelSlots.Count();
                if (count > 0)
                {
                    slotCounts.Add((level, count));
                }
            }
            level++;
        }

        if (slotCounts.Any())
        {
            foreach (var (slotLevel, count) in slotCounts)
            {
                sb.AppendLine($"  Level {slotLevel}: {count} slot{(count != 1 ? "s" : "")}");
            }
        }
        else
        {
            sb.AppendLine("  (No spell slots available)");
        }
    }

    private void ParseKnownSpells(JToken spellbook, StringBuilder sb)
    {
        var knownSpells = spellbook["m_KnownSpells"];
        if (knownSpells == null || !knownSpells.HasValues) return;

        sb.AppendLine();
        sb.AppendLine("Known Spells:");

        int level = 0;
        bool hasAnySpells = false;

        foreach (var levelSpells in knownSpells)
        {
            if (levelSpells.HasValues)
            {
                var spells = new List<string>();
                
                foreach (var spell in levelSpells)
                {
                    var spellBlueprint = spell["Blueprint"]?.ToString();
                    if (!string.IsNullOrEmpty(spellBlueprint))
                    {
                        var spellName = _blueprintLookup.GetName(spellBlueprint);
                        if (!string.IsNullOrEmpty(spellName) && 
                            !spellName.StartsWith("Blueprint_") && 
                            spellName != "None")
                        {
                            spells.Add(spellName);
                        }
                    }
                }

                if (spells.Any())
                {
                    hasAnySpells = true;
                    sb.AppendLine($"  Level {level}: {string.Join(", ", spells)}");
                }
            }
            level++;
        }

        if (!hasAnySpells)
        {
            sb.AppendLine("  (No spells known)");
        }
    }
}
