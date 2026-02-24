using System.Text;
using Chronicis.Shared.Models;

namespace Chronicis.Api.Services;

public partial class ExportService
{
    internal string BuildArticleMarkdown(Article article)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(article.Title)}\"");
        sb.AppendLine($"type: {article.Type}");
        sb.AppendLine($"visibility: {article.Visibility}");
        sb.AppendLine($"created: {article.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        if (article.ModifiedAt.HasValue)
            sb.AppendLine($"modified: {article.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
        if (article.SessionDate.HasValue)
            sb.AppendLine($"session_date: {article.SessionDate:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(article.InGameDate))
            sb.AppendLine($"in_game_date: \"{EscapeYaml(article.InGameDate)}\"");
        if (!string.IsNullOrEmpty(article.IconEmoji))
            sb.AppendLine($"icon: \"{article.IconEmoji}\"");
        sb.AppendLine("---");
        sb.AppendLine();

        sb.AppendLine($"# {article.Title}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(article.Body))
        {
            sb.AppendLine(HtmlToMarkdownConverter.Convert(article.Body));
        }

        AppendAISummary(sb, article);

        return sb.ToString();
    }

    private static void AppendAISummary(StringBuilder sb, Article article)
    {
        if (string.IsNullOrEmpty(article.AISummary))
            return;

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## AI Summary");
        sb.AppendLine();
        sb.AppendLine(article.AISummary);

        if (article.AISummaryGeneratedAt.HasValue)
        {
            sb.AppendLine();
            sb.AppendLine($"*Generated: {article.AISummaryGeneratedAt:yyyy-MM-dd HH:mm:ss}*");
        }
    }

    internal string BuildCampaignMarkdown(Campaign campaign)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(campaign.Name)}\"");
        sb.AppendLine("type: Campaign");
        sb.AppendLine($"created: {campaign.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {campaign.Name}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(campaign.Description))
            sb.AppendLine(campaign.Description);

        return sb.ToString();
    }

    internal string BuildSessionMarkdown(Session session)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(session.Name)}\"");
        sb.AppendLine("type: Session");
        sb.AppendLine($"created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        if (session.ModifiedAt.HasValue)
        {
            sb.AppendLine($"modified: {session.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
        }

        if (session.SessionDate.HasValue)
        {
            sb.AppendLine($"session_date: {session.SessionDate:yyyy-MM-dd}");
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {session.Name}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(session.PublicNotes))
        {
            sb.AppendLine(HtmlToMarkdownConverter.Convert(session.PublicNotes));
        }

        AppendSessionAiSummary(sb, session);

        return sb.ToString();
    }

    private static void AppendSessionAiSummary(StringBuilder sb, Session session)
    {
        if (string.IsNullOrEmpty(session.AiSummary))
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## AI Summary");
        sb.AppendLine();
        sb.AppendLine(session.AiSummary);

        if (session.AiSummaryGeneratedAt.HasValue)
        {
            sb.AppendLine();
            sb.AppendLine($"*Generated: {session.AiSummaryGeneratedAt:yyyy-MM-dd HH:mm:ss}*");
        }
    }

    internal string BuildArcMarkdown(Arc arc)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(arc.Name)}\"");
        sb.AppendLine("type: Arc");
        sb.AppendLine($"sort_order: {arc.SortOrder}");
        sb.AppendLine($"created: {arc.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {arc.Name}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(arc.Description))
            sb.AppendLine(arc.Description);

        return sb.ToString();
    }
}
