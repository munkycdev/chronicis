using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Chronicis.Shared.Extensions;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;

namespace Chronicis.Api.Services.ExternalLinks;

/// <summary>
/// External link provider backed by Azure Blob Storage.
/// Supports progressive category drill-down: typing "[[ros" shows top-level categories,
/// "[[ros/bestiary" shows bestiary's children, and so on at any depth.
/// Cross-category text search is supported at the top level (no slash).
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

        var blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);

        _logger.LogDebug(
            "Initialized BlobExternalLinkProvider: Key={Key}, DisplayName={DisplayName}, RootPrefix={RootPrefix}",
            _options.Key, _options.DisplayName, _options.RootPrefix);
    }

    public string Key => _options.Key;

    // ==================================================================================
    // PUBLIC API
    // ==================================================================================

    public async Task<IReadOnlyList<ExternalLinkSuggestion>> SearchAsync(string query, CancellationToken ct)
    {
        query = query?.Trim() ?? string.Empty;

        var slashIndex = query.IndexOf('/');

        // Case A: No slash — top-level behavior
        // Empty query → show top-level categories only
        // Has text  → cross-category search (categories + items)
        if (slashIndex < 0)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetTopLevelCategorySuggestionsAsync(ct);
            }

            return await SearchAcrossAllCategoriesAsync(query, ct);
        }

        // Case B: Has slash — progressive drill-down
        // Split into path prefix and trailing text after last slash
        // Example: "bestiary/beast/abo" → pathPrefix="bestiary/beast", trailingText="abo"
        // Example: "bestiary/"          → pathPrefix="bestiary",       trailingText=""
        // Example: "bestiary/beast/"    → pathPrefix="bestiary/beast", trailingText=""
        var lastSlashIdx = query.LastIndexOf('/');
        var pathPrefix = query[..lastSlashIdx];
        var trailingText = query[(lastSlashIdx + 1)..];

        // Get the children at the path prefix
        var children = await GetChildrenAtPathAsync(pathPrefix, ct);

        if (children == null)
        {
            // Path doesn't exist — try partial matching against parent's children
            // Example: "bestiary/bea" → parent is "", trailing is "bestiary/bea"
            //   We need to find if "bestiary" partially matches a top-level folder
            return await SearchPartialPathAsync(query, ct);
        }

        var results = new List<ExternalLinkSuggestion>();

        // Build child folder suggestions (always shown first)
        var folderSuggestions = children.ChildFolders
            .Where(f => string.IsNullOrWhiteSpace(trailingText)
                || f.Slug.Contains(trailingText, StringComparison.OrdinalIgnoreCase))
            .Select(folder =>
            {
                var fullPath = string.IsNullOrEmpty(pathPrefix) ? folder.Slug : $"{pathPrefix}/{folder.Slug}";
                var title = BlobFilenameParser.PrettifySlug(folder.Slug);
                return new ExternalLinkSuggestion
                {
                    Source = _options.Key,
                    Id = $"_category/{fullPath}",
                    Title = title,
                    Subtitle = $"Browse {title}",
                    Category = "_category",
                    Icon = null,
                    Href = null
                };
            })
            .ToList();

        results.AddRange(folderSuggestions);

        // Build child file suggestions (shown after folders)
        // Use multi-token AND search if trailing text contains spaces
        var fileSuggestions = FilterChildFiles(children.ChildFiles, trailingText, pathPrefix)
            .Take(_options.MaxSuggestions - results.Count)
            .ToList();

        results.AddRange(fileSuggestions);

        _logger.LogDebugSanitized(
            "Drill-down search - Provider={Key}, Path={Path}, Trailing={Trailing}, Folders={FolderCount}, Files={FileCount}",
            _options.Key, pathPrefix, trailingText, folderSuggestions.Count, fileSuggestions.Count);

        return results;
    }

    public async Task<ExternalLinkContent> GetContentAsync(string id, CancellationToken ct)
    {
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
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(slug))
        {
            _logger.LogWarningSanitized(
                "Failed to parse content ID after validation - Provider={Key}, Id={Id}",
                _options.Key, id);
            return CreateEmptyContent(id);
        }

        // Step 3: Check content cache
        var cacheKey = BuildCacheKey("Content", id);
        if (_cache.TryGetValue<ExternalLinkContent>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogDebugSanitized("Content cache hit - Provider={Key}, Id={Id}", _options.Key, id);
            return cached;
        }

        // Step 4: Load category index to find the blob name
        // GetCategoryIndexAsync no longer validates against a flat category list —
        // it directly queries blob storage at the given path.
        var index = await GetCategoryIndexAsync(category, ct);
        var item = index.FirstOrDefault(i => i.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            _logger.LogWarningSanitized(
                "Content ID not found in category index - Provider={Key}, Id={Id}, Category={Category}, IndexCount={IndexCount}",
                _options.Key, id, category, index.Count);
            return CreateEmptyContent(id);
        }

        // Step 5: Fetch blob content and render
        try
        {
            var blobClient = _containerClient.GetBlobClient(item.BlobName);
            var response = await blobClient.DownloadContentAsync(ct);
            var content = response.Value.Content;

            // Handle UTF-8 BOM
            var jsonBytes = content.ToMemory();
            var span = jsonBytes.Span;
            if (span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
            {
                jsonBytes = jsonBytes.Slice(3);
            }

            using var json = JsonDocument.Parse(jsonBytes);

            // Capture raw JSON for client-side structured rendering
            var rawJson = System.Text.Encoding.UTF8.GetString(jsonBytes.Span);

            var markdown = GenericJsonMarkdownRenderer.RenderMarkdown(
                json, _options.DisplayName, item.Title);

            var result = new ExternalLinkContent
            {
                Source = _options.Key,
                Id = id,
                Title = item.Title,
                Kind = BlobFilenameParser.PrettifySlug(category),
                Markdown = markdown,
                Attribution = $"Source: {_options.DisplayName}",
                ExternalUrl = null,
                JsonData = rawJson
            };

            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.ContentCacheTtl)
            });

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
    // PRIVATE METHODS - Progressive Discovery
    // ==================================================================================

    /// <summary>
    /// Result of discovering direct children at a given path.
    /// ChildFolders carry both the original blob name and the lowercase slug for display/IDs.
    /// ChildFiles are CategoryItem records for JSON files at this level.
    /// </summary>
    private record PathChildren(List<ChildFolder> ChildFolders, List<CategoryItem> ChildFiles);

    /// <summary>
    /// A subfolder discovered at a given path level.
    /// BlobName is the original casing as stored in blob (needed for subsequent queries).
    /// Slug is the lowercase-normalized name used in IDs and display.
    /// </summary>
    private record ChildFolder(string BlobName, string Slug);

    /// <summary>
    /// Discovers the direct children (subfolders and files) at a given relative path
    /// within the provider's root prefix. Uses GetBlobsByHierarchy with "/" delimiter
    /// to get exactly one level of the hierarchy.
    /// 
    /// Returns null if the path has no children (doesn't exist or is empty).
    /// Results are cached per path.
    /// </summary>
    /// <param name="relativePath">
    /// Path relative to RootPrefix. Empty string for root level.
    /// Can be in original blob casing OR lowercase — the method resolves via cached mappings.
    /// Example: "bestiary" or "Bestiary" or "bestiary/beast" or "items/armor/light"
    /// </param>
    private async Task<PathChildren?> GetChildrenAtPathAsync(string relativePath, CancellationToken ct)
    {
        var normalizedPath = relativePath.Trim('/');
        
        // Resolve the path to its original blob casing
        var blobPath = await ResolveBlobPathAsync(normalizedPath, ct);
        
        var cacheKey = BuildCacheKey("Children", normalizedPath.ToLowerInvariant());

        if (_cache.TryGetValue<PathChildren>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        // Build the blob prefix using the ORIGINAL casing from blob storage
        var prefix = string.IsNullOrEmpty(blobPath)
            ? _options.RootPrefix
            : $"{_options.RootPrefix}{blobPath}/";

        var childFolders = new List<ChildFolder>();
        var childFiles = new List<CategoryItem>();

        await foreach (var item in _containerClient.GetBlobsByHierarchyAsync(
            prefix: prefix,
            delimiter: "/",
            cancellationToken: ct))
        {
            if (item.IsPrefix && item.Prefix != null)
            {
                // Subfolder — extract the folder name in its ORIGINAL casing
                var originalFolderName = item.Prefix[prefix.Length..].TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(originalFolderName))
                {
                    var slug = originalFolderName.ToLowerInvariant();
                    childFolders.Add(new ChildFolder(originalFolderName, slug));
                    
                    // Cache the slug → blob path mapping for this child
                    var childSlugPath = string.IsNullOrEmpty(normalizedPath)
                        ? slug
                        : $"{normalizedPath.ToLowerInvariant()}/{slug}";
                    var childBlobPath = string.IsNullOrEmpty(blobPath)
                        ? originalFolderName
                        : $"{blobPath}/{originalFolderName}";
                    CacheBlobPathMapping(childSlugPath, childBlobPath);
                }
            }
            else if (item.IsBlob && item.Blob != null)
            {
                var blobName = item.Blob.Name;
                if (!blobName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (item.Blob.Properties.ContentLength.HasValue &&
                    item.Blob.Properties.ContentLength.Value > 5_000_000)
                    continue;

                var lastSlash = blobName.LastIndexOf('/');
                var filename = lastSlash >= 0 ? blobName[(lastSlash + 1)..] : blobName;
                var slug = BlobFilenameParser.DeriveSlug(filename);

                if (string.IsNullOrWhiteSpace(slug))
                    continue;

                // IDs are always lowercase
                var id = string.IsNullOrEmpty(normalizedPath)
                    ? slug.ToLowerInvariant()
                    : $"{normalizedPath.ToLowerInvariant()}/{slug.ToLowerInvariant()}";
                var title = BlobFilenameParser.PrettifySlug(slug);

                childFiles.Add(new CategoryItem(id, title, blobName, Pk: null));
            }
        }

        if (childFolders.Count == 0 && childFiles.Count == 0)
        {
            return null;
        }

        childFolders.Sort((a, b) => string.Compare(a.Slug, b.Slug, StringComparison.OrdinalIgnoreCase));
        childFiles = childFiles
            .OrderBy(f => f.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.Id)
            .ToList();

        var result = new PathChildren(childFolders, childFiles);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CategoriesCacheTtl)
        });

        _logger.LogDebugSanitized(
            "Children discovered - Provider={Key}, Path={Path}, BlobPath={BlobPath}, Folders={FolderCount}, Files={FileCount}",
            _options.Key, normalizedPath, blobPath, childFolders.Count, childFiles.Count);

        return result;
    }

    /// <summary>
    /// Resolves a lowercase slug path back to its original blob casing.
    /// Uses cached mappings built during discovery.
    /// Falls back to the input path if no mapping exists (first-time root discovery).
    /// </summary>
    private async Task<string> ResolveBlobPathAsync(string slugPath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(slugPath))
            return string.Empty;

        var mappingKey = BuildCacheKey("BlobPathMap", slugPath.ToLowerInvariant());
        if (_cache.TryGetValue<string>(mappingKey, out var blobPath) && blobPath != null)
        {
            return blobPath;
        }

        // No cached mapping — this can happen if the cache expired or on first access
        // to a deep path. Walk from root to rebuild mappings.
        var segments = slugPath.Split('/');
        var currentSlugPath = "";
        var currentBlobPath = "";

        for (var i = 0; i < segments.Length; i++)
        {
            // Ensure parent is discovered (this populates child mappings)
            var parentChildren = await GetChildrenAtPathAsync(currentBlobPath, ct);
            if (parentChildren == null)
            {
                // Parent doesn't exist — return input as-is
                return slugPath;
            }

            var targetSlug = segments[i].ToLowerInvariant();
            var matchedFolder = parentChildren.ChildFolders
                .FirstOrDefault(f => f.Slug.Equals(targetSlug, StringComparison.OrdinalIgnoreCase));

            if (matchedFolder == null)
            {
                // Segment not found — return input as-is
                return slugPath;
            }

            currentSlugPath = string.IsNullOrEmpty(currentSlugPath)
                ? matchedFolder.Slug
                : $"{currentSlugPath}/{matchedFolder.Slug}";
            currentBlobPath = string.IsNullOrEmpty(currentBlobPath)
                ? matchedFolder.BlobName
                : $"{currentBlobPath}/{matchedFolder.BlobName}";
        }

        return currentBlobPath;
    }

    /// <summary>
    /// Caches a mapping from lowercase slug path to original blob path.
    /// </summary>
    private void CacheBlobPathMapping(string slugPath, string blobPath)
    {
        var mappingKey = BuildCacheKey("BlobPathMap", slugPath.ToLowerInvariant());
        _cache.Set(mappingKey, blobPath, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CategoriesCacheTtl)
        });
    }

    /// <summary>
    /// Returns suggestions for top-level categories only (no children expanded).
    /// Used when the user types "[[ros" with no slash and no text.
    /// </summary>
    private async Task<List<ExternalLinkSuggestion>> GetTopLevelCategorySuggestionsAsync(CancellationToken ct)
    {
        var children = await GetChildrenAtPathAsync("", ct);
        if (children == null)
        {
            return new List<ExternalLinkSuggestion>();
        }

        var suggestions = new List<ExternalLinkSuggestion>();

        foreach (var folder in children.ChildFolders)
        {
            var title = BlobFilenameParser.PrettifySlug(folder.Slug);
            suggestions.Add(new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = $"_category/{folder.Slug}",
                Title = title,
                Subtitle = $"Browse {title}",
                Category = "_category",
                Icon = null,
                Href = null
            });
        }

        // Also include any files at the root level (unlikely but possible)
        foreach (var file in children.ChildFiles.Take(_options.MaxSuggestions - suggestions.Count))
        {
            suggestions.Add(new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = file.Id,
                Title = file.Title,
                Subtitle = _options.DisplayName,
                Category = "",
                Icon = null,
                Href = null
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Handles partial path matching when the typed path doesn't match a real folder.
    /// Example: "besti" → finds "bestiary" in parent's children.
    /// Example: "bestiary/bea" → finds "beast" under "bestiary".
    /// </summary>
    private async Task<List<ExternalLinkSuggestion>> SearchPartialPathAsync(string query, CancellationToken ct)
    {
        return await SearchPartialPathInternalAsync(query, 0, ct);
    }

    private async Task<List<ExternalLinkSuggestion>> SearchPartialPathInternalAsync(
        string query, int depth, CancellationToken ct)
    {
        if (depth > _options.MaxDrillDownDepth)
            return new List<ExternalLinkSuggestion>();

        // Split into parent path and partial segment
        var lastSlashIdx = query.LastIndexOf('/');
        var parentPath = lastSlashIdx >= 0 ? query[..lastSlashIdx] : "";
        var partialSegment = lastSlashIdx >= 0 ? query[(lastSlashIdx + 1)..] : query;

        var children = await GetChildrenAtPathAsync(parentPath, ct);
        if (children == null)
        {
            // Parent path doesn't exist either — recurse up if there's still a slash
            if (lastSlashIdx > 0)
            {
                return await SearchPartialPathInternalAsync(parentPath, depth + 1, ct);
            }

            return new List<ExternalLinkSuggestion>();
        }

        var results = new List<ExternalLinkSuggestion>();

        // Match folders that contain the partial segment
        var matchingFolders = children.ChildFolders
            .Where(f => f.Slug.Contains(partialSegment, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var folder in matchingFolders)
        {
            var fullPath = string.IsNullOrEmpty(parentPath) ? folder.Slug : $"{parentPath}/{folder.Slug}";
            var title = BlobFilenameParser.PrettifySlug(folder.Slug);
            results.Add(new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = $"_category/{fullPath}",
                Title = title,
                Subtitle = $"Browse {title}",
                Category = "_category",
                Icon = null,
                Href = null
            });
        }

        // Also match files at this level
        var matchingFiles = children.ChildFiles
            .Where(f => f.Title.Contains(partialSegment, StringComparison.OrdinalIgnoreCase))
            .Take(_options.MaxSuggestions - results.Count);

        foreach (var file in matchingFiles)
        {
            results.Add(new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = file.Id,
                Title = file.Title,
                Subtitle = BlobFilenameParser.PrettifySlug(parentPath),
                Category = parentPath,
                Icon = null,
                Href = null
            });
        }

        return results;
    }

    /// <summary>
    /// Searches across ALL leaf categories for items matching the query.
    /// Also returns matching category names at the top.
    /// Used only for top-level text search (no slash in query).
    /// </summary>
    private async Task<List<ExternalLinkSuggestion>> SearchAcrossAllCategoriesAsync(string query, CancellationToken ct)
    {
        var results = new List<ExternalLinkSuggestion>();

        // Part 1: Check top-level categories that match the query text
        var topChildren = await GetChildrenAtPathAsync("", ct);
        if (topChildren != null)
        {
            var matchingFolders = topChildren.ChildFolders
                .Where(f => f.Slug.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var folder in matchingFolders)
            {
                var title = BlobFilenameParser.PrettifySlug(folder.Slug);
                results.Add(new ExternalLinkSuggestion
                {
                    Source = _options.Key,
                    Id = $"_category/{folder.Slug}",
                    Title = title,
                    Subtitle = $"Browse {title}",
                    Category = "_category",
                    Icon = null,
                    Href = null
                });
            }
        }

        // Part 2: Search items across all leaf categories
        var leafCategories = await GetAllLeafCategoriesAsync(ct);

        var itemResults = new List<ExternalLinkSuggestion>();
        foreach (var leafPath in leafCategories)
        {
            var index = await GetCategoryIndexAsync(leafPath, ct);

            var matchingItems = index
                .Where(item => item.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(item => new ExternalLinkSuggestion
                {
                    Source = _options.Key,
                    Id = item.Id,
                    Title = item.Title,
                    Subtitle = BlobFilenameParser.PrettifySlug(leafPath),
                    Category = leafPath,
                    Icon = null,
                    Href = null
                });

            itemResults.AddRange(matchingItems);
        }

        results.AddRange(itemResults.Take(_options.MaxSuggestions - results.Count));

        _logger.LogDebugSanitized(
            "Cross-category search - Provider={Key}, Query={Query}, LeafCategories={LeafCount}, ItemMatches={ItemCount}",
            _options.Key, query, leafCategories.Count, itemResults.Count);

        return results;
    }

    /// <summary>
    /// Recursively discovers all leaf category paths (folders that contain files).
    /// A leaf category is a path where GetChildrenAtPathAsync returns files.
    /// Cached for CategoriesCacheTtl minutes.
    /// </summary>
    private async Task<List<string>> GetAllLeafCategoriesAsync(CancellationToken ct)
    {
        var cacheKey = BuildCacheKey("AllLeafCategories");

        if (_cache.TryGetValue<List<string>>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var sw = Stopwatch.StartNew();
        var leaves = new List<string>();

        await CollectLeafCategoriesAsync("", leaves, 0, ct);

        leaves.Sort(StringComparer.OrdinalIgnoreCase);

        sw.Stop();
        _logger.LogDebug(
            "Leaf categories discovered - Provider={Key}, Count={Count}, Elapsed={Elapsed}ms",
            _options.Key, leaves.Count, sw.ElapsedMilliseconds);

        _cache.Set(cacheKey, leaves, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CategoriesCacheTtl)
        });

        return leaves;
    }

    /// <summary>
    /// Recursive helper to walk the folder tree and collect all paths that contain files.
    /// </summary>
    private async Task CollectLeafCategoriesAsync(
        string path, List<string> leaves, int depth, CancellationToken ct)
    {
        if (depth > _options.MaxDrillDownDepth)
        {
            _logger.LogWarningSanitized(
                "Max drill-down depth reached - Provider={Key}, Path={Path}, Depth={Depth}",
                _options.Key, path, depth);
            return;
        }

        var children = await GetChildrenAtPathAsync(path, ct);
        if (children == null) return;

        // If this path has files and is not root, it's a leaf (or mixed) category
        // Skip root-level files since they'd produce IDs without a category prefix
        if (children.ChildFiles.Count > 0 && !string.IsNullOrEmpty(path))
        {
            leaves.Add(path);
        }

        // Recurse into subfolders
        foreach (var folder in children.ChildFolders)
        {
            var childPath = string.IsNullOrEmpty(path) ? folder.Slug : $"{path}/{folder.Slug}";
            await CollectLeafCategoriesAsync(childPath, leaves, depth + 1, ct);
        }
    }

    // ==================================================================================
    // PRIVATE METHODS - Index Building & Filtering
    // ==================================================================================

    private string BuildCacheKey(string type, string? key = null)
    {
        var cacheKey = $"ExternalLinks:{_options.Key}:{type}";
        if (!string.IsNullOrEmpty(key))
        {
            cacheKey += $":{key}";
        }
        return cacheKey;
    }

    /// <summary>
    /// Builds and caches the index of files for a specific category path.
    /// Delegates to GetChildrenAtPathAsync to ensure consistent blob path resolution
    /// (handling mixed-case folder names in blob storage).
    /// Returns only the files (not subfolders) at the given path.
    /// </summary>
    private async Task<List<CategoryItem>> GetCategoryIndexAsync(string category, CancellationToken ct)
    {
        var cacheKey = BuildCacheKey("CategoryIndex", category.ToLowerInvariant());

        if (_cache.TryGetValue<List<CategoryItem>>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        // Delegate to GetChildrenAtPathAsync which handles blob path resolution
        var children = await GetChildrenAtPathAsync(category, ct);
        if (children == null)
        {
            _logger.LogDebugSanitized(
                "Category index empty (path not found) - Provider={Key}, Category={Category}",
                _options.Key, category);
            return new List<CategoryItem>();
        }

        // The ChildFiles are already sorted and have correct lowercase IDs
        var items = children.ChildFiles;

        _logger.LogDebugSanitized(
            "Category index built (via children) - Provider={Key}, Category={Category}, Count={Count}",
            _options.Key, category, items.Count);

        _cache.Set(cacheKey, items, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CategoryIndexCacheTtl)
        });

        return items;
    }

    /// <summary>
    /// Filters child files by search term and converts to suggestions.
    /// Supports multi-token AND search (space-separated tokens all must match).
    /// Empty search term returns all files.
    /// </summary>
    private IEnumerable<ExternalLinkSuggestion> FilterChildFiles(
        List<CategoryItem> files,
        string searchTerm,
        string categoryPath)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return files.Select(item => new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = item.Id,
                Title = item.Title,
                Subtitle = BlobFilenameParser.PrettifySlug(categoryPath),
                Category = categoryPath,
                Icon = null,
                Href = null
            });
        }

        var tokens = searchTerm
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        if (tokens.Count == 0)
            return Enumerable.Empty<ExternalLinkSuggestion>();

        return files
            .Where(item => tokens.All(token =>
                item.Title.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .Select(item => new ExternalLinkSuggestion
            {
                Source = _options.Key,
                Id = item.Id,
                Title = item.Title,
                Subtitle = BlobFilenameParser.PrettifySlug(categoryPath),
                Category = categoryPath,
                Icon = null,
                Href = null
            });
    }
}
