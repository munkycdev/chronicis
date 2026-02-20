using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class SpellCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "spells";
    public override string Endpoint => "spells";
    public override string DisplayName => "Spell";
    public override string? Icon => "✨";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        AppendLevelAndSchool(sb, root);
        AppendCastingInfo(sb, root);
        AppendComponents(sb, root);
        sb.AppendLine();
        AppendDescription(sb, root);
        AppendHigherLevels(sb, root);

        return sb.ToString().Trim();
    }

    public override string BuildSubtitle(JsonElement item)
    {
        var parts = new List<string> { DisplayName };

        var level = GetInt(item, "level");
        var school = GetStringFromObject(item, "school", "name");
        if (level.HasValue)
        {
            parts.Add(level == 0 ? "Cantrip" : $"Level {level}");
        }
        if (!string.IsNullOrEmpty(school))
        {
            parts.Add(school);
        }

        return string.Join(" • ", parts);
    }

    private static void AppendLevelAndSchool(StringBuilder sb, JsonElement root)
    {
        var level = GetInt(root, "level");
        var school = GetStringFromObject(root, "school", "name");
        var levelText = level == 0 ? "Cantrip" : $"Level {level}";

        sb.AppendLine(!string.IsNullOrEmpty(school) ? $"*{levelText} {school}*" : $"*{levelText}*");
        sb.AppendLine();
    }

    private static void AppendCastingInfo(StringBuilder sb, JsonElement root)
    {
        var castingTime = GetString(root, "casting_time");
        var range = GetString(root, "range_text") ?? GetString(root, "range")?.ToString();
        var duration = GetString(root, "duration");
        var concentration = GetBool(root, "concentration");
        var ritual = GetBool(root, "ritual");

        sb.AppendLine("## Casting");
        if (!string.IsNullOrEmpty(castingTime))
            sb.AppendLine($"- **Casting Time:** {castingTime}{(ritual == true ? " (ritual)" : "")}");
        if (!string.IsNullOrEmpty(range))
            sb.AppendLine($"- **Range:** {range}");
        if (!string.IsNullOrEmpty(duration))
            sb.AppendLine($"- **Duration:** {(concentration == true ? "Concentration, " : "")}{duration}");
    }

    private static void AppendComponents(StringBuilder sb, JsonElement root)
    {
        var verbal = GetBool(root, "verbal");
        var somatic = GetBool(root, "somatic");
        var material = GetBool(root, "material");
        var materialDesc = GetString(root, "material_specified");

        var components = new List<string>();
        if (verbal == true)
            components.Add("V");
        if (somatic == true)
            components.Add("S");
        if (material == true)
            components.Add($"M{(!string.IsNullOrEmpty(materialDesc) ? $" ({materialDesc})" : "")}");
        if (components.Count > 0)
            sb.AppendLine($"- **Components:** {string.Join(", ", components)}");
    }

    private static void AppendDescription(StringBuilder sb, JsonElement root)
    {
        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine("## Description");
            sb.AppendLine(desc);
            sb.AppendLine();
        }
    }

    private static void AppendHigherLevels(StringBuilder sb, JsonElement root)
    {
        var higherLevel = GetString(root, "higher_level");
        if (!string.IsNullOrEmpty(higherLevel))
        {
            sb.AppendLine("## At Higher Levels");
            sb.AppendLine(higherLevel);
        }
    }
}
