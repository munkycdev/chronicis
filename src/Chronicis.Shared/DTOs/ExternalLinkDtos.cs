namespace Chronicis.Shared.DTOs;

public class ExternalLinkSuggestionDto
{
    public string Source { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Icon { get; set; }
    public string? Href { get; set; }
}

public class ExternalLinkContentDto
{
    public string Source { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public string? Attribution { get; set; }
    public string? ExternalUrl { get; set; }
}

public class ExternalLinkErrorDto
{
    public string Message { get; set; } = string.Empty;
}
