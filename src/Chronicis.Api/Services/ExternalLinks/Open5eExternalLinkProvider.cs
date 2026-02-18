using System.Text.Json;
using Chronicis.Shared.Extensions;

namespace Chronicis.Api.Services.ExternalLinks;

public class Open5eExternalLinkProvider : IExternalLinkProvider
{
    private const string SourceKey = "srd";
    private const string HttpClientName = "Open5eApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Open5eExternalLinkProvider> _logger;

    // Category definitions - all using v2 API
    // v2 API uses document__gamesystem__key filter for SRD content
    private static readonly Dictionary<string, CategoryConfig> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["spells"] = new("spells", "5e-2014", "Spell"),
        ["monsters"] = new("creatures", "5e-2014", "Monster"),
        ["magicitems"] = new("items", "5e-2014", "Magic Item"),
        ["conditions"] = new("conditions", "5e-2014", "Condition"),
        ["backgrounds"] = new("backgrounds", "5e-2014", "Background"),
        ["feats"] = new("feats", "5e-2014", "Feat"),
        ["classes"] = new("classes", "5e-2014", "Class"),
        ["races"] = new("races", "5e-2014", "Race"),
        ["weapons"] = new("weapons", "5e-2014", "Weapon"),
        ["armor"] = new("armor", "5e-2014", "Armor")
    };

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
        {
            // Empty query - return all category suggestions
            return GetCategorySuggestions(string.Empty);
        }

        // Check if query contains a slash (indicating category/searchterm format)
        var slashIndex = query.IndexOf('/');

        if (slashIndex < 0)
        {
            // No slash - user is still typing a category name
            // Return matching category suggestions
            return GetCategorySuggestions(query);
        }

        // Has a slash - parse as category/searchterm
        var categoryPart = query[..slashIndex].Trim().ToLowerInvariant();
        var searchTerm = slashIndex < query.Length - 1 ? query[(slashIndex + 1)..].Trim() : string.Empty;

        // Find the matching category (exact or prefix match)
        var category = Categories.Keys.FirstOrDefault(k =>
            k.Equals(categoryPart, StringComparison.OrdinalIgnoreCase))
            ?? Categories.Keys.FirstOrDefault(k =>
                k.StartsWith(categoryPart, StringComparison.OrdinalIgnoreCase));

        if (category == null)
        {
            // No matching category - return filtered category suggestions
            return GetCategorySuggestions(categoryPart);
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            // Category selected but no search term yet
            // Return empty - user needs to type something to search
            return Array.Empty<ExternalLinkSuggestion>();
        }

        // Search the specific category
        var client = _httpClientFactory.CreateClient(HttpClientName);

        var config = Categories[category];
        var results = await SearchCategoryAsync(client, category, config, searchTerm, ct);

        return results
            .OrderBy(s => s.Title)
            .Take(20)
            .ToList();
    }

    private List<ExternalLinkSuggestion> GetCategorySuggestions(string filter)
    {
        var suggestions = new List<ExternalLinkSuggestion>();

        foreach (var kvp in Categories)
        {
            var categoryName = kvp.Key;
            var config = kvp.Value;

            // Filter by prefix if filter is provided
            if (!string.IsNullOrEmpty(filter) &&
                !categoryName.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            suggestions.Add(new ExternalLinkSuggestion
            {
                Source = Key,
                Id = $"_category/{categoryName}",  // Special ID prefix for categories
                Title = char.ToUpper(categoryName[0]) + categoryName[1..],  // Capitalize
                Subtitle = $"Browse {config.DisplayName}s",
                Category = "_category",  // Special marker
                Icon = GetCategoryIcon(categoryName)
            });
        }

        return suggestions.OrderBy(s => s.Title).ToList();
    }

    private static string? GetCategoryIcon(string category)
    {
        return category switch
        {
            "spells" => "✨",
            "monsters" => "🐉",
            "magicitems" => "💎",
            "conditions" => "⚡",
            "backgrounds" => "📜",
            "feats" => "⭐",
            "classes" => "⚔️",
            "races" => "👤",
            "weapons" => "🗡️",
            "armor" => "🛡️",
            _ => null
        };
    }

    public async Task<ExternalLinkContent> GetContentAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return CreateEmptyContent(id ?? string.Empty);
        }

        // Parse category and key from id (e.g., "spells/srd_fireball")
        var (category, itemKey) = ParseId(id);
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(itemKey))
        {
            _logger.LogWarningSanitized("Invalid id format: {Id}", id);
            return CreateEmptyContent(id);
        }

        if (!Categories.TryGetValue(category, out var config))
        {
            _logger.LogWarningSanitized("Unknown category in id: {Category}", category);
            return CreateEmptyContent(id);
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);

        try
        {
            var url = $"/v2/{config.Endpoint}/{itemKey}/";
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
            var markdown = BuildMarkdown(root, title, category, config);
            var attribution = BuildAttribution(root);

            return new ExternalLinkContent
            {
                Source = Key,
                Id = id,
                Title = title,
                Kind = config.DisplayName,
                Markdown = markdown,
                Attribution = attribution,
                ExternalUrl = BuildWebUrl(category, itemKey)
            };
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Open5e content fetch failed for {Id}", id);
            return CreateEmptyContent(id);
        }
    }

    private async Task<List<ExternalLinkSuggestion>> SearchCategoryAsync(
        HttpClient client,
        string category,
        CategoryConfig config,
        string query,
        CancellationToken ct)
    {
        try
        {
            // Build URL with document filter (v2 API)
            var docFilter = $"document__gamesystem__key={config.DocumentSlug}";

            // Use name__contains for name-based filtering
            var url = $"/v2/{config.Endpoint}/?name__contains={Uri.EscapeDataString(query)}&{docFilter}&limit=50";

            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarningSanitized("Open5e search failed for {Category} with status {StatusCode}",
                    category, response.StatusCode);
                return new List<ExternalLinkSuggestion>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var results = document.RootElement.GetProperty("results");
            var suggestions = new List<ExternalLinkSuggestion>();

            foreach (var item in results.EnumerateArray())
            {
                // Filter to only include items where the name contains the search term
                // (Open5e searches full content, we want name-only matching)
                var name = GetString(item, "name");
                if (string.IsNullOrEmpty(name) ||
                    !name.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var suggestion = ParseSearchResult(item, category, config);
                if (suggestion != null)
                {
                    suggestions.Add(suggestion);
                }
            }

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Open5e search failed for category {Category} query {Query}", category, query);
            return new List<ExternalLinkSuggestion>();
        }
    }

    private ExternalLinkSuggestion? ParseSearchResult(JsonElement item, string category, CategoryConfig config)
    {
        // v2 API uses "key" for item identifier
        var itemKey = GetString(item, "key");
        var name = GetString(item, "name");

        if (string.IsNullOrEmpty(itemKey) || string.IsNullOrEmpty(name))
        {
            return null;
        }

        // Build subtitle with additional context
        var subtitle = BuildSubtitle(item, category, config);

        return new ExternalLinkSuggestion
        {
            Source = Key,
            Id = $"{category}/{itemKey}",
            Title = name,
            Subtitle = subtitle,
            Category = category,
            Href = BuildWebUrl(category, itemKey)
        };
    }

    private string BuildSubtitle(JsonElement item, string category, CategoryConfig config)
    {
        var parts = new List<string> { config.DisplayName };

        switch (category)
        {
            case "spells":
                var level = GetInt(item, "level");
                var school = GetStringFromObject(item, "school", "name");
                if (level.HasValue)
                {
                    parts.Add(level == 0 ? "Cantrip" : $"Level {level}");
                }
                if (!string.IsNullOrEmpty(school))
                {
                    parts.Add(school);
                }
                break;

            case "monsters":
                var cr = GetString(item, "challenge_rating");
                var type = GetString(item, "type");
                if (!string.IsNullOrEmpty(cr))
                {
                    parts.Add($"CR {cr}");
                }
                if (!string.IsNullOrEmpty(type))
                {
                    parts.Add(type);
                }
                break;

            case "magicitems":
                var rarity = GetString(item, "rarity");
                var itemType = GetString(item, "type");
                if (!string.IsNullOrEmpty(rarity))
                {
                    parts.Add(rarity);
                }
                if (!string.IsNullOrEmpty(itemType))
                {
                    parts.Add(itemType);
                }
                break;

            case "armor":
            case "weapons":
                var category_range = GetString(item, "category_range") ?? GetString(item, "category");
                if (!string.IsNullOrEmpty(category_range))
                {
                    parts.Add(category_range);
                }
                break;
        }

        return string.Join(" • ", parts);
    }

    private string? BuildWebUrl(string category, string itemKey)
    {
        // Map to Open5e website URLs
        var webCategory = category switch
        {
            "magicitems" => "magic-items",
            _ => category
        };

        return $"https://open5e.com/{webCategory}/{itemKey}";
    }

    private static (string? category, string searchTerm) ParseQuery(string query)
    {
        var trimmed = query.Trim();
        var slashIndex = trimmed.IndexOf('/');

        if (slashIndex > 0 && slashIndex < trimmed.Length - 1)
        {
            var possibleCategory = trimmed[..slashIndex].ToLowerInvariant();

            // Try exact match first
            if (Categories.ContainsKey(possibleCategory))
            {
                return (possibleCategory, trimmed[(slashIndex + 1)..].Trim());
            }

            // Try prefix/partial match (e.g., "spell" matches "spells", "monster" matches "monsters")
            var matchedCategory = Categories.Keys
                .FirstOrDefault(k => k.StartsWith(possibleCategory, StringComparison.OrdinalIgnoreCase));

            if (matchedCategory != null)
            {
                return (matchedCategory, trimmed[(slashIndex + 1)..].Trim());
            }
        }

        return (null, trimmed);
    }

    private static (string? category, string? itemKey) ParseId(string id)
    {
        var slashIndex = id.IndexOf('/');
        if (slashIndex > 0 && slashIndex < id.Length - 1)
        {
            return (id[..slashIndex], id[(slashIndex + 1)..]);
        }

        return (null, null);
    }

    private string BuildMarkdown(JsonElement root, string title, string category, CategoryConfig config)
    {
        return category switch
        {
            "spells" => BuildSpellMarkdown(root, title),
            "monsters" => BuildMonsterMarkdown(root, title),
            "magicitems" => BuildMagicItemMarkdown(root, title),
            "conditions" => BuildSimpleMarkdown(root, title, "desc"),
            "backgrounds" => BuildBackgroundMarkdown(root, title),
            "feats" => BuildFeatMarkdown(root, title),
            "classes" => BuildClassMarkdown(root, title),
            "races" => BuildRaceMarkdown(root, title),
            "weapons" => BuildWeaponMarkdown(root, title),
            "armor" => BuildArmorMarkdown(root, title),
            _ => BuildSimpleMarkdown(root, title, "desc")
        };
    }

    private string BuildSpellMarkdown(JsonElement root, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        // Spell details
        var level = GetInt(root, "level");
        var school = GetStringFromObject(root, "school", "name");
        var levelText = level == 0 ? "Cantrip" : $"Level {level}";
        if (!string.IsNullOrEmpty(school))
        {
            sb.AppendLine($"*{levelText} {school}*");
        }
        else
        {
            sb.AppendLine($"*{levelText}*");
        }
        sb.AppendLine();

        // Casting info
        var castingTime = GetString(root, "casting_time");
        var range = GetString(root, "range_text") ?? GetString(root, "range")?.ToString();
        var duration = GetString(root, "duration");
        var concentration = GetBool(root, "concentration");
        var ritual = GetBool(root, "ritual");

        sb.AppendLine("## Casting");
        if (!string.IsNullOrEmpty(castingTime))
            sb.AppendLine($"- **Casting Time:** {castingTime}{(ritual == true ? " (ritual)" : "")}");
        if (!string.IsNullOrEmpty(range))
            sb.AppendLine($"- **Range:** {range}");
        if (!string.IsNullOrEmpty(duration))
            sb.AppendLine($"- **Duration:** {(concentration == true ? "Concentration, " : "")}{duration}");

        // Components
        var verbal = GetBool(root, "verbal");
        var somatic = GetBool(root, "somatic");
        var material = GetBool(root, "material");
        var materialDesc = GetString(root, "material_specified");

        var components = new List<string>();
        if (verbal == true)
            components.Add("V");
        if (somatic == true)
            components.Add("S");
        if (material == true)
            components.Add($"M{(!string.IsNullOrEmpty(materialDesc) ? $" ({materialDesc})" : "")}");
        if (components.Count > 0)
            sb.AppendLine($"- **Components:** {string.Join(", ", components)}");

        sb.AppendLine();

        // Description
        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine("## Description");
            sb.AppendLine(desc);
            sb.AppendLine();
        }

        // Higher levels
        var higherLevel = GetString(root, "higher_level");
        if (!string.IsNullOrEmpty(higherLevel))
        {
            sb.AppendLine("## At Higher Levels");
            sb.AppendLine(higherLevel);
        }

        return sb.ToString().Trim();
    }

    private string BuildMonsterMarkdown(JsonElement root, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        // Type line - v2 API returns size and type as objects with "name" property
        var size = GetStringFromObject(root, "size", "name") ?? GetString(root, "size");
        var type = GetStringFromObject(root, "type", "name") ?? GetString(root, "type");
        var alignment = GetString(root, "alignment");
        var typeLine = string.Join(" ", new[] { size, type }.Where(s => !string.IsNullOrEmpty(s)));
        if (!string.IsNullOrEmpty(alignment))
            typeLine += $", {alignment}";
        if (!string.IsNullOrEmpty(typeLine))
        {
            sb.AppendLine($"*{typeLine}*");
            sb.AppendLine();
        }

        // Basic stats
        sb.AppendLine("## Statistics");
        var ac = GetString(root, "armor_class");
        var hp = GetString(root, "hit_points");
        var hitDice = GetString(root, "hit_dice");
        var cr = GetString(root, "challenge_rating") ?? GetString(root, "cr");

        if (!string.IsNullOrEmpty(ac))
            sb.AppendLine($"**Armor Class:** {ac}");
        if (!string.IsNullOrEmpty(hp))
            sb.AppendLine($"**Hit Points:** {hp}{(!string.IsNullOrEmpty(hitDice) ? $" ({hitDice})" : "")}");
        if (!string.IsNullOrEmpty(cr))
            sb.AppendLine($"**Challenge:** {cr}");

        // Speed
        var speed = GetSpeedString(root);
        if (!string.IsNullOrEmpty(speed))
            sb.AppendLine($"**Speed:** {speed}");

        sb.AppendLine();

        // Actions
        AppendNamedArray(sb, root, "actions", "Actions");

        // Special abilities
        AppendNamedArray(sb, root, "special_abilities", "Special Abilities");

        // Legendary actions
        AppendNamedArray(sb, root, "legendary_actions", "Legendary Actions");

        return sb.ToString().Trim();
    }

    private string BuildMagicItemMarkdown(JsonElement root, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var type = GetString(root, "type");
        var rarity = GetString(root, "rarity");
        var attunement = GetString(root, "requires_attunement");

        var subtitle = string.Join(", ", new[] { type, rarity }.Where(s => !string.IsNullOrEmpty(s)));
        if (!string.IsNullOrEmpty(attunement) && attunement.ToLower() != "false")
            subtitle += $" ({attunement})";

        if (!string.IsNullOrEmpty(subtitle))
        {
            sb.AppendLine($"*{subtitle}*");
            sb.AppendLine();
        }

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine(desc);
        }

        return sb.ToString().Trim();
    }

    private string BuildBackgroundMarkdown(JsonElement root, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine(desc);
            sb.AppendLine();
        }

        // Skill proficiencies
        var skillProf = GetString(root, "skill_proficiencies");
        if (!string.IsNullOrEmpty(skillProf))
        {
            sb.AppendLine($"**Skill Proficiencies:** {skillProf}");
            sb.AppendLine();
        }

        // Equipment
        var equipment = GetString(root, "equipment");
        if (!string.IsNullOrEmpty(equipment))
        {
            sb.AppendLine($"**Equipment:** {equipment}");
            sb.AppendLine();
        }

        // Feature
        var featureName = GetString(root, "feature");
        var featureDesc = GetString(root, "feature_desc");
        if (!string.IsNullOrEmpty(featureName))
        {
            sb.AppendLine($"## {featureName}");
            if (!string.IsNullOrEmpty(featureDesc))
                sb.AppendLine(featureDesc);
        }

        return sb.ToString().Trim();
    }

    private string BuildFeatMarkdown(JsonElement root, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var prerequisite = GetString(root, "prerequisite");
        if (!string.IsNullOrEmpty(prerequisite))
        {
            sb.AppendLine($"*Prerequisite: {prerequisite}*");
            sb.AppendLine();
        }

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine(desc);
        }

        return sb.ToString().Trim();
    }

    private string BuildClassMarkdown(JsonElement root, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var hitDie = GetString(root, "hit_dice");
        if (!string.IsNullOrEmpty(hitDie))
        {
            sb.AppendLine($"**Hit Die:** {hitDie}");
            sb.AppendLine();
        }

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine(desc);
        }

        return sb.ToString().Trim();
    }

    private string BuildRaceMarkdown(JsonElement root, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var size = GetString(root, "size");
        var speed = GetString(root, "speed");

        if (!string.IsNullOrEmpty(size))
            sb.AppendLine($"**Size:** {size}");
        if (!string.IsNullOrEmpty(speed))
            sb.AppendLine($"**Speed:** {speed}");

        sb.AppendLine();

        var desc = GetString(root, "desc");
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine(desc);
            sb.AppendLine();
        }

        // Traits
        var traits = GetString(root, "traits");
        if (!string.IsNullOrEmpty(traits))
        {
            sb.AppendLine("## Traits");
            sb.AppendLine(traits);
        }

        return sb.ToString().Trim();
    }

    private string BuildWeaponMarkdown(JsonElement root, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var category = GetString(root, "category") ?? GetString(root, "category_range");
        if (!string.IsNullOrEmpty(category))
        {
            sb.AppendLine($"*{category}*");
            sb.AppendLine();
        }

        var damage = GetString(root, "damage_dice") ?? GetString(root, "damage");
        var damageType = GetString(root, "damage_type");
        var cost = GetString(root, "cost");
        var weight = GetString(root, "weight");

        if (!string.IsNullOrEmpty(damage))
            sb.AppendLine($"**Damage:** {damage}{(!string.IsNullOrEmpty(damageType) ? $" {damageType}" : "")}");
        if (!string.IsNullOrEmpty(cost))
            sb.AppendLine($"**Cost:** {cost}");
        if (!string.IsNullOrEmpty(weight))
            sb.AppendLine($"**Weight:** {weight}");

        var properties = GetStringArray(root, "properties");
        if (properties.Count > 0)
            sb.AppendLine($"**Properties:** {string.Join(", ", properties)}");

        return sb.ToString().Trim();
    }

    private string BuildArmorMarkdown(JsonElement root, string title)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var category = GetString(root, "category");
        if (!string.IsNullOrEmpty(category))
        {
            sb.AppendLine($"*{category}*");
            sb.AppendLine();
        }

        var ac = GetString(root, "base_ac") ?? GetString(root, "ac_string");
        var cost = GetString(root, "cost");
        var weight = GetString(root, "weight");
        var strength = GetString(root, "strength_requirement");
        var stealth = GetString(root, "stealth_disadvantage");

        if (!string.IsNullOrEmpty(ac))
            sb.AppendLine($"**Armor Class:** {ac}");
        if (!string.IsNullOrEmpty(cost))
            sb.AppendLine($"**Cost:** {cost}");
        if (!string.IsNullOrEmpty(weight))
            sb.AppendLine($"**Weight:** {weight}");
        if (!string.IsNullOrEmpty(strength))
            sb.AppendLine($"**Strength Required:** {strength}");
        if (stealth == "true" || stealth == "True")
            sb.AppendLine($"**Stealth:** Disadvantage");

        return sb.ToString().Trim();
    }

    private string BuildSimpleMarkdown(JsonElement root, string title, string descField)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var desc = GetString(root, descField);
        if (!string.IsNullOrEmpty(desc))
        {
            sb.AppendLine(desc);
        }

        return sb.ToString().Trim();
    }

    private string BuildAttribution(JsonElement root)
    {
        // Try to get document info
        var docTitle = GetStringFromObject(root, "document", "name")
            ?? GetString(root, "document__title")
            ?? "System Reference Document 5.1";

        return $"Source: {docTitle}";
    }

    private void AppendAbilityScores(System.Text.StringBuilder sb, JsonElement root)
    {
        var str = GetInt(root, "strength");
        var dex = GetInt(root, "dexterity");
        var con = GetInt(root, "constitution");
        var intel = GetInt(root, "intelligence");
        var wis = GetInt(root, "wisdom");
        var cha = GetInt(root, "charisma");

        if (str.HasValue || dex.HasValue)
        {
            sb.AppendLine("## Ability Scores");
            sb.AppendLine($"| STR | DEX | CON | INT | WIS | CHA |");
            sb.AppendLine($"|:---:|:---:|:---:|:---:|:---:|:---:|");
            sb.AppendLine($"| {str ?? 10} | {dex ?? 10} | {con ?? 10} | {intel ?? 10} | {wis ?? 10} | {cha ?? 10} |");
            sb.AppendLine();
        }
    }

    private void AppendNamedArray(System.Text.StringBuilder sb, JsonElement root, string field, string header)
    {
        if (!root.TryGetProperty(field, out var array) || array.ValueKind != JsonValueKind.Array)
            return;

        var items = array.EnumerateArray().ToList();
        if (items.Count == 0)
            return;

        sb.AppendLine($"## {header}");
        foreach (var item in items)
        {
            var name = GetString(item, "name");
            var desc = GetString(item, "desc");

            if (!string.IsNullOrEmpty(name))
            {
                sb.AppendLine($"### {name}");
            }
            if (!string.IsNullOrEmpty(desc))
            {
                sb.AppendLine(desc);
            }
            sb.AppendLine();
        }
    }

    private string? GetSpeedString(JsonElement root)
    {
        if (root.TryGetProperty("speed", out var speed))
        {
            if (speed.ValueKind == JsonValueKind.Object)
            {
                var parts = new List<string>();
                foreach (var prop in speed.EnumerateObject())
                {
                    var value = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString()
                        : prop.Value.ToString();
                    parts.Add($"{prop.Name} {value}");
                }
                return string.Join(", ", parts);
            }
            return speed.ToString();
        }
        return null;
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

    private static bool? GetBool(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.True)
                return true;
            if (value.ValueKind == JsonValueKind.False)
                return false;
            if (value.ValueKind == JsonValueKind.String)
            {
                var str = value.GetString()!.ToLowerInvariant();
                return str == "true" || str == "yes";
            }
        }
        return null;
    }

    private static List<string> GetStringArray(JsonElement element, string propertyName)
    {
        var list = new List<string>();
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
            return list;

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                string? str = null;
                if (item.ValueKind == JsonValueKind.String)
                {
                    str = item.GetString();
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    // Some arrays contain objects with a "name" property
                    str = GetString(item, "name");
                }

                if (!string.IsNullOrWhiteSpace(str))
                {
                    list.Add(str);
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.String)
        {
            var str = value.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                list.Add(str);
            }
        }

        return list;
    }

    private sealed record CategoryConfig(
        string Endpoint,
        string DocumentSlug,
        string DisplayName);
}
