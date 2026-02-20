using System.Text.Json;
using Chronicis.Shared.Extensions;
using static Chronicis.Api.Services.ExternalLinks.Open5eJsonHelpers;

namespace Chronicis.Api.Services.ExternalLinks;

public class Open5eExternalLinkProvider : IExternalLinkProvider
{
    private const string SourceKey = "srd";
    private const string HttpClientName = "Open5eApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Open5eExternalLinkProvider> _logger;

    private static readonly Dictionary<string, IOpen5eCategoryStrategy> Strategies =
        CreateStrategies();

    public Open5eExternalLinkProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<Open5eExternalLinkProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Key => SourceKey;

    public async Task<IReadOnlyList<ExternalLinkSuggestion>> SearchAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetCategorySuggestions(string.Empty);

        var slashIndex = query.IndexOf('/');

        if (slashIndex < 0)
            return GetCategorySuggestions(query);

        var categoryPart = query[..slashIndex].Trim().ToLowerInvariant();
        var searchTerm = slashIndex < query.Length - 1 ? query[(slashIndex + 1)..].Trim() : string.Empty;

        var strategy = FindStrategy(categoryPart);
        if (strategy == null)
            return GetCategorySuggestions(categoryPart);

        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<ExternalLinkSuggestion>();

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var results = await SearchCategoryAsync(client, strategy, searchTerm, ct);

        return results
            .OrderBy(s => s.Title)
            .Take(20)
            .ToList();
    }

    public async Task<ExternalLinkContent> GetContentAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return CreateEmptyContent(id ?? string.Empty);

        var (category, itemKey) = ParseId(id);
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(itemKey))
        {
            _logger.LogWarningSanitized("Invalid id format: {Id}", id);
            return CreateEmptyContent(id);
        }

        if (!Strategies.TryGetValue(category, out var strategy))
        {
            _logger.LogWarningSanitized("Unknown category in id: {Category}", category);
            return CreateEmptyContent(id);
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);

        try
        {
            var url = $"/v2/{strategy.Endpoint}/{itemKey}/";
            _logger.LogDebugSanitized("Fetching Open5e content: {Url}", url);

            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarningSanitized("Open5e content fetch failed for {Id} with status {StatusCode}",
                    id, response.StatusCode);
                return CreateEmptyContent(id);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var root = document.RootElement;
            var title = GetString(root, "name") ?? itemKey;
            var markdown = strategy.BuildMarkdown(root, title);
            var attribution = BuildAttribution(root);

            return new ExternalLinkContent
            {
                Source = Key,
                Id = id,
                Title = title,
                Kind = strategy.DisplayName,
                Markdown = markdown,
                Attribution = attribution,
                ExternalUrl = BuildWebUrl(strategy, itemKey)
            };
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Open5e content fetch failed for {Id}", id);
            return CreateEmptyContent(id);
        }
    }

    private List<ExternalLinkSuggestion> GetCategorySuggestions(string filter)
    {
        var suggestions = new List<ExternalLinkSuggestion>();

        foreach (var strategy in Strategies.Values)
        {
            if (!string.IsNullOrEmpty(filter) &&
                !strategy.CategoryKey.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            suggestions.Add(new ExternalLinkSuggestion
            {
                Source = Key,
                Id = $"_category/{strategy.CategoryKey}",
                Title = char.ToUpper(strategy.CategoryKey[0]) + strategy.CategoryKey[1..],
                Subtitle = $"Browse {strategy.DisplayName}s",
                Category = "_category",
                Icon = strategy.Icon
            });
        }

        return suggestions.OrderBy(s => s.Title).ToList();
    }

    private async Task<List<ExternalLinkSuggestion>> SearchCategoryAsync(
        HttpClient client,
        IOpen5eCategoryStrategy strategy,
        string query,
        CancellationToken ct)
    {
        try
        {
            var docFilter = $"document__gamesystem__key={strategy.DocumentSlug}";
            var url = $"/v2/{strategy.Endpoint}/?name__contains={Uri.EscapeDataString(query)}&{docFilter}&limit=50";

            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarningSanitized("Open5e search failed for {Category} with status {StatusCode}",
                    strategy.CategoryKey, response.StatusCode);
                return new List<ExternalLinkSuggestion>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var results = document.RootElement.GetProperty("results");
            var suggestions = new List<ExternalLinkSuggestion>();

            foreach (var item in results.EnumerateArray())
            {
                var name = GetString(item, "name");
                if (string.IsNullOrEmpty(name) ||
                    !name.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var suggestion = ParseSearchResult(item, strategy);
                if (suggestion != null)
                    suggestions.Add(suggestion);
            }

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Open5e search failed for category {Category} query {Query}",
                strategy.CategoryKey, query);
            return new List<ExternalLinkSuggestion>();
        }
    }

    private ExternalLinkSuggestion? ParseSearchResult(JsonElement item, IOpen5eCategoryStrategy strategy)
    {
        var itemKey = GetString(item, "key");
        var name = GetString(item, "name");

        if (string.IsNullOrEmpty(itemKey) || string.IsNullOrEmpty(name))
            return null;

        return new ExternalLinkSuggestion
        {
            Source = Key,
            Id = $"{strategy.CategoryKey}/{itemKey}",
            Title = name,
            Subtitle = strategy.BuildSubtitle(item),
            Category = strategy.CategoryKey,
            Href = BuildWebUrl(strategy, itemKey)
        };
    }

    private static string? BuildWebUrl(IOpen5eCategoryStrategy strategy, string itemKey) =>
        $"https://open5e.com/{strategy.WebCategory}/{itemKey}";

    private static (string? category, string? itemKey) ParseId(string id)
    {
        var slashIndex = id.IndexOf('/');
        if (slashIndex > 0 && slashIndex < id.Length - 1)
            return (id[..slashIndex], id[(slashIndex + 1)..]);
        return (null, null);
    }

    private static IOpen5eCategoryStrategy? FindStrategy(string categoryPart)
    {
        if (Strategies.TryGetValue(categoryPart, out var exact))
            return exact;

        return Strategies.Values.FirstOrDefault(s =>
            s.CategoryKey.StartsWith(categoryPart, StringComparison.OrdinalIgnoreCase));
    }

    private ExternalLinkContent CreateEmptyContent(string id)
    {
        return new ExternalLinkContent
        {
            Source = Key,
            Id = id,
            Title = string.Empty,
            Kind = string.Empty,
            Markdown = string.Empty
        };
    }

    private static Dictionary<string, IOpen5eCategoryStrategy> CreateStrategies()
    {
        var strategies = new IOpen5eCategoryStrategy[]
        {
            new SpellCategoryStrategy(),
            new MonsterCategoryStrategy(),
            new MagicItemCategoryStrategy(),
            new ConditionCategoryStrategy(),
            new BackgroundCategoryStrategy(),
            new FeatCategoryStrategy(),
            new ClassCategoryStrategy(),
            new RaceCategoryStrategy(),
            new WeaponCategoryStrategy(),
            new ArmorCategoryStrategy()
        };

        return strategies.ToDictionary(s => s.CategoryKey, StringComparer.OrdinalIgnoreCase);
    }
}
