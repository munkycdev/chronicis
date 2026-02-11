using System.Net.Http.Json;
using Chronicis.Client.Models;

namespace Chronicis.Client.Services;

/// <summary>
/// Loads render definitions from wwwroot/render-definitions/ as static assets.
/// Caches definitions in memory after first load.
/// Resolution walks from most-specific category to least-specific, then falls back to default.
/// </summary>
public class RenderDefinitionService : IRenderDefinitionService
{
    private readonly HttpClient _http;
    private readonly ILogger<RenderDefinitionService> _logger;
    private readonly Dictionary<string, RenderDefinition?> _cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly RenderDefinition DefaultDefinition = new()
    {
        Version = 1,
        DisplayName = null,
        TitleField = "name",
        Sections = new List<RenderSection>(),
        Hidden = new List<string> { "pk", "model" },
        CatchAll = true
    };

    public RenderDefinitionService(HttpClient http, ILogger<RenderDefinitionService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<RenderDefinition> ResolveAsync(string source, string? categoryPath)
    {
        // Build candidate paths from most-specific to least-specific
        // e.g., for source="ros", categoryPath="bestiary/Cultural-Being":
        //   1. render-definitions/ros/bestiary/Cultural-Being.json
        //   2. render-definitions/ros/bestiary.json
        //   3. render-definitions/ros.json
        //   4. render-definitions/_default.json
        //   5. Built-in default (hardcoded)

        var candidates = BuildCandidatePaths(source, categoryPath);

        foreach (var candidate in candidates)
        {
            var definition = await TryLoadAsync(candidate);
            if (definition != null)
            {
                _logger.LogDebug("Resolved render definition: {Path}", candidate);
                return definition;
            }
        }

        _logger.LogDebug(
            "No render definition found for source={Source}, category={Category}. Using built-in default.",
            source, categoryPath);
        return DefaultDefinition;
    }

    private static List<string> BuildCandidatePaths(string source, string? categoryPath)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(categoryPath))
        {
            // Walk up the category path
            // "bestiary/Cultural-Being" → ["bestiary/Cultural-Being", "bestiary"]
            var segments = categoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            for (var i = segments.Length; i > 0; i--)
            {
                var partialPath = string.Join("/", segments.Take(i));
                candidates.Add($"render-definitions/{source}/{partialPath}.json");
            }
        }

        // Source-level fallback
        candidates.Add($"render-definitions/{source}.json");

        // Global default
        candidates.Add("render-definitions/_default.json");

        return candidates;
    }

    private async Task<RenderDefinition?> TryLoadAsync(string path)
    {
        // Check cache (including negative cache for 404s)
        if (_cache.TryGetValue(path, out var cached))
        {
            _logger.LogDebug("RenderDef cache hit for {Path}: {Result}", path, cached != null ? "found" : "negative");
            return cached;
        }

        try
        {
            var fullUri = new Uri(_http.BaseAddress!, path);
            _logger.LogInformation("RenderDef fetching: {Uri}", fullUri);

            var response = await _http.GetAsync(path);
            _logger.LogInformation("RenderDef response for {Path}: {Status} {ContentType}",
                path, (int)response.StatusCode, response.Content.Headers.ContentType?.MediaType);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("RenderDef fetch failed for {Path}: HTTP {Status}", path, (int)response.StatusCode);
                _cache[path] = null;
                return null;
            }

            var definition = await response.Content.ReadFromJsonAsync<RenderDefinition>();
            _cache[path] = definition;
            _logger.LogInformation("RenderDef loaded {Path}: {Sections} sections",
                path, definition?.Sections.Count ?? 0);
            return definition;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "RenderDef HTTP error for {Path}", path);
            _cache[path] = null;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RenderDef parse error for {Path}", path);
            _cache[path] = null;
            return null;
        }
    }
}
