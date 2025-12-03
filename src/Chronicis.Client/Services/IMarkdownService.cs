namespace Chronicis.Client.Services;

public interface IMarkdownService
{
    string GetPreview(string markdown, int maxLength = 200);
    string ToHtml(string markdown);
    string ToPlainText(string markdown);
}