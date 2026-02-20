using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class BackgroundCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "backgrounds";
    public override string Endpoint => "backgrounds";
    public override string DisplayName => "Background";
    public override string? Icon => "📜";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine(desc);
            sb.AppendLine();
        }

        var skillProf = GetString(root, "skill_proficiencies");
        if (!string.IsNullOrEmpty(skillProf))
        {
            sb.AppendLine($"**Skill Proficiencies:** {skillProf}");
            sb.AppendLine();
        }

        var equipment = GetString(root, "equipment");
        if (!string.IsNullOrEmpty(equipment))
        {
            sb.AppendLine($"**Equipment:** {equipment}");
            sb.AppendLine();
        }

        var featureName = GetString(root, "feature");
        var featureDesc = GetString(root, "feature_desc");
        if (!string.IsNullOrEmpty(featureName))
        {
            sb.AppendLine($"## {featureName}");
            if (!string.IsNullOrEmpty(featureDesc))
                sb.AppendLine(featureDesc);
        }

        return sb.ToString().Trim();
    }
}
