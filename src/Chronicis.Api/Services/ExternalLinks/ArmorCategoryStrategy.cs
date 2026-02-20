using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class ArmorCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "armor";
    public override string Endpoint => "armor";
    public override string DisplayName => "Armor";
    public override string? Icon => "🛡️";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var category = GetString(root, "category");
        if (!string.IsNullOrEmpty(category))
        {
            sb.AppendLine($"*{category}*");
            sb.AppendLine();
        }

        var ac = GetString(root, "base_ac") ?? GetString(root, "ac_string");
        var cost = GetString(root, "cost");
        var weight = GetString(root, "weight");
        var strength = GetString(root, "strength_requirement");
        var stealth = GetString(root, "stealth_disadvantage");

        if (!string.IsNullOrEmpty(ac))
            sb.AppendLine($"**Armor Class:** {ac}");
        if (!string.IsNullOrEmpty(cost))
            sb.AppendLine($"**Cost:** {cost}");
        if (!string.IsNullOrEmpty(weight))
            sb.AppendLine($"**Weight:** {weight}");
        if (!string.IsNullOrEmpty(strength))
            sb.AppendLine($"**Strength Required:** {strength}");
        if (stealth == "true" || stealth == "True")
            sb.AppendLine("**Stealth:** Disadvantage");

        return sb.ToString().Trim();
    }

    public override string BuildSubtitle(JsonElement item)
    {
        var parts = new List<string> { DisplayName };
        var categoryRange = GetString(item, "category_range") ?? GetString(item, "category");
        if (!string.IsNullOrEmpty(categoryRange))
            parts.Add(categoryRange);
        return string.Join(" • ", parts);
    }
}
