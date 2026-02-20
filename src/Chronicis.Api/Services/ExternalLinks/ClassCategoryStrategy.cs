using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class ClassCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "classes";
    public override string Endpoint => "classes";
    public override string DisplayName => "Class";
    public override string? Icon => "⚔️";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var hitDie = GetString(root, "hit_dice");
        if (!string.IsNullOrEmpty(hitDie))
        {
            sb.AppendLine($"**Hit Die:** {hitDie}");
            sb.AppendLine();
        }

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
            sb.AppendLine(desc);

        return sb.ToString().Trim();
    }
}
