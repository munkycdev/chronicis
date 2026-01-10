using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services.ExternalLinks;

public class SrdExternalLinkProvider : IExternalLinkProvider
{
    private const string SourceKey = "srd";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SrdExternalLinkProvider> _logger;
    private readonly Uri? _baseUri;

    public SrdExternalLinkProvider(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SrdExternalLinkProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var baseUrl = configuration.GetValue<string>("ExternalLinks:Srd:BaseUrl");
        if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            _baseUri = uri;
        }
    }

    public string Key => SourceKey;

    public async Task<IReadOnlyList<ExternalLinkSuggestion>> SearchAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<ExternalLinkSuggestion>();
        }

        if (_baseUri == null)
        {
            _logger.LogWarning("SRD base URL is not configured.");
            return Array.Empty<ExternalLinkSuggestion>();
        }

        var client = _httpClientFactory.CreateClient("SrdExternalLinks");
        if (client.BaseAddress == null)
        {
            client.BaseAddress = _baseUri;
        }

        var spellsTask = SearchCategoryAsync(client, "spells", query, "Spell", ct);
        var monstersTask = SearchCategoryAsync(client, "monsters", query, "Monster", ct);

        await Task.WhenAll(spellsTask, monstersTask);

        var combined = spellsTask.Result
            .Concat(monstersTask.Result)
            .OrderBy(s => s.Title)
            .Take(10)
            .ToList();

        return combined;
    }

    public async Task<ExternalLinkContent> GetContentAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new ExternalLinkContent
            {
                Source = Key,
                Id = id ?? string.Empty,
                Title = string.Empty,
                Kind = string.Empty,
                Markdown = string.Empty
            };
        }

        if (_baseUri == null)
        {
            _logger.LogWarning("SRD base URL is not configured.");
            return new ExternalLinkContent
            {
                Source = Key,
                Id = id,
                Title = id,
                Kind = string.Empty,
                Markdown = string.Empty
            };
        }

        if (!IsValidSrdId(id))
        {
            _logger.LogWarning("SRD id rejected for content fetch: {Id}", id);
            return new ExternalLinkContent
            {
                Source = Key,
                Id = id,
                Title = id,
                Kind = string.Empty,
                Markdown = string.Empty
            };
        }

        var client = _httpClientFactory.CreateClient("SrdExternalLinks");
        if (client.BaseAddress == null)
        {
            client.BaseAddress = _baseUri;
        }

        try
        {
            using var response = await client.GetAsync(id, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SRD content fetch failed for {Id} with status {StatusCode}", id, response.StatusCode);
                return new ExternalLinkContent
                {
                    Source = Key,
                    Id = id,
                    Title = id,
                    Kind = string.Empty,
                    Markdown = string.Empty
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var root = document.RootElement;
            var title = GetString(root, "name") ?? id;
            var kind = DetermineKind(root);
            var markdown = BuildMarkdown(root, title);

            if (string.IsNullOrWhiteSpace(markdown))
            {
                markdown = $"# {title}";
            }

            return new ExternalLinkContent
            {
                Source = Key,
                Id = id,
                Title = title,
                Kind = kind,
                Markdown = markdown,
                ExternalUrl = BuildHref(id)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SRD content fetch failed for {Id}", id);
            return new ExternalLinkContent
            {
                Source = Key,
                Id = id,
                Title = id,
                Kind = string.Empty,
                Markdown = string.Empty
            };
        }
    }

    private async Task<IReadOnlyList<ExternalLinkSuggestion>> SearchCategoryAsync(
        HttpClient client,
        string category,
        string query,
        string subtitle,
        CancellationToken ct)
    {
        try
        {
            var url = $"/api/{category}?name={Uri.EscapeDataString(query)}";
            var response = await client.GetFromJsonAsync<SrdListResponse>(url, ct);
            if (response?.Results == null || response.Results.Count == 0)
            {
                return Array.Empty<ExternalLinkSuggestion>();
            }

            var suggestions = new List<ExternalLinkSuggestion>();
            foreach (var item in response.Results)
            {
                if (string.IsNullOrWhiteSpace(item.Url) || !item.Url.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                suggestions.Add(new ExternalLinkSuggestion
                {
                    Source = Key,
                    Id = item.Url,
                    Title = item.Name ?? item.Index ?? item.Url,
                    Subtitle = subtitle,
                    Href = BuildHref(item.Url)
                });
            }

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SRD search failed for category {Category} query {Query}", category, query);
            return Array.Empty<ExternalLinkSuggestion>();
        }
    }

    private string? BuildHref(string relativePath)
    {
        if (_baseUri == null)
        {
            return null;
        }

        return new Uri(_baseUri, relativePath).ToString();
    }

    private static bool IsValidSrdId(string id)
    {
        if (!id.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Uri.TryCreate(id, UriKind.Absolute, out _))
        {
            return false;
        }

        return Uri.TryCreate(id, UriKind.Relative, out _);
    }

    private static string DetermineKind(JsonElement root)
    {
        if (root.TryGetProperty("type", out var typeElement))
        {
            var type = typeElement.GetString();
            if (!string.IsNullOrWhiteSpace(type))
            {
                return type;
            }
        }

        if (root.TryGetProperty("school", out var schoolElement))
        {
            var school = GetString(schoolElement, "name");
            if (!string.IsNullOrWhiteSpace(school))
            {
                return "spell";
            }
        }

        if (root.TryGetProperty("hit_points", out _))
        {
            return "monster";
        }

        return string.Empty;
    }

    private static string BuildMarkdown(JsonElement root, string title)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {title}");

        var metadata = BuildMetadata(root);
        if (metadata.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Details");
            foreach (var line in metadata)
            {
                builder.AppendLine($"- {line}");
            }
        }

        AppendDescription(builder, root, "desc", "Description");
        AppendDescription(builder, root, "higher_level", "At Higher Levels");

        AppendNamedListSection(builder, root, "special_abilities", "Special Abilities");
        AppendNamedListSection(builder, root, "actions", "Actions");
        AppendNamedListSection(builder, root, "legendary_actions", "Legendary Actions");

        return builder.ToString().Trim();
    }

    private static List<string> BuildMetadata(JsonElement root)
    {
        var metadata = new List<string>();

        AddMetadata(metadata, "Level", GetInt(root, "level"));
        AddMetadata(metadata, "School", GetStringFromObject(root, "school", "name"));
        AddMetadata(metadata, "Casting Time", GetString(root, "casting_time"));
        AddMetadata(metadata, "Range", GetString(root, "range"));
        AddMetadata(metadata, "Duration", GetString(root, "duration"));

        var components = GetStringArray(root, "components");
        if (components.Count > 0)
        {
            metadata.Add($"Components: {string.Join(", ", components)}");
        }

        AddMetadata(metadata, "Material", GetString(root, "material"));
        AddMetadata(metadata, "Ritual", GetBool(root, "ritual"));
        AddMetadata(metadata, "Concentration", GetBool(root, "concentration"));

        AddMetadata(metadata, "Size", GetString(root, "size"));
        AddMetadata(metadata, "Type", GetString(root, "type"));
        AddMetadata(metadata, "Alignment", GetString(root, "alignment"));
        AddMetadata(metadata, "Armor Class", GetInt(root, "armor_class"));
        AddMetadata(metadata, "Hit Points", GetInt(root, "hit_points"));
        AddMetadata(metadata, "Challenge Rating", GetDecimal(root, "challenge_rating"));

        var speed = GetObjectSummary(root, "speed");
        if (!string.IsNullOrWhiteSpace(speed))
        {
            metadata.Add($"Speed: {speed}");
        }

        return metadata;
    }

    private static void AppendDescription(StringBuilder builder, JsonElement root, string field, string title)
    {
        var lines = GetStringArray(root, field);
        if (lines.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"## {title}");
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                builder.AppendLine();
                builder.AppendLine(line);
            }
        }
    }

    private static void AppendNamedListSection(StringBuilder builder, JsonElement root, string field, string title)
    {
        if (!root.TryGetProperty(field, out var listElement) || listElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var items = listElement.EnumerateArray().ToList();
        if (items.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"## {title}");

        foreach (var item in items)
        {
            var name = GetString(item, "name");
            var desc = GetString(item, "desc");
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(desc))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                builder.AppendLine();
                builder.AppendLine($"### {name}");
            }

            if (!string.IsNullOrWhiteSpace(desc))
            {
                builder.AppendLine();
                builder.AppendLine(desc);
            }
        }
    }

    private static void AddMetadata(List<string> metadata, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata.Add($"{label}: {value}");
        }
    }

    private static void AddMetadata(List<string> metadata, string label, int? value)
    {
        if (value.HasValue)
        {
            metadata.Add($"{label}: {value.Value}");
        }
    }

    private static void AddMetadata(List<string> metadata, string label, decimal? value)
    {
        if (value.HasValue)
        {
            metadata.Add($"{label}: {value.Value}");
        }
    }

    private static void AddMetadata(List<string> metadata, string label, bool? value)
    {
        if (value.HasValue)
        {
            metadata.Add($"{label}: {(value.Value ? "Yes" : "No")}");
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        return null;
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool? GetBool(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? GetStringFromObject(JsonElement element, string propertyName, string childPropertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            return GetString(value, childPropertyName);
        }

        return null;
    }

    private static List<string> GetStringArray(JsonElement element, string propertyName)
    {
        var list = new List<string>();
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
        {
            return list;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                {
                    list.Add(item.GetString()!);
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
        {
            list.Add(value.GetString()!);
        }

        return list;
    }

    private static string? GetObjectSummary(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            var parts = new List<string>();
            foreach (var prop in value.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    var propValue = prop.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(propValue))
                    {
                        parts.Add($"{prop.Name} {propValue}");
                    }
                }
                else if (prop.Value.ValueKind == JsonValueKind.Number)
                {
                    parts.Add($"{prop.Name} {prop.Value}");
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        return value.ToString();
    }

    private sealed class SrdListResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("results")]
        public List<SrdListItem> Results { get; set; } = new();
    }

    private sealed class SrdListItem
    {
        [JsonPropertyName("index")]
        public string? Index { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
