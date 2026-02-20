using System.Text;
using System.Text.Json;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public sealed class RaceCategoryStrategy : Open5eCategoryStrategyBase
{
    public override string CategoryKey => "races";
    public override string Endpoint => "races";
    public override string DisplayName => "Race";
    public override string? Icon => "👤";

    public override string BuildMarkdown(JsonElement root, string title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var size = GetString(root, "size");
        var speed = GetString(root, "speed");

        if (!string.IsNullOrEmpty(size))
            sb.AppendLine($"**Size:** {size}");
        if (!string.IsNullOrEmpty(speed))
            sb.AppendLine($"**Speed:** {speed}");

        sb.AppendLine();

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine(desc);
            sb.AppendLine();
        }

        var traits = GetString(root, "traits");
        if (!string.IsNullOrEmpty(traits))
        {
            sb.AppendLine("## Traits");
            sb.AppendLine(traits);
        }

        return sb.ToString().Trim();
    }
}
