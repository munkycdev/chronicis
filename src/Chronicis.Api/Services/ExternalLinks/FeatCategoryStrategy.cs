using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class FeatCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "feats";
    public override string Endpoint => "feats";
    public override string DisplayName => "Feat";
    public override string? Icon => "⭐";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var prerequisite = GetString(root, "prerequisite");
        if (!string.IsNullOrEmpty(prerequisite))
        {
            sb.AppendLine($"*Prerequisite: {prerequisite}*");
            sb.AppendLine();
        }

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
            sb.AppendLine(desc);

        return sb.ToString().Trim();
    }
}
