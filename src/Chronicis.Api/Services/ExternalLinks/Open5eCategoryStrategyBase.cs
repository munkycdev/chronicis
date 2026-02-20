using System.Text.Json;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// Base class providing shared defaults for Open5e category strategies.
/// </summary>
public abstract class Open5eCategoryStrategyBase : IOpen5eCategoryStrategy
{
    public abstract string CategoryKey { get; }
    public abstract string Endpoint { get; }
    public abstract string DisplayName { get; }
    public abstract string? Icon { get; }

    public virtual string DocumentSlug => "5e-2014";
    public virtual string WebCategory => CategoryKey;

    public abstract string BuildMarkdown(JsonElement root, string title);

    /// <summary>
    /// Default subtitle is just the display name. Override to add category-specific details.
    /// </summary>
    public virtual string BuildSubtitle(JsonElement item) => DisplayName;
}
