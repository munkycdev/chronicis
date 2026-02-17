namespace Chronicis.Api.Services.ExternalLinks;

public class ExternalLinkContent
{
    public string Source { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public string? Attribution { get; set; }
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// Raw JSON data for client-side structured rendering.
    /// Populated by blob-backed providers. Null for API-backed providers.
    /// </summary>
    public string? JsonData { get; set; }
}
