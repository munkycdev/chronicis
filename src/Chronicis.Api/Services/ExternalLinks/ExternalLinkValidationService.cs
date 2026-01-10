namespace Chronicis.Api.Services.ExternalLinks;

public class ExternalLinkValidationService
{
    private readonly IExternalLinkProviderRegistry _registry;

    public ExternalLinkValidationService(IExternalLinkProviderRegistry registry)
    {
        _registry = registry;
    }

    public bool TryValidateSource(string source, out string error)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            error = "Source is required.";
            return false;
        }

        var provider = _registry.GetProvider(source);
        if (provider == null)
        {
            var available = _registry
                .GetAllProviders()
                .Select(p => p.Key)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            error = available.Count == 0
                ? $"Unknown external link source '{source}'."
                : $"Unknown external link source '{source}'. Available sources: {string.Join(", ", available)}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public bool TryValidateId(string source, string id, out string error)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "Id is required.";
            return false;
        }

        if (Uri.TryCreate(id, UriKind.Absolute, out _))
        {
            error = "External link id must be a relative path.";
            return false;
        }

        if (!Uri.TryCreate(id, UriKind.Relative, out _))
        {
            error = "External link id must be a relative path.";
            return false;
        }

        if (source.Equals("srd", StringComparison.OrdinalIgnoreCase)
            && !id.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            error = "SRD ids must start with /api/.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
