using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class MagicItemCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "magicitems";
    public override string Endpoint => "items";
    public override string DisplayName => "Magic Item";
    public override string? Icon => "💎";
    public override string WebCategory => "magic-items";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var type = GetString(root, "type");
        var rarity = GetString(root, "rarity");
        var attunement = GetString(root, "requires_attunement");

        var subtitle = string.Join(", ", new[] { type, rarity }.Where(s => !string.IsNullOrEmpty(s)));
        if (!string.IsNullOrEmpty(attunement) && attunement.ToLower() != "false")
            subtitle += $" ({attunement})";

        if (!string.IsNullOrEmpty(subtitle))
        {
            sb.AppendLine($"*{subtitle}*");
            sb.AppendLine();
        }

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
            sb.AppendLine(desc);

        return sb.ToString().Trim();
    }

    public override string BuildSubtitle(JsonElement item)
    {
        var parts = new List<string> { DisplayName };

        var rarity = GetString(item, "rarity");
        var itemType = GetString(item, "type");
        if (!string.IsNullOrEmpty(rarity))
            parts.Add(rarity);
        if (!string.IsNullOrEmpty(itemType))
            parts.Add(itemType);

        return string.Join(" • ", parts);
    }
}
