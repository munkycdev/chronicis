using System.Text.Json;
using Chronicis.Client.Models;

namespace Chronicis.Client.Services;

/// <summary>
/// Generates a starter RenderDefinition from a sample JSON record.
/// Uses heuristics to group fields, detect ability scores, and choose render hints.
/// The output is a starting point for manual refinement.
/// </summary>
public static class RenderDefinitionGeneratorService
{
    // Well-known metadata fields to hide
    private static readonly HashSet<string> HiddenFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "pk", "model", "document", "illustration", "url", "key", "slug",
        "hover", "v2_converted_path", "img_main", "document__slug",
        "document__title", "document__license_url", "document__url",
        "page_no", "spell_list", "environments"
    };

    private static readonly string[] TitleCandidates = { "name", "title" };

    private static readonly string[] AbilitySuffixes =
        { "strength", "dexterity", "constitution", "intelligence", "wisdom", "charisma" };

    private static readonly string[] AbilityLabels =
        { "STR", "DEX", "CON", "INT", "WIS", "CHA" };

    public static RenderDefinition Generate(JsonElement sample)
    {
        var dataSource = sample;
        if (sample.ValueKind == JsonValueKind.Object &&
            sample.TryGetProperty("fields", out var fields) &&
            fields.ValueKind == JsonValueKind.Object)
        {
            dataSource = fields;
        }

        if (dataSource.ValueKind != JsonValueKind.Object)
            return CreateMinimal();

        var fieldInfos = new List<FieldInfo>();
        foreach (var prop in dataSource.EnumerateObject())
        {
            fieldInfos.Add(new FieldInfo
            {
                Name = prop.Name,
                Value = prop.Value,
                Kind = prop.Value.ValueKind,
                IsNull = IsNullOrEmpty(prop.Value),
                IsComplex = prop.Value.ValueKind == JsonValueKind.Object ||
                            prop.Value.ValueKind == JsonValueKind.Array
            });
        }

        var titleField = fieldInfos
            .FirstOrDefault(f => TitleCandidates.Contains(f.Name, StringComparer.OrdinalIgnoreCase))
            ?.Name ?? "name";

        var hidden = new List<string>();
        var remaining = new List<FieldInfo>();

        foreach (var fi in fieldInfos)
        {
            if (fi.Name.Equals(titleField, StringComparison.OrdinalIgnoreCase))
                continue;
            if (HiddenFields.Contains(fi.Name))
                hidden.Add(fi.Name);
            else
                remaining.Add(fi);
        }

        var prefixGroups = DetectPrefixGroups(remaining);
        var groupedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in prefixGroups)
            foreach (var fi in group.Fields)
                groupedNames.Add(fi.Name);

        var ungrouped = remaining.Where(f => !groupedNames.Contains(f.Name)).ToList();
        var sections = new List<RenderSection>();

        // Overview section
        var overviewFields = ungrouped
            .Where(f => !f.IsComplex)
            .OrderBy(f => IsDescriptionField(f.Name) ? 1 : 0)
            .ToList();

        if (overviewFields.Count > 0)
        {
            sections.Add(new RenderSection
            {
                Label = "Overview",
                Render = "fields",
                Fields = overviewFields.Select(f => new RenderField
                {
                    Path = f.Name,
                    Label = FormatFieldName(f.Name),
                    Render = IsDescriptionField(f.Name) ? "richtext" : "text"
                }).ToList()
            });
        }

        // Prefix-grouped sections
        foreach (var group in prefixGroups.OrderBy(g => g.Label))
        {
            var isAbilityScores = IsAbilityScoreGroup(group);
            var allNull = group.Fields.All(f => f.IsNull);
            var mostlyNull = group.Fields.Count(f => f.IsNull) > group.Fields.Count / 2;

            if (isAbilityScores)
            {
                // Stat-row rendering for ability scores
                sections.Add(new RenderSection
                {
                    Label = "Ability Scores",
                    Render = "stat-row",
                    Fields = AbilitySuffixes.Select((suffix, i) =>
                    {
                        var match = group.Fields.FirstOrDefault(f =>
                            f.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
                        return new RenderField
                        {
                            Path = match?.Name ?? $"ability_score_{suffix}",
                            Label = AbilityLabels[i]
                        };
                    }).ToList()
                });
            }
            else
            {
                sections.Add(new RenderSection
                {
                    Label = FormatGroupLabel(group.Prefix),
                    Render = "fields",
                    Collapsed = mostlyNull,
                    Fields = group.Fields
                        .OrderBy(f => f.IsNull ? 1 : 0)
                        .Select(f => new RenderField
                        {
                            Path = f.Name,
                            Label = FormatFieldName(StripPrefix(f.Name, group.Prefix))
                        }).ToList()
                });
            }
        }

        // Complex fields section (arrays/objects)
        var complexFields = ungrouped.Where(f => f.IsComplex).ToList();
        if (complexFields.Count > 0)
        {
            sections.Add(new RenderSection
            {
                Label = "Additional Data",
                Render = "fields",
                Collapsed = true,
                Fields = complexFields.Select(f => new RenderField
                {
                    Path = f.Name,
                    Label = FormatFieldName(f.Name)
                }).ToList()
            });
        }

        return new RenderDefinition
        {
            TitleField = titleField,
            CatchAll = true,
            Hidden = hidden,
            Sections = sections
        };
    }

    // --- Heuristic helpers ---

    private static List<PrefixGroup> DetectPrefixGroups(List<FieldInfo> fields)
    {
        var groups = new List<PrefixGroup>();
        var candidates = new Dictionary<string, List<FieldInfo>>(StringComparer.OrdinalIgnoreCase);

        foreach (var fi in fields)
        {
            var lastUnderscore = fi.Name.LastIndexOf('_');
            if (lastUnderscore <= 0)
                continue;

            // Try progressively shorter prefixes
            var prefix = fi.Name[..lastUnderscore];
            // Normalize: for multi-segment like "ability_score", try that first
            if (!candidates.ContainsKey(prefix))
                candidates[prefix] = new List<FieldInfo>();
            candidates[prefix].Add(fi);
        }

        // Only keep groups with 3+ fields (meaningful grouping)
        // Also consolidate nested prefixes: prefer "ability_score" over "ability"
        var validPrefixes = candidates
            .Where(kv => kv.Value.Count >= 3)
            .OrderByDescending(kv => kv.Key.Length)
            .ToList();

        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in validPrefixes)
        {
            var unclaimed = kv.Value.Where(f => !claimed.Contains(f.Name)).ToList();
            if (unclaimed.Count < 3)
                continue;

            groups.Add(new PrefixGroup
            {
                Prefix = kv.Key,
                Label = FormatGroupLabel(kv.Key),
                Fields = unclaimed
            });
            foreach (var f in unclaimed)
                claimed.Add(f.Name);
        }

        return groups;
    }

    private static bool IsAbilityScoreGroup(PrefixGroup group)
    {
        if (group.Fields.Count != 6)
            return false;
        var suffixes = group.Fields
            .Select(f => StripPrefix(f.Name, group.Prefix).ToLowerInvariant())
            .ToHashSet();
        return AbilitySuffixes.All(s => suffixes.Contains(s));
    }

    private static bool IsNullOrEmpty(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => true,
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ||
                                    value.GetString() == "—" || value.GetString() == "-",
            JsonValueKind.Array => value.GetArrayLength() == 0,
            _ => false
        };
    }

    private static bool IsDescriptionField(string name) =>
        name.Contains("description", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("desc", StringComparison.OrdinalIgnoreCase);

    private static string FormatFieldName(string name)
    {
        // "ability_score_strength" -> "Strength", "hit_points" -> "Hit Points"
        return string.Join(' ', name
            .Split('_')
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => char.ToUpperInvariant(s[0]) + s[1..]));
    }

    private static string FormatGroupLabel(string prefix)
    {
        // "saving_throw" -> "Saving Throws", "skill_bonus" -> "Skill Bonuses"
        var label = FormatFieldName(prefix);
        // Simple pluralization
        if (!label.EndsWith('s') && !label.EndsWith("es"))
            label += "s";
        return label;
    }

    private static string StripPrefix(string name, string prefix)
    {
        if (name.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase))
            return name[(prefix.Length + 1)..];
        return name;
    }

    private static RenderDefinition CreateMinimal() => new()
    {
        CatchAll = true,
        Sections = new List<RenderSection>()
    };

    // --- Internal types ---

    private class FieldInfo
    {
        public string Name { get; set; } = "";
        public JsonElement Value { get; set; }
        public JsonValueKind Kind { get; set; }
        public bool IsNull { get; set; }
        public bool IsComplex { get; set; }
    }

    private class PrefixGroup
    {
        public string Prefix { get; set; } = "";
        public string Label { get; set; } = "";
        public List<FieldInfo> Fields { get; set; } = new();
    }
}
