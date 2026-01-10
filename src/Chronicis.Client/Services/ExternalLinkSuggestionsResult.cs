using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public class ExternalLinkSuggestionsResult
{
    public List<ExternalLinkSuggestionDto> Suggestions { get; set; } = new();
    public bool RequiresSignIn { get; set; }
}
