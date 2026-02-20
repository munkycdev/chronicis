using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class WeaponCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "weapons";
    public override string Endpoint => "weapons";
    public override string DisplayName => "Weapon";
    public override string? Icon => "🗡️";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var category = GetString(root, "category") ?? GetString(root, "category_range");
        if (!string.IsNullOrEmpty(category))
        {
            sb.AppendLine($"*{category}*");
            sb.AppendLine();
        }

        var damage = GetString(root, "damage_dice") ?? GetString(root, "damage");
        var damageType = GetString(root, "damage_type");
        var cost = GetString(root, "cost");
        var weight = GetString(root, "weight");

        if (!string.IsNullOrEmpty(damage))
            sb.AppendLine($"**Damage:** {damage}{(!string.IsNullOrEmpty(damageType) ? $" {damageType}" : "")}");
        if (!string.IsNullOrEmpty(cost))
            sb.AppendLine($"**Cost:** {cost}");
        if (!string.IsNullOrEmpty(weight))
            sb.AppendLine($"**Weight:** {weight}");

        var properties = GetStringArray(root, "properties");
        if (properties.Count > 0)
            sb.AppendLine($"**Properties:** {string.Join(", ", properties)}");

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
