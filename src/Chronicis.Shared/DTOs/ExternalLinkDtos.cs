using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.DTOs;

[ExcludeFromCodeCoverage]
public class ExternalLinkSuggestionDto
{
    public string Source { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Category { get; set; }
    public string? Icon { get; set; }
    public string? Href { get; set; }
}

[ExcludeFromCodeCoverage]
public class ExternalLinkContentDto
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
    /// Null for providers that don't support it (e.g., SRD API).
    /// When present, the client can render this using render definitions
    /// instead of the markdown fallback.
    /// </summary>
    public string? JsonData { get; set; }
}

[ExcludeFromCodeCoverage]
public class ExternalLinkErrorDto
{
    public string Message { get; set; } = string.Empty;
}
