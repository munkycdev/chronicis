using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class MonsterCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "monsters";
    public override string Endpoint => "creatures";
    public override string DisplayName => "Monster";
    public override string? Icon => "🐉";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        AppendTypeLine(sb, root);
        AppendStatistics(sb, root);
        AppendNamedArray(sb, root, "actions", "Actions");
        AppendNamedArray(sb, root, "special_abilities", "Special Abilities");
        AppendNamedArray(sb, root, "legendary_actions", "Legendary Actions");

        return sb.ToString().Trim();
    }

    public override string BuildSubtitle(JsonElement item)
    {
        var parts = new List<string> { DisplayName };

        var cr = GetString(item, "challenge_rating");
        var type = GetString(item, "type");
        if (!string.IsNullOrEmpty(cr))
            parts.Add($"CR {cr}");
        if (!string.IsNullOrEmpty(type))
            parts.Add(type);

        return string.Join(" • ", parts);
    }

    private static void AppendTypeLine(StringBuilder sb, JsonElement root)
    {
        var size = GetStringFromObject(root, "size", "name") ?? GetString(root, "size");
        var type = GetStringFromObject(root, "type", "name") ?? GetString(root, "type");
        var alignment = GetString(root, "alignment");
        var typeLine = string.Join(" ", new[] { size, type }.Where(s => !string.IsNullOrEmpty(s)));
        if (!string.IsNullOrEmpty(alignment))
            typeLine += $", {alignment}";
        if (!string.IsNullOrEmpty(typeLine))
        {
            sb.AppendLine($"*{typeLine}*");
            sb.AppendLine();
        }
    }

    private static void AppendStatistics(StringBuilder sb, JsonElement root)
    {
        sb.AppendLine("## Statistics");
        var ac = GetString(root, "armor_class");
        var hp = GetString(root, "hit_points");
        var hitDice = GetString(root, "hit_dice");
        var cr = GetString(root, "challenge_rating") ?? GetString(root, "cr");

        if (!string.IsNullOrEmpty(ac))
            sb.AppendLine($"**Armor Class:** {ac}");
        if (!string.IsNullOrEmpty(hp))
            sb.AppendLine($"**Hit Points:** {hp}{(!string.IsNullOrEmpty(hitDice) ? $" ({hitDice})" : "")}");
        if (!string.IsNullOrEmpty(cr))
            sb.AppendLine($"**Challenge:** {cr}");

        var speed = GetSpeedString(root);
        if (!string.IsNullOrEmpty(speed))
            sb.AppendLine($"**Speed:** {speed}");

        sb.AppendLine();
    }
}
