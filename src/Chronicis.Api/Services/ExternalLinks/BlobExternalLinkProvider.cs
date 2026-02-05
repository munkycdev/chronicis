using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Chronicis.Shared.Extensions;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// External link provider backed by Azure Blob Storage.
/// Each provider instance has its own connection string and blob client.
/// </summary>
public class BlobExternalLinkProvider : IExternalLinkProvider
{
    private readonly BlobExternalLinkProviderOptions _options;
    private readonly BlobContainerClient _containerClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BlobExternalLinkProvider> _logger;

    public BlobExternalLinkProvider(
        BlobExternalLinkProviderOptions options,
        IMemoryCache cache,
        ILogger<BlobExternalLinkProvider> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create provider-specific blob client from its own connection string
        // This ensures srd14 and srd24 are fully decoupled
        var blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);

        _logger.LogDebug(
            "Initialized BlobExternalLinkProvider: Key={Key}, DisplayName={DisplayName}, RootPrefix={RootPrefix}",
            _options.Key, _options.DisplayName, _options.RootPrefix);
    }

    public string Key => _options.Key;

    public async Task<IReadOnlyList<ExternalLinkSuggestion>> SearchAsync(string query, CancellationToken ct)
    {
        // Normalize query
        query = query?.Trim() ?? string.Empty;

        var slashIndex = query.IndexOf('/');
        
        // Case A: No slash -> search across all categories + show matching categories
        if (slashIndex < 0)
        {
            return await SearchAcrossAllCategoriesAsync(query, ct);
        }

        // Has slash - need to determine if it's a partial category path or complete category + search
        // Get all categories (includes hierarchical like "items/armor")
        var categories = await GetCategoriesAsync(ct);
        
        // Strategy: Try to find the longest matching category prefix
        // Examples:
        //   Query: "items/armor/lea"
        //   Try: "items/armor/lea" (no match)
        //   Try: "items/armor" (match!) → category="items/armor", search="lea"
        //   
        //   Query: "items/ar"
        //   Try: "items/ar" (no match)
        //   Try: "items" (no match)
        //   → Partial hierarchical path, filter categories
        
        string? matchedCategory = null;
        string searchTerm = string.Empty;
        
        // Try all possible slash positions from right to left
        var remainingQuery = query;
        while (true)
        {
            var lastSlash = remainingQuery.LastIndexOf('/');
            if (lastSlash < 0)
            {
                // No more slashes - check if entire query is a category
                if (categories.Contains(remainingQuery, StringComparer.OrdinalIgnoreCase))
                {
                    matchedCategory = remainingQuery;
                    searchTerm = string.Empty;
                }
                break;
            }
            
            var potentialCategory = remainingQuery[..lastSlash];
            var potentialSearch = remainingQuery[(lastSlash + 1)..];
            
            if (categories.Contains(potentialCategory, StringComparer.OrdinalIgnoreCase))
            {
                // Found a matching category!
                matchedCategory = potentialCategory;
                searchTerm = potentialSearch;
                break;
            }
            
            // Try shorter prefix (remove last segment)
            remainingQuery = potentialCategory;
        }
        
        if (matchedCategory != null)
        {
            // Found exact category - browse or search within it
            var index = await GetCategoryIndexAsync(matchedCategory, ct);

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                // Browse category - return first N items
                var firstN = index
                    .Take(_options.FirstNCategoryItems)
                    .Select(item => new ExternalLinkSuggestion
                    {
                        Source = _options.Key,
                        Id = item.Id,
                        Title = item.Title,
                        Subtitle = matchedCategory,
                        Category = matchedCategory,
                        Icon = null,
                        Href = null
                    })
                    .ToList();

                return firstN;
            }
            else
            {
                // Search within category
                var filtered = FilterCategoryItems(index, matchedCategory, searchTerm);

                _logger.LogDebugSanitized(
                    "Search completed - Provider={Key}, Category={Category}, SearchTerm={SearchTerm}, Results={Count}",
                    _options.Key, matchedCategory, searchTerm, filtered.Count);

                return filtered;
            }
        }
        
        // No exact category match - check if it's a partial hierarchical path
        // Example: "items/ar" should match "items/armor"
        var fullQuery = query.ToLowerInvariant();
        var matchingCategories = categories
            .Where(c => c.StartsWith(fullQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (matchingCategories.Count > 0)
        {
            // Partial hierarchical path - return matching subcategories
            var suggestions = new List<ExternalLinkSuggestion>();
            foreach (var category in matchingCategories)
            {
                var title = BlobFilenameParser.PrettifySlug(category);
                
                suggestions.Add(new ExternalLinkSuggestion
                {
                    Source = _options.Key,
                    Id = $"_category/{category}",
                    Title = title,
                    Subtitle = $"Browse {title}",
                    Category = "_category",
                    Icon = null,
                    Href = null
                });
            }
            
            return suggestions;
        }

        // No matches - return empty
        _logger.LogDebugSanitized(
            "No matching categories - Provider={Key}, Query={Query}",
            _options.Key, query);
        return Array.Empty<ExternalLinkSuggestion>();
    }

    public async Task<ExternalLinkContent> GetContentAsync(string id, CancellationToken ct)
    {
        // Phase 4: Content retrieval and rendering

        // Step 1: Validate ID format
        if (!BlobIdValidator.IsValid(id, out var validationError))
        {
            _logger.LogWarningSanitized(
                "Invalid content ID - Provider={Key}, Id={Id}, Error={Error}",
                _options.Key, id, validationError);
            
            return CreateEmptyContent(id);
        }

        // Step 2: Parse category and slug
        var (category, slug) = BlobIdValidator.ParseId(id);

        // Ensure parsing succeeded (should not fail if validation passed, but be defensive)
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(slug))
        {
            _logger.LogWarningSanitized(
                "Failed to parse content ID after validation - Provider={Key}, Id={Id}",
                _options.Key, id);
            
            return CreateEmptyContent(id);
        }

        // Step 3: Validate category exists
        var categories = await GetCategoriesAsync(ct);
        if (!categories.Contains(category, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarningSanitized(
                "Content requested for invalid category - Provider={Key}, Category={Category}, Id={Id}",
                _options.Key, category, id);
            
            return CreateEmptyContent(id);
        }

        // Step 4: Check cache first
        var cacheKey = BuildCacheKey("Content", id);
        if (_cache.TryGetValue<ExternalLinkContent>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebugSanitized(
                "Content cache hit - Provider={Key}, Id={Id}",
                _options.Key, id);
            return cached;
        }

        // Step 5: Load category index to get blob name mapping
        var index = await GetCategoryIndexAsync(category, ct);
        var item = index.FirstOrDefault(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            _logger.LogWarningSanitized(
                "Content ID not found in category index - Provider={Key}, Id={Id}, Category={Category}, IndexCount={IndexCount}, SampleIds={SampleIds}",
                _options.Key, id, category, index.Count, string.Join(", ", index.Take(5).Select(i => i.Id)));
            
            return CreateEmptyContent(id);
        }

        // Step 6: Fetch blob content
        try
        {
            var blobClient = _containerClient.GetBlobClient(item.BlobName);
            var response = await blobClient.DownloadContentAsync(ct);
            var content = response.Value.Content;

            // Step 7: Parse JSON (handle UTF-8 BOM if present)
            var jsonBytes = content.ToMemory();
            
            // Check for and skip UTF-8 BOM (EF BB BF)
            var span = jsonBytes.Span;
            if (span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
            {
                jsonBytes = jsonBytes.Slice(3);
            }
            
            using var json = JsonDocument.Parse(jsonBytes);

            // Step 8: Render markdown
            var markdown = GenericJsonMarkdownRenderer.RenderMarkdown(
                json, 
                _options.DisplayName, 
                item.Title);

            // Step 9: Create result
            var result = new ExternalLinkContent
            {
                Source = _options.Key,
                Id = id,
                Title = item.Title,
                Kind = BlobFilenameParser.PrettifySlug(category),
                Markdown = markdown,
                Attribution = $"Source: {_options.DisplayName}",
                ExternalUrl = null
            };

            // Cache result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.ContentCacheTtl)
            };
            _cache.Set(cacheKey, result, cacheOptions);

            _logger.LogDebugSanitized(
                "Content retrieved and rendered - Provider={Key}, Id={Id}, BlobName={BlobName}",
                _options.Key, id, item.BlobName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex,
                "Failed to retrieve or render content - Provider={Key}, Id={Id}, BlobName={BlobName}",
                _options.Key, id, item.BlobName);
            
            return CreateEmptyContent(id);
        }
    }

    private ExternalLinkContent CreateEmptyContent(string id)
    {
        return new ExternalLinkContent
        {
            Source = _options.Key,
            Id = id,
            Title = "Content Not Found",
            Kind = "Unknown",
            Markdown = "The requested content could not be found.",
            Attribution = $"Source: {_options.DisplayName}",
            ExternalUrl = null
        };
    }

    // ==================================================================================
    // PRIVATE METHODS - Phase 2: Category Discovery & Index Building
    // ==================================================================================

    // Helper method for creating cache keys
    private string BuildCacheKey(string type, string? key = null)
    {
        // Format: ExternalLinks:{source}:{type}:{key}
        // Examples:
        //   ExternalLinks:srd14:Categories
        //   ExternalLinks:srd14:CategoryIndex:spells
        //   ExternalLinks:srd14:Content:spells/fireball
        var cacheKey = $"ExternalLinks:{_options.Key}:{type}";
        if (!string.IsNullOrEmpty(key))
        {
            cacheKey += $":{key}";
        }
        return cacheKey;
    }

    /// <summary>
    /// Discovers all categories (and subcategories) under the provider's root prefix.
    /// Returns hierarchical categories like "items/armor" if subcategories exist.
    /// Returns flat categories like "spells" if no subcategories.
    /// Results are cached for CategoriesCacheTtl minutes.
    /// </summary>
    private async Task<List<string>> GetCategoriesAsync(CancellationToken ct)
    {
        var cacheKey = BuildCacheKey("Categories");

        // Check cache first
        if (_cache.TryGetValue<List<string>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebug(
                "Categories cache hit - Provider={Key}, Count={Count}",
                _options.Key, cached.Count);
            return cached;
        }

        _logger.LogDebug(
            "Discovering categories - Provider={Key}, RootPrefix={RootPrefix}",
            _options.Key, _options.RootPrefix);

        var sw = Stopwatch.StartNew();
        var categories = new List<string>();

        // Step 1: Get first-level folders (e.g., backgrounds, items, spells)
        var firstLevelBlobs = _containerClient.GetBlobsByHierarchy(
            prefix: _options.RootPrefix,
            delimiter: "/",
            cancellationToken: ct);
        
        foreach (var item in firstLevelBlobs)
        {
            if (item.IsPrefix && item.Prefix != null)
            {
                // Extract top-level category name
                var prefix = item.Prefix;
                
                if (prefix.StartsWith(_options.RootPrefix, StringComparison.Ordinal))
                {
                    var categoryWithSlash = prefix[_options.RootPrefix.Length..];
                    var topLevelCategory = categoryWithSlash.TrimEnd('/');
                    
                    if (string.IsNullOrWhiteSpace(topLevelCategory))
                        continue;

                    // Step 2: Check if this category has subcategories
                    var subcategories = await GetSubcategoriesAsync(topLevelCategory, ct);
                    
                    if (subcategories.Count > 0)
                    {
                        // Has subcategories - add hierarchical paths
                        foreach (var sub in subcategories)
                        {
                            categories.Add($"{topLevelCategory}/{sub}");
                        }
                    }
                    else
                    {
                        // No subcategories - add as flat category
                        categories.Add(topLevelCategory);
                    }
                }
            }
        }

        // Sort alphabetically (case-insensitive)
        categories.Sort(StringComparer.OrdinalIgnoreCase);

        sw.Stop();
        _logger.LogDebug(
            "Categories discovered - Provider={Key}, Count={Count}, Elapsed={Elapsed}ms",
            _options.Key, categories.Count, sw.ElapsedMilliseconds);

        // Cache result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CategoriesCacheTtl)
        };
        _cache.Set(cacheKey, categories, cacheOptions);

        return categories;
    }

    /// <summary>
    /// Checks if a category has subcategories by looking for second-level folders.
    /// Returns empty list if category has only files (no subfolders).
    /// </summary>
    private async Task<List<string>> GetSubcategoriesAsync(string category, CancellationToken ct)
    {
        var subcategories = new List<string>();
        var categoryPrefix = $"{_options.RootPrefix}{category}/";

        var secondLevelBlobs = _containerClient.GetBlobsByHierarchyAsync(
            prefix: categoryPrefix,
            delimiter: "/",
            cancellationToken: ct);

        var hasFiles = false;
        
        await foreach (var item in secondLevelBlobs)
        {
            if (item.IsPrefix && item.Prefix != null)
            {
                // This is a subfolder - extract subcategory name
                var fullPrefix = item.Prefix;
                if (fullPrefix.StartsWith(categoryPrefix, StringComparison.Ordinal))
                {
                    var subcategory = fullPrefix[categoryPrefix.Length..].TrimEnd('/');
                    if (!string.IsNullOrWhiteSpace(subcategory))
                    {
                        subcategories.Add(subcategory);
                    }
                }
            }
            else if (item.IsBlob)
            {
                // This is a file at the category level
                hasFiles = true;
            }
        }

        // If we found files directly under the category, it's flat (no subcategories)
        if (hasFiles && subcategories.Count > 0)
        {
            _logger.LogWarningSanitized(
                "Category has BOTH files and subfolders - treating as flat - Category={Category}",
                category);
            return new List<string>();
        }

        if (hasFiles)
        {
            return new List<string>();
        }

        // Return subcategories sorted
        subcategories.Sort(StringComparer.OrdinalIgnoreCase);
        return subcategories;
    }

    /// <summary>
    /// Generates category suggestions from the categories list.
    /// Returns all categories - no filtering.
    /// </summary>
    private async Task<List<ExternalLinkSuggestion>> GetCategorySuggestionsAsync(CancellationToken ct)
    {
        var categories = await GetCategoriesAsync(ct);
        var suggestions = new List<ExternalLinkSuggestion>();

        foreach (var category in categories)
        {
            var title = BlobFilenameParser.PrettifySlug(category);
            
            suggestions.Add(new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = $"_category/{category}",
                Title = title,
                Subtitle = $"Browse {title}",
                Category = "_category",
                Icon = null,
                Href = null
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Generates filtered category suggestions based on query prefix.
    /// Empty query returns all categories. Non-empty query filters by prefix (case-insensitive).
    /// </summary>
    private async Task<List<ExternalLinkSuggestion>> GetFilteredCategorySuggestionsAsync(string query, CancellationToken ct)
    {
        var categories = await GetCategoriesAsync(ct);
        
        // If query is empty, return all categories
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetCategorySuggestionsAsync(ct);
        }

        // Filter categories by prefix (case-insensitive)
        var queryLower = query.ToLowerInvariant();
        var filtered = categories
            .Where(c => c.StartsWith(queryLower, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var suggestions = new List<ExternalLinkSuggestion>();
        foreach (var category in filtered)
        {
            var title = BlobFilenameParser.PrettifySlug(category);
            
            suggestions.Add(new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = $"_category/{category}",
                Title = title,
                Subtitle = $"Browse {title}",
                Category = "_category",
                Icon = null,
                Href = null
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Searches across all categories for matching items.
    /// Returns category suggestions first, then matching items from all categories.
    /// </summary>
    private async Task<List<ExternalLinkSuggestion>> SearchAcrossAllCategoriesAsync(string query, CancellationToken ct)
    {
        var results = new List<ExternalLinkSuggestion>();
        
        // If query is empty, return all categories only
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetCategorySuggestionsAsync(ct);
        }

        var categories = await GetCategoriesAsync(ct);
        
        // Part 1: Add matching categories at the top
        var matchingCategories = categories
            .Where(c => c.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        foreach (var category in matchingCategories)
        {
            var title = BlobFilenameParser.PrettifySlug(category);
            
            results.Add(new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = $"_category/{category}",
                Title = title,
                Subtitle = $"Browse {title}",
                Category = "_category",
                Icon = null,
                Href = null
            });
        }

        // Part 2: Search for matching items across all categories
        var itemResults = new List<ExternalLinkSuggestion>();
        
        foreach (var category in categories)
        {
            // Get index for this category
            var index = await GetCategoryIndexAsync(category, ct);
            
            // Filter items that match the query
            var matchingItems = index
                .Where(item => item.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(5) // Limit per category to avoid overwhelming results
                .Select(item => new ExternalLinkSuggestion
                {
                    Source = _options.Key,
                    Id = item.Id,
                    Title = item.Title,
                    Subtitle = $"{BlobFilenameParser.PrettifySlug(category)}",
                    Category = category,
                    Icon = null,
                    Href = null
                });
            
            itemResults.AddRange(matchingItems);
        }
        
        // Add item results after categories, limit total
        results.AddRange(itemResults.Take(_options.MaxSuggestions - results.Count));
        
        _logger.LogDebugSanitized(
            "Cross-category search - Provider={Key}, Query={Query}, Categories={CatCount}, Items={ItemCount}",
            _options.Key, query, matchingCategories.Count, itemResults.Count);
        
        return results;
    }

    /// <summary>
    /// Builds and caches the index for a specific category.
    /// Uses blob filenames as titles (no download needed - FAST).
    /// </summary>
    private async Task<List<CategoryItem>> GetCategoryIndexAsync(string category, CancellationToken ct)
    {
        // Normalize category to lowercase
        category = category.ToLowerInvariant();

        // Validate category exists
        var categories = await GetCategoriesAsync(ct);
        if (!categories.Contains(category, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarningSanitized(
                "Category not found - Provider={Key}, Category={Category}",
                _options.Key, category);
            return new List<CategoryItem>();
        }

        var cacheKey = BuildCacheKey("CategoryIndex", category);

        // Check cache first
        if (_cache.TryGetValue<List<CategoryItem>>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebugSanitized(
                "Category index cache hit - Provider={Key}, Category={Category}, Count={Count}",
                _options.Key, category, cached.Count);
            return cached;
        }

        _logger.LogDebugSanitized(
            "Building category index - Provider={Key}, Category={Category}",
            _options.Key, category);

        var sw = Stopwatch.StartNew();
        var items = new List<CategoryItem>();
        var prefix = $"{_options.RootPrefix}{category}/";

        // List all blobs in category folder
        await foreach (var blobItem in _containerClient.GetBlobsAsync(
            prefix: prefix,
            cancellationToken: ct))
        {
            var blobName = blobItem.Name;

            // Only process .json files
            if (!blobName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Optional: Skip very large blobs (safety measure)
            if (blobItem.Properties.ContentLength.HasValue &&
                blobItem.Properties.ContentLength.Value > 5_000_000) // 5 MB
            {
                _logger.LogWarningSanitized(
                    "Skipping large blob - Provider={Key}, Blob={Blob}, Size={Size}",
                    _options.Key, blobName, blobItem.Properties.ContentLength.Value);
                continue;
            }

            // Extract filename from full path
            var lastSlash = blobName.LastIndexOf('/');
            var filename = lastSlash >= 0 ? blobName[(lastSlash + 1)..] : blobName;

            // Derive slug
            var slug = BlobFilenameParser.DeriveSlug(filename);

            // CRITICAL: Skip if slug is empty (per contract)
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarningSanitized(
                    "Skipping blob due to empty slug - Provider={Key}, Blob={Blob}",
                    _options.Key, blobName);
                continue;
            }

            // Compute ID
            var id = $"{category}/{slug}";

            // Use filename as title (no download needed - FAST!)
            // The JSON filename IS the entity title
            var title = BlobFilenameParser.PrettifySlug(slug);

            items.Add(new CategoryItem(id, title, blobName, Pk: null));
        }

        // Sort by title (case-insensitive), then by ID for determinism
        var sortedItems = items
            .OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Id)
            .ToList();

        sw.Stop();
        _logger.LogDebugSanitized(
            "Category index built - Provider={Key}, Category={Category}, Count={Count}, Elapsed={Elapsed}ms",
            _options.Key, category, sortedItems.Count, sw.ElapsedMilliseconds);

        // Cache result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CategoryIndexCacheTtl)
        };
        _cache.Set(cacheKey, sortedItems, cacheOptions);

        return sortedItems;
    }

    /// <summary>
    /// Filters category items by search term using AND semantics.
    /// Phase 3: Multi-token matching with case-insensitive contains.
    /// </summary>
    /// <param name="items">Category index items (pre-sorted alphabetically).</param>
    /// <param name="category">The category being searched (for Subtitle/Category fields).</param>
    /// <param name="searchTerm">Search term with one or more space-separated tokens.</param>
    /// <returns>Filtered suggestions, ordered alphabetically, limited to MaxSuggestions.</returns>
    private List<ExternalLinkSuggestion> FilterCategoryItems(
        List<CategoryItem> items, 
        string category,
        string searchTerm)
    {
        // Tokenize search term: split on whitespace, trim, ignore empty
        var tokens = searchTerm
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        if (tokens.Count == 0)
        {
            // No valid tokens - return empty
            return new List<ExternalLinkSuggestion>();
        }

        // Filter: ALL tokens must match item.Title (case-insensitive)
        var matches = items
            .Where(item => tokens.All(token => 
                item.Title.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .Take(_options.MaxSuggestions)
            .Select(item => new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = item.Id,
                Title = item.Title,
                Subtitle = category,  // Use explicit category parameter
                Category = category,  // Use explicit category parameter
                Icon = null,
                Href = null
            })
            .ToList();

        return matches;
    }
}
