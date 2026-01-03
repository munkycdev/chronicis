namespace Chronicis.Client.Services;

public interface IMarkdownService
{
    string GetPreview(string markdown, int maxLength = 200);
    string ToHtml(string markdown);
    string ToPlainText(string markdown);
    
    /// <summary>
    /// Ensures content is HTML. If content appears to be markdown, converts it to HTML.
    /// If content is already HTML, returns it as-is.
    /// </summary>
    string EnsureHtml(string content);
    
    /// <summary>
    /// Detects if content is HTML (vs markdown)
    /// </summary>
    bool IsHtml(string content);
}