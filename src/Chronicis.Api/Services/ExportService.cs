using System.IO.Compression;
using Chronicis.Api.Data;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for exporting world data to downloadable archives
/// </summary>
public partial class ExportService : IExportService
{
    private readonly ChronicisDbContext _db;
    private readonly IWorldMembershipService _membershipService;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        ChronicisDbContext db,
        IWorldMembershipService membershipService,
        ILogger<ExportService> logger)
    {
        _db = db;
        _membershipService = membershipService;
        _logger = logger;
    }

    public async Task<byte[]?> ExportWorldToMarkdownAsync(Guid worldId, Guid userId)
    {
        // Verify user has access to the world
        if (!await _membershipService.UserHasAccessAsync(worldId, userId))
        {
            _logger.LogWarning("User {UserId} attempted to export world {WorldId} without access", userId, worldId);
            return null;
        }

        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId);

        if (world == null)
        {
            _logger.LogWarning("World {WorldId} not found for export", worldId);
            return null;
        }

        _logger.LogDebug("Starting export of world {WorldId} ({WorldName}) for user {UserId}",
            worldId, world.Name, userId);

        // Get all articles for this world with their hierarchy info
        var articles = await _db.Articles
            .AsNoTracking()
            .Where(a => a.WorldId == worldId)
            .OrderBy(a => a.Title)
            .ToListAsync();

        // Get campaigns and arcs for building folder structure
        var campaigns = await _db.Campaigns
            .AsNoTracking()
            .Where(c => c.WorldId == worldId)
            .ToListAsync();

        var arcs = await _db.Arcs
            .AsNoTracking()
            .Where(a => campaigns.Select(c => c.Id).Contains(a.CampaignId))
            .ToListAsync();

        var arcIds = arcs.Select(a => a.Id).ToList();
        var sessionEntities = await _db.Sessions
            .AsNoTracking()
            .Where(s => arcIds.Contains(s.ArcId))
            .ToListAsync();

        // Build the zip archive
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var worldFolderName = SanitizeFileName(world.Name);

            // Export wiki articles (hierarchical)
            var wikiArticles = articles.Where(a => a.Type == ArticleType.WikiArticle || a.Type == ArticleType.Legacy).ToList();
            await ExportArticleHierarchy(archive, wikiArticles, $"{worldFolderName}/Wiki", null);

            // Export characters
            var characters = articles.Where(a => a.Type == ArticleType.Character).ToList();
            await ExportArticleHierarchy(archive, characters, $"{worldFolderName}/Characters", null);

            // Export campaigns with their sessions
            var usedCampaignNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var campaign in campaigns)
            {
                var campaignFolderName = GetUniqueSiblingName(campaign.Name, usedCampaignNames);
                var campaignFolder = $"{worldFolderName}/Campaigns/{campaignFolderName}";

                // Campaign info file
                var campaignContent = BuildCampaignMarkdown(campaign);
                await AddFileToArchive(archive, $"{campaignFolder}/{campaignFolderName}.md", campaignContent);

                // Export arcs and sessions
                var campaignArcs = arcs.Where(a => a.CampaignId == campaign.Id).OrderBy(a => a.SortOrder).ToList();
                var usedArcNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var arc in campaignArcs)
                {
                    var arcFolderName = GetUniqueSiblingName(arc.Name, usedArcNames);
                    var arcFolder = $"{campaignFolder}/{arcFolderName}";

                    // Arc info file
                    var arcContent = BuildArcMarkdown(arc);
                    await AddFileToArchive(archive, $"{arcFolder}/{arcFolderName}.md", arcContent);

                    // Session entities in this arc
                    var arcSessions = sessionEntities
                        .Where(s => s.ArcId == arc.Id)
                        .OrderBy(s => s.SessionDate ?? s.CreatedAt)
                        .ThenBy(s => s.Name)
                        .ToList();

                    var usedSessionFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var session in arcSessions)
                    {
                        var sessionFolderName = GetUniqueSiblingName(session.Name, usedSessionFolderNames);
                        var sessionContent = BuildSessionMarkdown(session);
                        await AddFileToArchive(archive, $"{arcFolder}/{sessionFolderName}/{sessionFolderName}.md", sessionContent);

                        // Session notes attached via Article.SessionId
                        var sessionNotes = articles
                            .Where(a => a.SessionId == session.Id && a.Type == ArticleType.SessionNote)
                            .OrderBy(a => a.CreatedAt)
                            .ThenBy(a => a.Title)
                            .ToList();

                        var usedSessionNoteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var note in sessionNotes)
                        {
                            var noteContent = BuildArticleMarkdown(note);
                            var noteFileName = GetUniqueSiblingName(note.Title, usedSessionNoteNames);
                            await AddFileToArchive(archive, $"{arcFolder}/{sessionFolderName}/{noteFileName}.md", noteContent);
                        }
                    }
                }
            }
        }

        memoryStream.Position = 0;
        var result = memoryStream.ToArray();

        _logger.LogDebug("Export completed for world {WorldId}. Archive size: {Size} bytes",
            worldId, result.Length);

        return result;
    }

    private async Task ExportArticleHierarchy(
        ZipArchive archive,
        List<Article> articles,
        string basePath,
        Guid? parentId)
    {
        var children = articles.Where(a => a.ParentId == parentId).OrderBy(a => a.Title).ToList();
        var usedSiblingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var article in children)
        {
            var articleFileName = GetUniqueSiblingName(article.Title, usedSiblingNames);
            var hasChildren = articles.Any(a => a.ParentId == article.Id);

            if (hasChildren)
            {
                // Article with children: ArticleName/ArticleName.md
                var articleFolder = $"{basePath}/{articleFileName}";
                var content = BuildArticleMarkdown(article);
                await AddFileToArchive(archive, $"{articleFolder}/{articleFileName}.md", content);

                // Recursively export children
                await ExportArticleHierarchy(archive, articles, articleFolder, article.Id);
            }
            else
            {
                // Leaf article: ArticleName.md
                var content = BuildArticleMarkdown(article);
                await AddFileToArchive(archive, $"{basePath}/{articleFileName}.md", content);
            }
        }
    }

}
