using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services
{
    public sealed class ArticleService : IArticleService
    {
        // Compiled queries for hot-path article tree operations.
        // Access policy filters are inlined from ReadAccessPolicyService.ApplyAuthenticatedWorldArticleFilter.
        private static readonly Func<ChronicisDbContext, Guid, IAsyncEnumerable<ArticleTreeDto>>
            GetRootArticlesQuery = EF.CompileAsyncQuery<ChronicisDbContext, Guid, ArticleTreeDto>(
                (ChronicisDbContext ctx, Guid userId) => ctx.Articles
                    .AsNoTracking()
                    .Where(a => a.Type != ArticleType.Tutorial && a.WorldId != Guid.Empty)
                    .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == userId))
                    .Where(a => a.Visibility != ArticleVisibility.Private || a.CreatedBy == userId)
                    .Where(a => a.ParentId == null)
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
                        ChildCount = ctx.Articles.Count(c => c.ParentId == a.Id),
                        Children = new List<ArticleTreeDto>(),
                        CreatedAt = a.CreatedAt,
                        EffectiveDate = a.EffectiveDate,
                        IconEmoji = a.IconEmoji,
                        CreatedBy = a.CreatedBy,
                        HasAISummary = a.AISummary != null
                    })
                    .OrderBy(a => a.Title));

        private static readonly Func<ChronicisDbContext, Guid, Guid, IAsyncEnumerable<ArticleTreeDto>>
            GetRootArticlesInWorldQuery = EF.CompileAsyncQuery<ChronicisDbContext, Guid, Guid, ArticleTreeDto>(
                (ChronicisDbContext ctx, Guid userId, Guid worldId) => ctx.Articles
                    .AsNoTracking()
                    .Where(a => a.Type != ArticleType.Tutorial && a.WorldId != Guid.Empty)
                    .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == userId))
                    .Where(a => a.Visibility != ArticleVisibility.Private || a.CreatedBy == userId)
                    .Where(a => a.ParentId == null && a.WorldId == worldId)
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
                        ChildCount = ctx.Articles.Count(c => c.ParentId == a.Id),
                        Children = new List<ArticleTreeDto>(),
                        CreatedAt = a.CreatedAt,
                        EffectiveDate = a.EffectiveDate,
                        IconEmoji = a.IconEmoji,
                        CreatedBy = a.CreatedBy,
                        HasAISummary = a.AISummary != null
                    })
                    .OrderBy(a => a.Title));

        private static readonly Func<ChronicisDbContext, Guid, Guid, IAsyncEnumerable<ArticleTreeDto>>
            GetChildrenCompiledQuery = EF.CompileAsyncQuery<ChronicisDbContext, Guid, Guid, ArticleTreeDto>(
                (ChronicisDbContext ctx, Guid parentId, Guid userId) => ctx.Articles
                    .AsNoTracking()
                    .Where(a => a.Type != ArticleType.Tutorial && a.WorldId != Guid.Empty)
                    .Where(a => a.World != null && a.World.Members.Any(m => m.UserId == userId))
                    .Where(a => a.Visibility != ArticleVisibility.Private || a.CreatedBy == userId)
                    .Where(a => a.ParentId == parentId)
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
                        ChildCount = ctx.Articles.Count(c => c.ParentId == a.Id),
                        Children = new List<ArticleTreeDto>(),
                        CreatedAt = a.CreatedAt,
                        EffectiveDate = a.EffectiveDate,
                        IconEmoji = a.IconEmoji,
                        CreatedBy = a.CreatedBy,
                        HasAISummary = a.AISummary != null
                    })
                    .OrderBy(a => a.Title));

        private readonly ChronicisDbContext _context;
        private readonly ILogger<ArticleService> _logger;
        private readonly IArticleHierarchyService _hierarchyService;
        private readonly IReadAccessPolicyService _readAccessPolicy;

        public ArticleService(
            ChronicisDbContext context,
            ILogger<ArticleService> logger,
            IArticleHierarchyService hierarchyService,
            IReadAccessPolicyService readAccessPolicy)
        {
            _context = context;
            _logger = logger;
            _hierarchyService = hierarchyService;
            _readAccessPolicy = readAccessPolicy;
        }

        /// <summary>
        /// Gets world-scoped articles the user can access via WorldMembers.
        /// Tutorial/system articles are explicitly excluded from this query.
        /// Private articles are only visible to their creator.
        /// </summary>
        private IQueryable<Article> GetAccessibleArticles(Guid userId)
        {
            return _readAccessPolicy.ApplyAuthenticatedWorldArticleFilter(_context.Articles, userId);
        }

        /// <summary>
        /// Gets articles readable by an authenticated user, including global tutorial articles.
        /// </summary>
        private IQueryable<Article> GetReadableArticles(Guid userId)
        {
            return _readAccessPolicy.ApplyAuthenticatedReadableArticleFilter(_context.Articles, userId);
        }

        /// <summary>
        /// Get all root-level articles (ParentId is null) for worlds the user has access to.
        /// Optionally filter by WorldId.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetRootArticlesAsync(Guid userId, Guid? worldId = null)
        {
            var compiled = worldId.HasValue
                ? GetRootArticlesInWorldQuery(_context, userId, worldId.Value)
                : GetRootArticlesQuery(_context, userId);

            var rootArticles = new List<ArticleTreeDto>();
            await foreach (var a in compiled)
            {
                a.HasChildren = a.ChildCount > 0;
                rootArticles.Add(a);
            }

            return rootArticles;
        }

        /// <summary>
        /// Get all articles for worlds the user has access to, in a flat list (no hierarchy).
        /// Useful for dropdowns, linking dialogs, etc.
        /// Optionally filter by WorldId.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetAllArticlesAsync(Guid userId, Guid? worldId = null)
        {
            var query = GetAccessibleArticles(userId).AsNoTracking();

            if (worldId.HasValue)
            {
                query = query.Where(a => a.WorldId == worldId.Value);
            }

            var articles = await query
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
                    HasChildren = false, // Not relevant in flat list
                    ChildCount = 0,      // Not relevant in flat list
                    Children = new List<ArticleTreeDto>(),
                    CreatedAt = a.CreatedAt,
                    EffectiveDate = a.EffectiveDate,
                    IconEmoji = a.IconEmoji,
                    CreatedBy = a.CreatedBy,
                    HasAISummary = a.AISummary != null
                    // Note: Aliases intentionally not loaded here - loaded separately via GetLinkSuggestions
                })
                .OrderBy(a => a.Title)
                .ToListAsync();

            return articles;
        }

        /// <summary>
        /// Get all child articles of a specific parent.
        /// User must have access to the article's world via WorldMembers.
        /// </summary>
        public async Task<List<ArticleTreeDto>> GetChildrenAsync(Guid parentId, Guid userId)
        {
            var children = new List<ArticleTreeDto>();
            await foreach (var a in GetChildrenCompiledQuery(_context, parentId, userId))
            {
                a.HasChildren = a.ChildCount > 0;
                children.Add(a);
            }

            return children;
        }

        /// <summary>
        /// Get full article details including breadcrumb path from root.
        /// User must have access to the article's world via WorldMembers.
        /// </summary>
        public async Task<ArticleDto?> GetArticleDetailAsync(Guid id, Guid userId)
        {
            var article = await GetReadableArticles(userId)
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(ArticleReadModelProjection.ArticleDetail)
                .FirstOrDefaultAsync();

            if (article == null)
            {
                _logger.LogWarningSanitized("Article {ArticleId} not found", id);
                return null;
            }

            article.Aliases = await _context.ArticleAliases
                .AsNoTracking()
                .Where(al => al.ArticleId == id)
                .Select(al => new ArticleAliasDto
                {
                    Id = al.Id,
                    AliasText = al.AliasText,
                    AliasType = al.AliasType,
                    EffectiveDate = al.EffectiveDate,
                    CreatedAt = al.CreatedAt
                })
                .ToListAsync();

            // Build breadcrumb path using centralised hierarchy service.
            // Virtual groups (Wiki / Player Characters / Campaign-Arc-Session) are required
            // for the breadcrumb chain to produce valid URL paths under the slug routing scheme.
            HierarchyWalkOptions? options = null;
            if (article.WorldId.HasValue && article.WorldId.Value != Guid.Empty)
            {
                var world = await _context.Worlds
                    .AsNoTracking()
                    .Where(w => w.Id == article.WorldId.Value)
                    .Select(w => new WorldContext { Id = w.Id, Name = w.Name, Slug = w.Slug })
                    .FirstOrDefaultAsync();

                if (world != null)
                {
                    options = new HierarchyWalkOptions
                    {
                        IncludeVirtualGroups = true,
                        World = world
                    };
                }
            }

            article.Breadcrumbs = await _hierarchyService.BuildBreadcrumbsAsync(id, options);

            return article;
        }

        /// <summary>
        /// Move an article to a new parent (or to root if newParentId is null).
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> MoveArticleAsync(Guid articleId, Guid? newParentId, Guid? newSessionId, Guid userId)
        {
            // 1. Get the article to move (must be in a world user has access to)
            var article = await GetAccessibleArticles(userId)
                .FirstOrDefaultAsync(a => a.Id == articleId);

            if (article == null)
            {
                _logger.LogWarningSanitized("Article {ArticleId} not found for user {UserId}", articleId, userId);
                return (false, "Article not found");
            }

            var sessionChangeRequested = newSessionId.HasValue;

            // 2. If moving to same parent (and same session when requested), nothing to do
            if (article.ParentId == newParentId &&
                (!sessionChangeRequested || article.SessionId == newSessionId))
            {
                return (true, null);
            }

            // 3. If newParentId is specified, validate the target exists and user has access
            Article? targetParent = null;
            if (newParentId.HasValue)
            {
                targetParent = await GetAccessibleArticles(userId)
                    .FirstOrDefaultAsync(a => a.Id == newParentId.Value);

                if (targetParent == null)
                {
                    _logger.LogWarningSanitized("Target parent {NewParentId} not found for user {UserId}", newParentId, userId);
                    return (false, "Target parent article not found");
                }

                // 4. Check for circular reference - cannot move an article to be a child of itself or its descendants
                if (await WouldCreateCircularReferenceAsync(articleId, newParentId.Value, userId))
                {
                    _logger.LogWarningSanitized("Moving article {ArticleId} to {NewParentId} would create circular reference",
                        articleId, newParentId);
                    return (false, "Cannot move an article to be a child of itself or its descendants");
                }
            }

            Session? targetSession = null;
            if (sessionChangeRequested)
            {
                if (article.Type != ArticleType.SessionNote)
                {
                    return (false, "Only SessionNote articles can be attached to sessions");
                }

                targetSession = await _context.Sessions
                    .Include(s => s.Arc)
                        .ThenInclude(a => a.Campaign)
                            .ThenInclude(c => c.World)
                                .ThenInclude(w => w.Members)
                    .FirstOrDefaultAsync(s => s.Id == newSessionId!.Value);

                if (targetSession == null)
                {
                    return (false, "Target session not found");
                }

                if (!targetSession.Arc.Campaign.World.Members.Any(m => m.UserId == userId))
                {
                    return (false, "Target session not found or access denied");
                }

                if (article.WorldId != targetSession.Arc.Campaign.WorldId)
                {
                    return (false, "Cannot move articles between different worlds");
                }

                if (targetParent != null && targetParent.SessionId != targetSession.Id)
                {
                    return (false, "Target parent must belong to the selected session");
                }
            }

            // 5. Perform the move
            article.ParentId = newParentId;

            if (targetSession != null)
            {
                await ReassignSessionContextForSubtreeAsync(
                    articleId,
                    targetSession.Id,
                    targetSession.ArcId,
                    targetSession.Arc.CampaignId);
            }

            article.ModifiedAt = DateTime.UtcNow;
            article.LastModifiedBy = userId;

            await _context.SaveChangesAsync();

            return (true, null);
        }

        /// <summary>
        /// Check if moving articleId to become a child of targetParentId would create a circular reference.
        /// </summary>
        private async Task<bool> WouldCreateCircularReferenceAsync(Guid articleId, Guid targetParentId, Guid userId)
        {
            // If trying to move to self, that's circular
            if (articleId == targetParentId)
            {
                return true;
            }

            // Walk up from targetParentId to root, checking if we encounter articleId
            var currentId = (Guid?)targetParentId;
            var visited = new HashSet<Guid>();

            while (currentId.HasValue)
            {
                // Prevent infinite loops (shouldn't happen with valid data, but safety first)
                if (visited.Contains(currentId.Value))
                {
                    _logger.LogErrorSanitized("Detected existing circular reference in hierarchy at article {ArticleId}", currentId.Value);
                    return true;
                }
                visited.Add(currentId.Value);

                // If we find the article we're trying to move in the ancestor chain, it's circular
                if (currentId.Value == articleId)
                {
                    return true;
                }

                // Move up to parent
                var parent = await GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.Id == currentId.Value)
                    .Select(a => new { a.ParentId })
                    .FirstOrDefaultAsync();

                currentId = parent?.ParentId;
            }

            return false;
        }

        /// <summary>
        /// Reassigns Session/Acr/Campaign context for a SessionNote subtree when moving between sessions.
        /// </summary>
        private async Task ReassignSessionContextForSubtreeAsync(
            Guid rootArticleId,
            Guid targetSessionId,
            Guid targetArcId,
            Guid targetCampaignId)
        {
            var queue = new Queue<Guid>();
            var visited = new HashSet<Guid>();
            queue.Enqueue(rootArticleId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                if (!visited.Add(currentId))
                {
                    continue;
                }

                var current = await _context.Articles.FirstOrDefaultAsync(a => a.Id == currentId);
                if (current == null)
                {
                    continue;
                }

                if (current.Type == ArticleType.SessionNote)
                {
                    current.SessionId = targetSessionId;
                    current.ArcId = targetArcId;
                    current.CampaignId = targetCampaignId;
                }

                var childIds = await _context.Articles
                    .Where(a => a.ParentId == currentId)
                    .Select(a => a.Id)
                    .ToListAsync();

                foreach (var childId in childIds)
                {
                    queue.Enqueue(childId);
                }
            }
        }



        /// <summary>
        /// Get article by hierarchical path.
        /// Path format: "world-slug/article-slug/child-slug" (e.g., "stormlight/wiki/characters").
        /// The first segment is the world slug, remaining segments are article hierarchy.
        /// </summary>
        public async Task<ArticleDto?> GetArticleByPathAsync(string path, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var slugs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (slugs.Length == 0)
                return null;

            // Normal paths are world-scoped ("world-slug/article-slug/..."), but tutorial
            // articles are global/system content (WorldId == Guid.Empty) and arrive as
            // "tutorial-slug[/child-slug]". Try world resolution first, then tutorial fallback.
            var article = await TryResolveWorldArticleByPathAsync(slugs, userId)
                ?? await TryResolveTutorialArticleByPathAsync(slugs, userId);

            if (article == null && slugs.Length > 1)
            {
                // Some tutorial URLs include a synthetic "system tutorial world" prefix
                // (e.g. /article/system-tutorial/tutorial-article-any). Tutorials are
                // stored as global/system articles, so retry after stripping that prefix.
                article = await TryResolveTutorialArticleByPathAsync(slugs.Skip(1).ToArray(), userId);
            }

            if (article == null)
            {
                _logger.LogWarningSanitized("Article not found for path '{Path}' for user {UserId}", path, userId);
            }

            return article;
        }

        /// <summary>
        /// Check if a slug is unique among siblings.
        /// Session notes (with sessionId) are scoped to (SessionId, Slug).
        /// Child articles are scoped to (ParentId, Slug).
        /// Root non-session-note articles are scoped to (WorldId, Slug).
        /// </summary>
        public async Task<bool> IsSlugUniqueAsync(string slug, Guid? parentId, Guid? worldId, Guid userId, Guid? excludeArticleId = null, ArticleType articleType = ArticleType.WikiArticle, Guid? sessionId = null)
        {
            IQueryable<Article> query;

            if (articleType == ArticleType.SessionNote && sessionId.HasValue && !parentId.HasValue)
            {
                // Root session note: unique within (SessionId, Slug)
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.Type == ArticleType.SessionNote &&
                                a.SessionId == sessionId.Value &&
                                a.ParentId == null);
            }
            else if (parentId.HasValue)
            {
                // Child article: unique among siblings with same parent
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.ParentId == parentId);
            }
            else
            {
                // Root article: unique among root articles in the same world
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.Slug == slug &&
                                a.ParentId == null &&
                                a.WorldId == worldId);
            }

            if (excludeArticleId.HasValue)
            {
                query = query.Where(a => a.Id != excludeArticleId.Value);
            }

            return !await query.AnyAsync();
        }

        /// <summary>
        /// Generate a unique slug for an article among its siblings.
        /// Session notes (with sessionId) are scoped to (SessionId, Slug).
        /// Child articles are scoped to (ParentId, Slug).
        /// Root non-session-note articles are scoped to (WorldId, Slug).
        /// </summary>
        public async Task<string> GenerateUniqueSlugAsync(string title, Guid? parentId, Guid? worldId, Guid userId, Guid? excludeArticleId = null, ArticleType articleType = ArticleType.WikiArticle, Guid? sessionId = null)
        {
            var baseSlug = SlugGenerator.GenerateSlug(title);

            IQueryable<Article> query;

            if (articleType == ArticleType.SessionNote && sessionId.HasValue && !parentId.HasValue)
            {
                // Root session note: get slugs from session notes in the same session
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.Type == ArticleType.SessionNote &&
                                a.SessionId == sessionId.Value &&
                                a.ParentId == null);
            }
            else if (parentId.HasValue)
            {
                // Child article: get slugs from siblings with same parent
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.ParentId == parentId);
            }
            else
            {
                // Root article: get slugs from root articles in the same world
                query = GetAccessibleArticles(userId)
                    .AsNoTracking()
                    .Where(a => a.ParentId == null && a.WorldId == worldId);
            }

            var existingSlugs = await query
                .Where(a => !excludeArticleId.HasValue || a.Id != excludeArticleId.Value)
                .Select(a => a.Slug)
                .ToHashSetAsync();

            return SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);
        }

        /// <summary>
        /// Build the full hierarchical path for an article, including world slug.
        /// Returns format: "world-slug/article-slug/child-slug"
        /// </summary>
        public async Task<string> BuildArticlePathAsync(Guid articleId, Guid userId)
        {
            return await _hierarchyService.BuildPathAsync(articleId);
        }

        public async Task<(Guid ArticleId, IReadOnlyList<(string Slug, string Title)> PathBreadcrumbs)?> ResolveWorldArticlePathAsync(
            Guid worldId,
            IReadOnlyList<string> slugs,
            Guid? userId,
            CancellationToken cancellationToken = default)
        {
            if (slugs.Count == 0)
                return null;

            var breadcrumbs = new List<(string Slug, string Title)>();

            Func<string, Guid?, bool, Task<(Guid Id, ArticleType Type)?>> resolveSegment;

            if (userId.HasValue)
            {
                var uid = userId.Value;
                resolveSegment = async (slug, parentId, isRootLevel) =>
                {
                    var query = GetAccessibleArticles(uid)
                        .AsNoTracking()
                        .Where(a => a.Slug == slug);

                    query = isRootLevel
                        ? query.Where(a => a.ParentId == null && a.WorldId == worldId)
                        : query.Where(a => a.ParentId == parentId);

                    var article = await query.Select(a => new { a.Id, a.Type, a.Title }).FirstOrDefaultAsync(cancellationToken);
                    if (article != null)
                        breadcrumbs.Add((slug, article.Title ?? slug));

                    return article == null ? null : (article.Id, article.Type);
                };
            }
            else
            {
                resolveSegment = async (slug, parentId, isRootLevel) =>
                {
                    var query = _context.Articles.AsNoTracking()
                        .Where(a => a.Slug == slug
                                    && a.Visibility == Chronicis.Shared.Enums.ArticleVisibility.Public
                                    && a.WorldId == worldId);

                    query = isRootLevel
                        ? query.Where(a => a.ParentId == null)
                        : query.Where(a => a.ParentId == parentId);

                    var article = await query.Select(a => new { a.Id, a.Type, a.Title }).FirstOrDefaultAsync(cancellationToken);
                    if (article != null)
                        breadcrumbs.Add((slug, article.Title ?? slug));

                    return article == null ? null : (article.Id, article.Type);
                };
            }

            var resolved = await ArticleSlugPathResolver.ResolveAsync(slugs, resolveSegment);
            return resolved.HasValue
                ? (resolved.Value.Id, (IReadOnlyList<(string, string)>)breadcrumbs)
                : null;
        }

        public async Task<(Guid ArticleId, string Title)?> GetSessionNoteBySlugAsync(
            Guid sessionId,
            string slug,
            Guid? userId,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Article> query = _context.Articles
                .AsNoTracking()
                .Where(a => a.SessionId == sessionId
                            && a.Type == Chronicis.Shared.Enums.ArticleType.SessionNote
                            && a.Slug == slug);

            if (userId.HasValue)
            {
                query = _readAccessPolicy.ApplyAuthenticatedWorldArticleFilter(query, userId.Value);
            }
            else
            {
                query = _readAccessPolicy.ApplyPublicVisibilityFilter(query);
            }

            var article = await query
                .Select(a => new { a.Id, a.Title })
                .FirstOrDefaultAsync(cancellationToken);

            return article == null ? null : (article.Id, article.Title ?? slug);
        }

        public async Task<(Guid ArticleId, string Title)?> GetTutorialBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            var article = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Slug == slug
                            && a.Type == Chronicis.Shared.Enums.ArticleType.Tutorial
                            && a.WorldId == Guid.Empty)
                .Select(a => new { a.Id, a.Title })
                .FirstOrDefaultAsync(cancellationToken);

            return article == null ? null : (article.Id, article.Title ?? slug);
        }

        private async Task<ArticleDto?> TryResolveWorldArticleByPathAsync(string[] slugs, Guid userId)
        {
            if (slugs.Length < 2)
            {
                return null;
            }

            var worldSlug = slugs[0];

            var world = _context.Worlds
                .AsNoTracking();

            var resolvedWorld = await _readAccessPolicy
                .ApplyAuthenticatedWorldFilter(world, userId)
                .Where(w => w.Slug == worldSlug)
                .Select(w => new { w.Id })
                .FirstOrDefaultAsync();

            if (resolvedWorld == null)
            {
                return null;
            }

            var resolvedArticle = await ArticleSlugPathResolver.ResolveAsync(
                slugs.Skip(1).ToArray(),
                async (slug, parentId, isRootLevel) =>
                {
                    var query = GetAccessibleArticles(userId)
                        .AsNoTracking()
                        .Where(a => a.Slug == slug);

                    query = isRootLevel
                        ? query.Where(a => a.ParentId == null && a.WorldId == resolvedWorld.Id)
                        : query.Where(a => a.ParentId == parentId);

                    var article = await query
                        .Select(a => new { a.Id, a.Type })
                        .FirstOrDefaultAsync();

                    return article == null
                        ? null
                        : (article.Id, article.Type);
                });

            return resolvedArticle.HasValue
                ? await GetArticleDetailAsync(resolvedArticle.Value.Id, userId)
                : null;
        }

        private async Task<ArticleDto?> TryResolveTutorialArticleByPathAsync(string[] slugs, Guid userId)
        {
            var resolvedArticle = await ArticleSlugPathResolver.ResolveAsync(
                slugs,
                async (slug, parentId, isRootLevel) =>
                {
                    var query = _context.Articles
                        .AsNoTracking()
                        .Where(a => a.Type == ArticleType.Tutorial &&
                                    a.WorldId == Guid.Empty &&
                                    a.Slug == slug);

                    query = isRootLevel
                        ? query.Where(a => a.ParentId == null)
                        : query.Where(a => a.ParentId == parentId);

                    var article = await query
                        .Select(a => new { a.Id, a.Type })
                        .FirstOrDefaultAsync();

                    return article == null
                        ? null
                        : (article.Id, article.Type);
                });

            return resolvedArticle.HasValue
                ? await GetArticleDetailAsync(resolvedArticle.Value.Id, userId)
                : null;
        }
    }
}
