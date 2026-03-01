using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for anonymous public access to worlds.
/// All methods return only publicly visible content - no authentication required.
/// </summary>
public class PublicWorldService : IPublicWorldService
{
    private readonly ChronicisDbContext _context;
    private readonly ILogger<PublicWorldService> _logger;
    private readonly IArticleHierarchyService _hierarchyService;
    private readonly IBlobStorageService _blobStorage;

    public PublicWorldService(
        ChronicisDbContext context,
        ILogger<PublicWorldService> logger,
        IArticleHierarchyService hierarchyService,
        IBlobStorageService blobStorage)
    {
        _context = context;
        _logger = logger;
        _hierarchyService = hierarchyService;
        _blobStorage = blobStorage;
    }

    /// <summary>
    /// Get a public world by its public slug.
    /// Returns null if world doesn't exist or is not public.
    /// </summary>
    public async Task<WorldDetailDto?> GetPublicWorldAsync(string publicSlug)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        var world = await _context.Worlds
            .AsNoTracking()
            .Include(w => w.Owner)
            .Include(w => w.Campaigns)
            .Where(w => w.PublicSlug == normalizedSlug && w.IsPublic)
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogDebugSanitized("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return null;
        }

        _logger.LogDebugSanitized("Public world '{WorldName}' accessed via slug '{PublicSlug}'",
            world.Name, normalizedSlug);

        return new WorldDetailDto
        {
            Id = world.Id,
            Name = world.Name,
            Slug = world.Slug,
            Description = world.Description,
            OwnerId = world.OwnerId,
            OwnerName = world.Owner?.DisplayName ?? "Unknown",
            CreatedAt = world.CreatedAt,
            CampaignCount = world.Campaigns?.Count ?? 0,
            IsPublic = world.IsPublic,
            IsTutorial = world.IsTutorial,
            PublicSlug = world.PublicSlug,
            // Include public campaigns
            Campaigns = world.Campaigns?.Select(c => new CampaignDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                WorldId = c.WorldId,
                IsActive = c.IsActive,
                StartedAt = c.StartedAt
            }).ToList() ?? new List<CampaignDto>()
        };
    }

    /// <summary>
    /// Get the article tree for a public world.
    /// Only returns articles with Public visibility.
    /// Returns a hierarchical tree structure organized by virtual groups (Campaigns, Characters, Wiki).
    /// </summary>
    public async Task<List<ArticleTreeDto>> GetPublicArticleTreeAsync(string publicSlug)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        // First, verify the world exists and is public
        var world = await _context.Worlds
            .AsNoTracking()
            .Include(w => w.Campaigns)
                .ThenInclude(c => c.Arcs)
                    .ThenInclude(a => a.SessionEntities)
            .Where(w => w.PublicSlug == normalizedSlug && w.IsPublic)
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogDebugSanitized("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return new List<ArticleTreeDto>();
        }

        // Get all public articles for this world
        var allPublicArticles = await _context.Articles
            .AsNoTracking()
            .Where(a => a.WorldId == world.Id && a.Visibility == ArticleVisibility.Public)
            .Select(a => new ArticleTreeDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                ParentId = a.ParentId,
                WorldId = a.WorldId,
                CampaignId = a.CampaignId,
                ArcId = a.ArcId,
                SessionId = a.SessionId,
                Type = a.Type,
                Visibility = a.Visibility,
                HasChildren = false, // Will calculate below
                ChildCount = 0,      // Will calculate below
                Children = new List<ArticleTreeDto>(),
                CreatedAt = a.CreatedAt,
                EffectiveDate = a.EffectiveDate,
                IconEmoji = a.IconEmoji,
                CreatedBy = a.CreatedBy
            })
            .ToListAsync();

        // Build article index and children relationships
        var articleIndex = allPublicArticles.ToDictionary(a => a.Id);
        var sessionNotesBySessionId = allPublicArticles
            .Where(a => a.Type == ArticleType.SessionNote && a.SessionId.HasValue)
            .GroupBy(a => a.SessionId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Link children to parents
        foreach (var article in allPublicArticles)
        {
            if (article.ParentId.HasValue && articleIndex.TryGetValue(article.ParentId.Value, out var parent))
            {
                parent.Children ??= new List<ArticleTreeDto>();
                parent.Children.Add(article);
                parent.HasChildren = true;
                parent.ChildCount++;
            }
        }

        // Sort children by title
        foreach (var article in allPublicArticles.Where(a => a.Children?.Any() == true))
        {
            article.Children = article.Children!.OrderBy(c => c.Title).ToList();
        }

        // Build virtual groups
        var result = new List<ArticleTreeDto>();

        // 1. Campaigns group - contains campaigns with their arcs and sessions
        var campaignsGroup = CreateVirtualGroup("campaigns", "Campaigns", "fa-solid fa-dungeon");
        foreach (var campaign in world.Campaigns?.OrderBy(c => c.Name) ?? Enumerable.Empty<Chronicis.Shared.Models.Campaign>())
        {
            var campaignNode = new ArticleTreeDto
            {
                Id = campaign.Id,
                Title = campaign.Name,
                Slug = campaign.Name.ToLowerInvariant().Replace(" ", "-"),
                Type = ArticleType.WikiArticle, // Use WikiArticle as placeholder
                IconEmoji = "fa-solid fa-dungeon",
                Children = new List<ArticleTreeDto>(),
                IsVirtualGroup = true
            };

            // Add arcs under campaign
            foreach (var arc in campaign.Arcs?.OrderBy(a => a.SortOrder).ThenBy(a => a.Name) ?? Enumerable.Empty<Chronicis.Shared.Models.Arc>())
            {
                var arcNode = new ArticleTreeDto
                {
                    Id = arc.Id,
                    Title = arc.Name,
                    Slug = arc.Name.ToLowerInvariant().Replace(" ", "-"),
                    Type = ArticleType.WikiArticle,
                    IconEmoji = "fa-solid fa-book-open",
                    Children = new List<ArticleTreeDto>(),
                    IsVirtualGroup = true
                };

                var sessions = arc.SessionEntities?
                    .OrderBy(s => s.SessionDate ?? DateTime.MaxValue)
                    .ThenBy(s => s.Name)
                    .ThenBy(s => s.CreatedAt)
                    ?? Enumerable.Empty<Chronicis.Shared.Models.Session>();

                foreach (var session in sessions)
                {
                    var hasLegacyPublicSessionArticle = TryGetLegacyPublicSessionArticle(
                        articleIndex,
                        session.Id,
                        out var legacySessionArticle);

                    var rootSessionNotes = GetRootSessionNotesForSession(sessionNotesBySessionId, session.Id);

                    if (hasLegacyPublicSessionArticle)
                    {
                        var compatibilitySessionArticle = legacySessionArticle!;
                        AttachRootSessionNotesToLegacySessionNode(
                            compatibilitySessionArticle,
                            rootSessionNotes,
                            !string.IsNullOrWhiteSpace(session.AiSummary));

                        arcNode.Children.Add(compatibilitySessionArticle);
                        arcNode.HasChildren = true;
                        arcNode.ChildCount++;
                        continue;
                    }

                    if (!rootSessionNotes.Any())
                    {
                        continue;
                    }

                    var sessionNode = new ArticleTreeDto
                    {
                        Id = session.Id,
                        Title = session.Name,
                        Slug = session.Name.ToLowerInvariant().Replace(" ", "-"),
                        Type = ArticleType.Session,
                        IconEmoji = "fa-solid fa-calendar-day",
                        Children = new List<ArticleTreeDto>(),
                        IsVirtualGroup = true,
                        HasAISummary = !string.IsNullOrWhiteSpace(session.AiSummary)
                    };

                    foreach (var note in rootSessionNotes)
                    {
                        sessionNode.Children.Add(note);
                        sessionNode.HasChildren = true;
                        sessionNode.ChildCount++;
                    }

                    arcNode.Children.Add(sessionNode);
                    arcNode.HasChildren = true;
                    arcNode.ChildCount++;
                }

                if (arcNode.Children.Any())
                {
                    campaignNode.Children.Add(arcNode);
                    campaignNode.HasChildren = true;
                    campaignNode.ChildCount++;
                }
            }

            if (campaignNode.Children.Any())
            {
                campaignsGroup.Children!.Add(campaignNode);
                campaignsGroup.HasChildren = true;
                campaignsGroup.ChildCount++;
            }
        }

        if (campaignsGroup.Children!.Any())
        {
            result.Add(campaignsGroup);
        }

        // 2. Player Characters group
        var charactersGroup = CreateVirtualGroup("characters", "Player Characters", "fa-solid fa-user-ninja");
        var characterArticles = allPublicArticles
            .Where(a => a.Type == ArticleType.Character && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var article in characterArticles)
        {
            charactersGroup.Children!.Add(article);
            charactersGroup.HasChildren = true;
            charactersGroup.ChildCount++;
        }

        if (charactersGroup.Children!.Any())
        {
            result.Add(charactersGroup);
        }

        // 3. Wiki group
        var wikiGroup = CreateVirtualGroup("wiki", "Wiki", "fa-solid fa-book");
        var wikiArticles = allPublicArticles
            .Where(a => a.Type == ArticleType.WikiArticle && a.ParentId == null)
            .OrderBy(a => a.Title)
            .ToList();

        foreach (var article in wikiArticles)
        {
            wikiGroup.Children!.Add(article);
            wikiGroup.HasChildren = true;
            wikiGroup.ChildCount++;
        }

        if (wikiGroup.Children!.Any())
        {
            result.Add(wikiGroup);
        }

        // 4. Uncategorized (Legacy and other types without parents not already included)
        var includedIds = new HashSet<Guid>();

        // Collect all IDs from campaigns/arcs/sessions
        foreach (var campaign in result.FirstOrDefault(r => r.Slug == "campaigns")?.Children ?? new List<ArticleTreeDto>())
        {
            foreach (var arc in campaign.Children ?? new List<ArticleTreeDto>())
            {
                foreach (var session in arc.Children ?? new List<ArticleTreeDto>())
                {
                    CollectArticleIds(session, includedIds);
                }
            }
        }

        // Collect character and wiki IDs
        foreach (var article in characterArticles)
        {
            CollectArticleIds(article, includedIds);
        }
        foreach (var article in wikiArticles)
        {
            CollectArticleIds(article, includedIds);
        }

        var uncategorizedArticles = allPublicArticles
            .Where(a => a.ParentId == null &&
                        !includedIds.Contains(a.Id) &&
                        a.Type != ArticleType.Session)
            .OrderBy(a => a.Title)
            .ToList();

        if (uncategorizedArticles.Any())
        {
            var uncategorizedGroup = CreateVirtualGroup("uncategorized", "Uncategorized", "fa-solid fa-folder");
            foreach (var article in uncategorizedArticles)
            {
                uncategorizedGroup.Children!.Add(article);
                uncategorizedGroup.HasChildren = true;
                uncategorizedGroup.ChildCount++;
            }
            result.Add(uncategorizedGroup);
        }

        _logger.LogDebugSanitized("Retrieved {Count} public articles for world '{PublicSlug}' in {GroupCount} groups",
            allPublicArticles.Count, normalizedSlug, result.Count);

        return result;
    }

    private static bool TryGetLegacyPublicSessionArticle(
        IReadOnlyDictionary<Guid, ArticleTreeDto> articleIndex,
        Guid sessionId,
        out ArticleTreeDto? legacySessionArticle)
    {
        if (articleIndex.TryGetValue(sessionId, out var article)
            && article.Type == ArticleType.Session
            && article.ParentId == null
            && article.Visibility == ArticleVisibility.Public)
        {
            legacySessionArticle = article;
            return true;
        }

        legacySessionArticle = null;
        return false;
    }

    private static List<ArticleTreeDto> GetRootSessionNotesForSession(
        IReadOnlyDictionary<Guid, List<ArticleTreeDto>> sessionNotesBySessionId,
        Guid sessionId)
    {
        if (!sessionNotesBySessionId.TryGetValue(sessionId, out var notes))
        {
            return new List<ArticleTreeDto>();
        }

        var sessionNoteIds = notes.Select(n => n.Id).ToHashSet();
        return notes
            .Where(n => !n.ParentId.HasValue || !sessionNoteIds.Contains(n.ParentId.Value))
            .OrderBy(n => n.Title)
            .ToList();
    }

    private static void AttachRootSessionNotesToLegacySessionNode(
        ArticleTreeDto legacySessionArticle,
        IReadOnlyCollection<ArticleTreeDto> rootSessionNotes,
        bool hasAiSummary)
    {
        legacySessionArticle.HasAISummary = hasAiSummary;
        legacySessionArticle.Children ??= new List<ArticleTreeDto>();

        var existingChildIds = legacySessionArticle.Children.Select(c => c.Id).ToHashSet();
        foreach (var note in rootSessionNotes.Where(n => !existingChildIds.Contains(n.Id)))
        {
            legacySessionArticle.Children.Add(note);
        }

        if (legacySessionArticle.Children.Any())
        {
            legacySessionArticle.Children = legacySessionArticle.Children.OrderBy(c => c.Title).ToList();
        }

        legacySessionArticle.HasChildren = legacySessionArticle.Children.Any();
        legacySessionArticle.ChildCount = legacySessionArticle.Children.Count;
    }

    private static ArticleTreeDto CreateVirtualGroup(string slug, string title, string icon)
    {
        return new ArticleTreeDto
        {
            Id = Guid.NewGuid(), // Virtual ID
            Title = title,
            Slug = slug,
            Type = ArticleType.WikiArticle,
            IconEmoji = icon,
            HasChildren = false,
            ChildCount = 0,
            Children = new List<ArticleTreeDto>(),
            IsVirtualGroup = true
        };
    }

    private static void CollectArticleIds(ArticleTreeDto article, HashSet<Guid> ids)
    {
        ids.Add(article.Id);
        if (article.Children != null)
        {
            foreach (var child in article.Children)
            {
                CollectArticleIds(child, ids);
            }
        }
    }

    private async Task<(Guid Id, ArticleType Type)?> TryResolveCompatibilitySessionRootNoteAsync(
        Guid worldId,
        string slug,
        Guid? legacySessionArticleId)
    {
        if (!legacySessionArticleId.HasValue)
        {
            return null;
        }

        var sessionRootNote = await _context.Articles
            .AsNoTracking()
            .Where(a => a.Slug == slug &&
                        a.ParentId == null &&
                        a.SessionId == legacySessionArticleId &&
                        a.WorldId == worldId &&
                        a.Type == ArticleType.SessionNote &&
                        a.Visibility == ArticleVisibility.Public)
            .Select(a => new { a.Id, a.Type })
            .FirstOrDefaultAsync();

        return sessionRootNote == null
            ? null
            : (sessionRootNote.Id, sessionRootNote.Type);
    }

    private async Task<string?> TryGetCompatibilityLegacySessionSlugAsync(Guid worldId, Guid sessionId)
    {
        return await _context.Articles
            .AsNoTracking()
            .Where(a => a.Id == sessionId &&
                        a.WorldId == worldId &&
                        a.Type == ArticleType.Session &&
                        a.Visibility == ArticleVisibility.Public)
            .Select(a => a.Slug)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get a specific article by path in a public world.
    /// Returns null if article doesn't exist, world is not public, or article is not Public visibility.
    /// Path format: "article-slug/child-slug" (does not include world slug)
    /// </summary>
    public async Task<ArticleDto?> GetPublicArticleAsync(string publicSlug, string articlePath)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        // First, verify the world exists and is public
        var world = await _context.Worlds
            .AsNoTracking()
            .Where(w => w.PublicSlug == normalizedSlug && w.IsPublic)
            .Select(w => new { w.Id, w.Name, w.Slug })
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogDebugSanitized("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return null;
        }

        if (string.IsNullOrWhiteSpace(articlePath))
        {
            _logger.LogDebugSanitized("Empty article path for public world '{PublicSlug}'", normalizedSlug);
            return null;
        }

        var slugs = articlePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (slugs.Length == 0)
            return null;

        // Walk down the tree using slugs
        Guid? currentParentId = null;
        Guid? articleId = null;
        ArticleType? currentArticleType = null;

        for (int i = 0; i < slugs.Length; i++)
        {
            var slug = slugs[i];
            var isRootLevel = (i == 0);

            (Guid Id, ArticleType Type)? foundArticle = null;

            if (isRootLevel)
            {
                // Root-level article: filter by WorldId and ParentId = null
                var rootArticle = await _context.Articles
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.ParentId == null &&
                                a.WorldId == world.Id &&
                                a.Visibility == ArticleVisibility.Public)
                    .Select(a => new { a.Id, a.Type })
                    .FirstOrDefaultAsync();
                if (rootArticle != null)
                {
                    foundArticle = (rootArticle.Id, rootArticle.Type);
                }
            }
            else
            {
                // Child article: filter by ParentId
                var childArticle = await _context.Articles
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.ParentId == currentParentId &&
                                a.WorldId == world.Id &&
                                a.Visibility == ArticleVisibility.Public)
                    .Select(a => new { a.Id, a.Type })
                    .FirstOrDefaultAsync();
                if (childArticle != null)
                {
                    foundArticle = (childArticle.Id, childArticle.Type);
                }

                if (foundArticle == null && currentArticleType == ArticleType.Session)
                {
                    foundArticle = await TryResolveCompatibilitySessionRootNoteAsync(
                        world.Id,
                        slug,
                        currentParentId);
                }
            }

            if (foundArticle == null)
            {
                _logger.LogDebugSanitized("Public article not found for slug '{Slug}' in path '{Path}' for world '{PublicSlug}'",
                    slug, articlePath, normalizedSlug);
                return null;
            }

            articleId = foundArticle.Value.Id;
            currentParentId = foundArticle.Value.Id;
            currentArticleType = foundArticle.Value.Type;
        }

        // Found the article, now get full details
        if (!articleId.HasValue)
            return null;

        var article = await _context.Articles
            .AsNoTracking()
            .Where(a => a.Id == articleId.Value &&
                        a.WorldId == world.Id &&
                        a.Visibility == ArticleVisibility.Public)
            .Select(a => new ArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                ParentId = a.ParentId,
                WorldId = a.WorldId,
                CampaignId = a.CampaignId,
                ArcId = a.ArcId,
                SessionId = a.SessionId,
                Body = a.Body ?? string.Empty,
                Type = a.Type,
                Visibility = a.Visibility,
                CreatedAt = a.CreatedAt,
                ModifiedAt = a.ModifiedAt,
                EffectiveDate = a.EffectiveDate,
                CreatedBy = a.CreatedBy,
                LastModifiedBy = a.LastModifiedBy,
                IconEmoji = a.IconEmoji,
                SessionDate = a.SessionDate,
                InGameDate = a.InGameDate,
                PlayerId = a.PlayerId,
                AISummary = a.AISummary,
                AISummaryGeneratedAt = a.AISummaryGeneratedAt,
                Breadcrumbs = new List<BreadcrumbDto>()
            })
            .FirstOrDefaultAsync();

        if (article == null)
            return null;

        // Build breadcrumbs (only including public articles) using centralised hierarchy service
        article.Breadcrumbs = await _hierarchyService.BuildBreadcrumbsAsync(articleId.Value, new HierarchyWalkOptions
        {
            PublicOnly = true,
            IncludeWorldBreadcrumb = true,
            IncludeVirtualGroups = true,
            World = new WorldContext { Id = world.Id, Name = world.Name, Slug = world.Slug }
        });

        _logger.LogDebugSanitized("Public article '{Title}' accessed in world '{PublicSlug}'",
            article.Title, normalizedSlug);

        return article;
    }

    /// <summary>
    /// Resolve an article ID to its public URL path.
    /// Returns null if the article doesn't exist, is not public, or doesn't belong to the specified world.
    /// </summary>
    public async Task<string?> GetPublicArticlePathAsync(string publicSlug, Guid articleId)
    {
        var normalizedSlug = publicSlug.Trim().ToLowerInvariant();

        // Verify the world exists and is public
        var world = await _context.Worlds
            .AsNoTracking()
            .Where(w => w.PublicSlug == normalizedSlug && w.IsPublic)
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync();

        if (world == null)
        {
            _logger.LogDebugSanitized("Public world not found for slug '{PublicSlug}'", normalizedSlug);
            return null;
        }

        // Get the article and verify it's public and belongs to this world
        var article = await _context.Articles
            .AsNoTracking()
            .Where(a => a.Id == articleId &&
                        a.WorldId == world.Id &&
                        a.Visibility == ArticleVisibility.Public)
            .Select(a => new { a.Id, a.Slug, a.ParentId, a.SessionId, a.Type })
            .FirstOrDefaultAsync();

        if (article == null)
        {
            _logger.LogDebugSanitized("Public article {ArticleId} not found in world '{PublicSlug}'", articleId, normalizedSlug);
            return null;
        }

        // Build the path by walking up the parent tree.
        // Root SessionNotes can belong to a Session entity while having ParentId = null.
        // In that case, prefer including the matching legacy Session article slug
        // (when one exists) so links are stable with older public URLs.
        var slugs = new List<string> { article.Slug };

        if (article.Type == ArticleType.SessionNote &&
            !article.ParentId.HasValue &&
            article.SessionId.HasValue)
        {
            var legacySessionArticleSlug = await TryGetCompatibilityLegacySessionSlugAsync(
                world.Id,
                article.SessionId.Value);

            if (!string.IsNullOrEmpty(legacySessionArticleSlug))
            {
                slugs.Insert(0, legacySessionArticleSlug);
            }
        }

        var currentParentId = article.ParentId;

        while (currentParentId.HasValue)
        {
            var parentArticle = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Id == currentParentId.Value &&
                            a.WorldId == world.Id &&
                            a.Visibility == ArticleVisibility.Public)
                .Select(a => new { a.Slug, a.ParentId })
                .FirstOrDefaultAsync();

            if (parentArticle == null)
            {
                // Parent is not public - this article's path is broken
                _logger.LogDebug("Parent article not public in chain for article {ArticleId}", articleId);
                return null;
            }

            slugs.Insert(0, parentArticle.Slug);
            currentParentId = parentArticle.ParentId;
        }

        var path = string.Join("/", slugs);
        _logger.LogDebugSanitized("Resolved article {ArticleId} to path '{Path}' in world '{PublicSlug}'",
            articleId, path, normalizedSlug);

        return path;
    }

    /// <summary>
    /// Resolve a public inline-image document ID to a fresh download URL.
    /// The document must be an image attached to a public article in a public world.
    /// </summary>
    public async Task<string?> GetPublicDocumentDownloadUrlAsync(Guid documentId)
    {
        var document = await _context.WorldDocuments
            .AsNoTracking()
            .Where(d => d.Id == documentId
                        && d.ArticleId.HasValue
                        && d.ContentType.StartsWith("image/"))
            .Join(
                _context.Articles.AsNoTracking(),
                d => d.ArticleId,
                a => a.Id,
                (d, a) => new
                {
                    d.BlobPath,
                    d.WorldId,
                    ArticleWorldId = a.WorldId,
                    a.Visibility
                })
            .Join(
                _context.Worlds.AsNoTracking().Where(w => w.IsPublic),
                joined => joined.WorldId,
                w => w.Id,
                (joined, _) => joined)
            .FirstOrDefaultAsync();

        if (document == null
            || document.Visibility != ArticleVisibility.Public
            || document.ArticleWorldId != document.WorldId)
        {
            return null;
        }

        return await _blobStorage.GenerateDownloadSasUrlAsync(document.BlobPath);
    }
}
