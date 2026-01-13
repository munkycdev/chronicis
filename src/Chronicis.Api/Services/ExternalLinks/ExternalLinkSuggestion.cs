namespace Chronicis.Api.Services.ExternalLinks;

public class ExternalLinkSuggestion
{
    public string Source { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Category { get; set; }
    public string? Icon { get; set; }
    public string? Href { get; set; }
}
