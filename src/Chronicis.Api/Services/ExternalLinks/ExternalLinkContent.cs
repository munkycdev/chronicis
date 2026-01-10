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
}
