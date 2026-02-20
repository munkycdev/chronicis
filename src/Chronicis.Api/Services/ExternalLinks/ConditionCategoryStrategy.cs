using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class ConditionCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "conditions";
    public override string Endpoint => "conditions";
    public override string DisplayName => "Condition";
    public override string? Icon => "⚡";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
            sb.AppendLine(desc);

        return sb.ToString().Trim();
    }
}
