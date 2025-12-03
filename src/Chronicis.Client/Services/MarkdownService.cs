using Ganss.Xss;
using Markdig;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for converting markdown to sanitized HTML
/// </summary>
public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly HtmlSanitizer _sanitizer;
    private readonly ILogger<MarkdownService> _logger;

    public MarkdownService(ILogger<MarkdownService> logger)
    {
        _logger = logger;

        // Configure Markdig pipeline with extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Tables, task lists, etc.
            .UseEmojiAndSmiley()     // :smile: syntax
            .UsePipeTables()         // GitHub-style tables
            .UseGridTables()         // Complex tables
            .UseAutoLinks()          // Auto-detect URLs
            .UseGenericAttributes()  // Add CSS classes
            .Build();

        // Configure HTML sanitizer to prevent XSS
        _sanitizer = new HtmlSanitizer();

        // Allow common markdown HTML elements
        _sanitizer.AllowedTags.Add("h1");
        _sanitizer.AllowedTags.Add("h2");
        _sanitizer.AllowedTags.Add("h3");
        _sanitizer.AllowedTags.Add("h4");
        _sanitizer.AllowedTags.Add("h5");
        _sanitizer.AllowedTags.Add("h6");
        _sanitizer.AllowedTags.Add("table");
        _sanitizer.AllowedTags.Add("thead");
        _sanitizer.AllowedTags.Add("tbody");
        _sanitizer.AllowedTags.Add("tr");
        _sanitizer.AllowedTags.Add("th");
        _sanitizer.AllowedTags.Add("td");
        _sanitizer.AllowedTags.Add("img");
        _sanitizer.AllowedTags.Add("code");
        _sanitizer.AllowedTags.Add("pre");
        _sanitizer.AllowedTags.Add("blockquote");
        _sanitizer.AllowedTags.Add("del");
        _sanitizer.AllowedTags.Add("ins");

        // Allow necessary attributes
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("src");
        _sanitizer.AllowedAttributes.Add("alt");
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("title");

        // Allow data attributes for syntax highlighting
        _sanitizer.AllowDataAttributes = true;
    }

    /// <summary>
    /// Convert markdown text to sanitized HTML
    /// </summary>
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        try
        {
            // Convert markdown to HTML
            var html = Markdown.ToHtml(markdown, _pipeline);

            // Sanitize to prevent XSS
            return _sanitizer.Sanitize(html);
        }
        catch (Exception ex)
        {
            // Log error and return escaped text as fallback
            _logger.LogError($"Markdown conversion error: {ex.Message}");
            return $"<p>{System.Net.WebUtility.HtmlEncode(markdown)}</p>";
        }
    }

    /// <summary>
    /// Convert markdown to plain text (strip formatting)
    /// </summary>
    public string ToPlainText(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        try
        {
            var html = Markdown.ToPlainText(markdown, _pipeline);
            return html;
        }
        catch
        {
            return markdown;
        }
    }

    /// <summary>
    /// Get a preview of the markdown (first N characters as plain text)
    /// </summary>
    public string GetPreview(string markdown, int maxLength = 200)
    {
        var plainText = ToPlainText(markdown);

        if (plainText.Length <= maxLength)
            return plainText;

        return plainText.Substring(0, maxLength) + "...";
    }
}
