using static Chronicis.Client.Services.RenderDefinitionHelpers;

namespace Chronicis.Client.Services;

/// <summary>
/// Detects prefix-based field groups and identifies ability score groups.
/// </summary>
public static class PrefixGroupDetector
{
    public static List<PrefixGroup> DetectGroups(List<FieldInfo> fields)
    {
        var candidates = new Dictionary<string, List<FieldInfo>>(StringComparer.OrdinalIgnoreCase);

        foreach (var fi in fields)
        {
            var lastUnderscore = fi.Name.LastIndexOf('_');
            if (lastUnderscore <= 0)
                continue;

            var prefix = fi.Name[..lastUnderscore];
            if (!candidates.ContainsKey(prefix))
                candidates[prefix] = new List<FieldInfo>();
            candidates[prefix].Add(fi);
        }

        var validPrefixes = candidates
            .Where(kv => kv.Value.Count >= 3)
            .ToList();
        validPrefixes.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));

        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groups = new List<PrefixGroup>();

        foreach (var kv in validPrefixes)
        {
            var unclaimed = kv.Value.Where(f => !claimed.Contains(f.Name)).ToList();
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

    public static bool IsAbilityScoreGroup(PrefixGroup group)
    {
        if (group.Fields.Count != 6)
            return false;
        var suffixes = group.Fields
            .Select(f => StripPrefix(f.Name, group.Prefix).ToLowerInvariant())
            .ToHashSet();
        return AbilitySuffixes.All(s => suffixes.Contains(s));
    }
}

/// <summary>
/// A group of fields sharing a common prefix.
/// </summary>
public class PrefixGroup
{
    public string Prefix { get; set; } = "";
    public string Label { get; set; } = "";
    public List<FieldInfo> Fields { get; set; } = new();
}
