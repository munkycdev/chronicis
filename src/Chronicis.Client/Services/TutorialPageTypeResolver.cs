using Chronicis.Shared.Enums;

namespace Chronicis.Client.Services;

/// <summary>
/// Resolves the canonical tutorial PageType key from the current route context.
/// </summary>
public class TutorialPageTypeResolver
{
    private static readonly Dictionary<string, string> RoutePageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dashboard"] = "Dashboard",
        ["settings"] = "Settings",
        ["world"] = "WorldDetail",
        ["campaign"] = "CampaignDetail",
        ["arc"] = "ArcDetail",
        ["session"] = "SessionDetail",
        ["admin/status"] = "AdminStatus",
        ["admin/utilities"] = "AdminUtilities",
        ["search"] = "Search",
        ["cosmos"] = "Cosmos",
        ["about"] = "About",
        ["getting-started"] = "GettingStarted",
        ["changelog"] = "ChangeLog",
        ["change-log"] = "ChangeLog",
        ["privacy"] = "Privacy",
        ["terms"] = "Terms",
        ["licenses"] = "Licenses"
    };

    /// <summary>
    /// Resolves the canonical page type for tutorial lookup.
    /// </summary>
    /// <param name="uri">Current absolute or app-relative URI.</param>
    /// <param name="articleType">Current article type when on article detail route.</param>
    /// <param name="worldId">Optional world identifier (reserved for future use).</param>
    /// <param name="isTutorialWorld">Optional tutorial-world flag (reserved for future use).</param>
    public string Resolve(
        string uri,
        ArticleType? articleType = null,
        Guid? worldId = null,
        bool? isTutorialWorld = null)
    {
        _ = worldId;
        _ = isTutorialWorld;

        if (!TryGetPathSegments(uri, out var segments))
        {
            return "Page:Default";
        }

        if (segments.Length == 0)
        {
            return "Page:Default";
        }

        if (segments[0].Equals("article", StringComparison.OrdinalIgnoreCase))
        {
            return articleType.HasValue
                ? $"ArticleType:{articleType.Value}"
                : "ArticleType:Any";
        }

        return $"Page:{ResolvePageName(segments)}";
    }

    private static string ResolvePageName(string[] segments)
    {
        if (segments.Length == 0)
        {
            return "Default";
        }

        if (segments[0].Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length >= 2 && RoutePageNames.TryGetValue($"{segments[0]}/{segments[1]}", out var adminPageName))
            {
                return adminPageName;
            }

            return segments.Length >= 2
                ? $"Admin{ToPascalCase(segments[1])}"
                : "Admin";
        }

        if (RoutePageNames.TryGetValue(segments[0], out var pageName))
        {
            return pageName;
        }

        return ToPascalCase(segments[0]);
    }

    private static bool TryGetPathSegments(string uri, out string[] segments)
    {
        segments = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        Uri? parsedUri = null;
        if (Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri) &&
            (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            parsedUri = absoluteUri;
        }
        else if (Uri.TryCreate($"https://local/{uri.TrimStart('/')}", UriKind.Absolute, out var relativeAsAbsolute))
        {
            parsedUri = relativeAsAbsolute;
        }

        if (parsedUri == null)
        {
            return false;
        }

        segments = parsedUri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return true;
    }

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Default";
        }

        var parts = value
            .Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "Default";
        }

        return string.Concat(parts.Select(ToPascalWord));
    }

    private static string ToPascalWord(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return string.Empty;
        }

        if (word.Length == 1)
        {
            return word.ToUpperInvariant();
        }

        return char.ToUpperInvariant(word[0]) + word[1..];
    }
}
