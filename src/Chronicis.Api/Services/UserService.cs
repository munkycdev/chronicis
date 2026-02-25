using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Chronicis.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Implementation of user management service
/// </summary>
public class UserService : IUserService
{
    private static readonly Guid TutorialTemplateWorldId = new("bbcee097-e733-4c55-a72b-91fa2cfa0391");

    private readonly ChronicisDbContext _context;
    private readonly IWorldService _worldService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ChronicisDbContext context,
        IWorldService worldService,
        ILogger<UserService> logger)
    {
        _context = context;
        _worldService = worldService;
        _logger = logger;
    }

    public async Task<User> GetOrCreateUserAsync(
        string auth0UserId,
        string email,
        string displayName,
        string? avatarUrl)
    {
        // Try to find existing user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId);

        if (user == null)
        {
            // Create new user
            user = new User
            {
                Id = Guid.NewGuid(),
                Auth0UserId = auth0UserId,
                Email = email,
                DisplayName = displayName,
                AvatarUrl = avatarUrl,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                HasCompletedOnboarding = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Created new user {UserId} for Auth0 ID {Auth0UserId}", user.Id, auth0UserId);

            try
            {
                if (!await TryCloneTutorialWorldFromTemplateAsync(user.Id))
                {
                    _logger.LogWarning(
                        "Tutorial template world {TemplateWorldId} was not found. Falling back to generated default tutorial world for user {UserId}",
                        TutorialTemplateWorldId,
                        user.Id);

                    await CreateDefaultWorldAsync(user.Id, isTutorial: true);
                }
            }
            catch (Exception ex)
            {
                // Do not block login if tutorial provisioning fails.
                _logger.LogError(ex, "Failed to provision tutorial world for new user {UserId}", user.Id);
            }
        }
        else
        {
            // Update user info in case it changed (e.g., user changed their name/avatar in Auth0)
            bool needsUpdate = false;

            if (user.Email != email)
            {
                user.Email = email;
                needsUpdate = true;
            }

            if (user.DisplayName != displayName)
            {
                user.DisplayName = displayName;
                needsUpdate = true;
            }

            if (user.AvatarUrl != avatarUrl)
            {
                user.AvatarUrl = avatarUrl;
                needsUpdate = true;
            }

            // Always update last login
            user.LastLoginAt = DateTime.UtcNow;
            needsUpdate = true;

            if (needsUpdate)
            {
                await _context.SaveChangesAsync();
            }
        }

        return user;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            HasCompletedOnboarding = user.HasCompletedOnboarding
        };
    }

    public async Task<bool> CompleteOnboardingAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Attempted to complete onboarding for non-existent user {UserId}", userId);
            return false;
        }

        if (!user.HasCompletedOnboarding)
        {
            user.HasCompletedOnboarding = true;
            await _context.SaveChangesAsync();
            _logger.LogDebug("User {UserId} completed onboarding", userId);
        }

        return true;
    }

    /// <summary>
    /// Creates a default world with root structure for a new user.
    /// Used as a fallback if the tutorial template world is unavailable.
    /// </summary>
    private async Task CreateDefaultWorldAsync(Guid userId, bool isTutorial = false)
    {
        _logger.LogDebug("Creating default world for user {UserId}", userId);

        var createDto = new WorldCreateDto
        {
            Name = "My World",
            Description = "Your personal world for campaigns and adventures"
        };

        var createdWorld = await _worldService.CreateWorldAsync(createDto, userId);

        if (!isTutorial)
        {
            return;
        }

        var world = await _context.Worlds.FindAsync(createdWorld.Id);
        if (world != null && !world.IsTutorial)
        {
            world.IsTutorial = true;
            await _context.SaveChangesAsync();
        }
    }

    private async Task<bool> TryCloneTutorialWorldFromTemplateAsync(Guid userId)
    {
        var templateWorld = await _context.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == TutorialTemplateWorldId);

        if (templateWorld == null)
        {
            return false;
        }

        var templateCampaigns = await _context.Campaigns
            .AsNoTracking()
            .Where(c => c.WorldId == templateWorld.Id)
            .ToListAsync();
        var templateCampaignIds = templateCampaigns.Select(c => c.Id).ToList();

        var templateArcs = await _context.Arcs
            .AsNoTracking()
            .Where(a => templateCampaignIds.Contains(a.CampaignId))
            .ToListAsync();
        var templateArcIds = templateArcs.Select(a => a.Id).ToList();

        var templateSessions = await _context.Sessions
            .AsNoTracking()
            .Where(s => templateArcIds.Contains(s.ArcId))
            .ToListAsync();
        var templateSessionIds = templateSessions.Select(s => s.Id).ToList();

        var templateQuests = await _context.Quests
            .AsNoTracking()
            .Where(q => templateArcIds.Contains(q.ArcId))
            .ToListAsync();
        var templateQuestIds = templateQuests.Select(q => q.Id).ToList();

        var templateQuestUpdates = await _context.QuestUpdates
            .AsNoTracking()
            .Where(qu => templateQuestIds.Contains(qu.QuestId))
            .ToListAsync();

        var templateSummaryTemplates = await _context.SummaryTemplates
            .AsNoTracking()
            .Where(st => st.WorldId == templateWorld.Id)
            .ToListAsync();

        var templateArticles = await _context.Articles
            .AsNoTracking()
            .Where(a =>
                a.WorldId == templateWorld.Id ||
                (a.CampaignId.HasValue && templateCampaignIds.Contains(a.CampaignId.Value)) ||
                (a.ArcId.HasValue && templateArcIds.Contains(a.ArcId.Value)) ||
                (a.SessionId.HasValue && templateSessionIds.Contains(a.SessionId.Value)))
            .ToListAsync();
        var templateArticleIds = templateArticles.Select(a => a.Id).ToList();

        var templateArticleAliases = await _context.ArticleAliases
            .AsNoTracking()
            .Where(aa => templateArticleIds.Contains(aa.ArticleId))
            .ToListAsync();

        var templateArticleExternalLinks = await _context.ArticleExternalLinks
            .AsNoTracking()
            .Where(ael => templateArticleIds.Contains(ael.ArticleId))
            .ToListAsync();

        var templateArticleLinks = await _context.ArticleLinks
            .AsNoTracking()
            .Where(al => templateArticleIds.Contains(al.SourceArticleId) && templateArticleIds.Contains(al.TargetArticleId))
            .ToListAsync();

        var templateWorldLinks = await _context.WorldLinks
            .AsNoTracking()
            .Where(wl => wl.WorldId == templateWorld.Id)
            .ToListAsync();

        var templateWorldResourceProviders = await _context.WorldResourceProviders
            .AsNoTracking()
            .Where(wrp => wrp.WorldId == templateWorld.Id)
            .ToListAsync();

        var skippedDocumentCount = await _context.WorldDocuments
            .AsNoTracking()
            .CountAsync(d => d.WorldId == templateWorld.Id);
        if (skippedDocumentCount > 0)
        {
            _logger.LogWarning(
                "Tutorial template world {TemplateWorldId} contains {DocumentCount} world documents. Document rows are not cloned by user provisioning because blob files are not copied",
                TutorialTemplateWorldId,
                skippedDocumentCount);
        }

        var now = DateTime.UtcNow;
        var newWorldId = Guid.NewGuid();
        var newWorldSlug = await GenerateUniqueWorldSlugAsync(templateWorld.Name, userId);

        var summaryTemplateIdMap = templateSummaryTemplates.ToDictionary(st => st.Id, _ => Guid.NewGuid());
        var campaignIdMap = templateCampaigns.ToDictionary(c => c.Id, _ => Guid.NewGuid());
        var arcIdMap = templateArcs.ToDictionary(a => a.Id, _ => Guid.NewGuid());
        var sessionIdMap = templateSessions.ToDictionary(s => s.Id, _ => Guid.NewGuid());
        var questIdMap = templateQuests.ToDictionary(q => q.Id, _ => Guid.NewGuid());
        var articleIdMap = templateArticles.ToDictionary(a => a.Id, _ => Guid.NewGuid());

        Guid? MapSummaryTemplate(Guid? templateSummaryTemplateId)
        {
            if (!templateSummaryTemplateId.HasValue)
            {
                return null;
            }

            return summaryTemplateIdMap.TryGetValue(templateSummaryTemplateId.Value, out var mappedId)
                ? mappedId
                : templateSummaryTemplateId;
        }

        Guid? MapOptionalGuid(Guid? templateId, IReadOnlyDictionary<Guid, Guid> idMap)
        {
            if (!templateId.HasValue)
            {
                return null;
            }

            return idMap.TryGetValue(templateId.Value, out var mappedId)
                ? mappedId
                : null;
        }

        var clonedWorld = new World
        {
            Id = newWorldId,
            Name = templateWorld.Name,
            Slug = newWorldSlug,
            Description = templateWorld.Description,
            OwnerId = userId,
            CreatedAt = now,
            IsTutorial = true,
            IsPublic = false,
            PublicSlug = null
        };

        var clonedSummaryTemplates = templateSummaryTemplates.Select(st => new SummaryTemplate
        {
            Id = summaryTemplateIdMap[st.Id],
            WorldId = newWorldId,
            Name = st.Name,
            Description = st.Description,
            PromptTemplate = st.PromptTemplate,
            IsSystem = st.IsSystem,
            CreatedBy = st.CreatedBy.HasValue ? userId : null,
            CreatedAt = st.CreatedAt
        }).ToList();

        var clonedCampaigns = templateCampaigns.Select(c => new Campaign
        {
            Id = campaignIdMap[c.Id],
            WorldId = newWorldId,
            Name = c.Name,
            Description = c.Description,
            OwnerId = userId,
            CreatedAt = c.CreatedAt,
            StartedAt = c.StartedAt,
            EndedAt = c.EndedAt,
            IsActive = c.IsActive,
            SummaryTemplateId = MapSummaryTemplate(c.SummaryTemplateId),
            SummaryCustomPrompt = c.SummaryCustomPrompt,
            SummaryIncludeWebSources = c.SummaryIncludeWebSources,
            AISummary = c.AISummary,
            AISummaryGeneratedAt = c.AISummaryGeneratedAt
        }).ToList();

        var clonedArcs = templateArcs.Select(a => new Arc
        {
            Id = arcIdMap[a.Id],
            CampaignId = campaignIdMap[a.CampaignId],
            Name = a.Name,
            Description = a.Description,
            SortOrder = a.SortOrder,
            CreatedAt = a.CreatedAt,
            CreatedBy = userId,
            IsActive = a.IsActive,
            SummaryTemplateId = MapSummaryTemplate(a.SummaryTemplateId),
            SummaryCustomPrompt = a.SummaryCustomPrompt,
            SummaryIncludeWebSources = a.SummaryIncludeWebSources,
            AISummary = a.AISummary,
            AISummaryGeneratedAt = a.AISummaryGeneratedAt
        }).ToList();

        var clonedSessions = templateSessions.Select(s => new Session
        {
            Id = sessionIdMap[s.Id],
            ArcId = arcIdMap[s.ArcId],
            Name = s.Name,
            SessionDate = s.SessionDate,
            PublicNotes = s.PublicNotes,
            PrivateNotes = s.PrivateNotes,
            AiSummary = s.AiSummary,
            AiSummaryGeneratedAt = s.AiSummaryGeneratedAt,
            AiSummaryGeneratedByUserId = s.AiSummaryGeneratedByUserId.HasValue ? userId : null,
            CreatedAt = s.CreatedAt,
            ModifiedAt = s.ModifiedAt,
            CreatedBy = userId
        }).ToList();

        var clonedQuests = templateQuests.Select(q => new Quest
        {
            Id = questIdMap[q.Id],
            ArcId = arcIdMap[q.ArcId],
            Title = q.Title,
            Description = q.Description,
            Status = q.Status,
            IsGmOnly = q.IsGmOnly,
            SortOrder = q.SortOrder,
            CreatedBy = userId,
            CreatedAt = q.CreatedAt,
            UpdatedAt = q.UpdatedAt,
            RowVersion = q.RowVersion?.ToArray() ?? Array.Empty<byte>()
        }).ToList();

        var clonedQuestUpdates = templateQuestUpdates.Select(qu => new QuestUpdate
        {
            Id = Guid.NewGuid(),
            QuestId = questIdMap[qu.QuestId],
            SessionId = MapOptionalGuid(qu.SessionId, sessionIdMap),
            Body = qu.Body,
            CreatedBy = userId,
            CreatedAt = qu.CreatedAt
        }).ToList();

        var clonedArticles = templateArticles.Select(a => new Article
        {
            Id = articleIdMap[a.Id],
            ParentId = MapOptionalGuid(a.ParentId, articleIdMap),
            WorldId = a.WorldId.HasValue ? newWorldId : null,
            CampaignId = MapOptionalGuid(a.CampaignId, campaignIdMap),
            ArcId = MapOptionalGuid(a.ArcId, arcIdMap),
            Title = a.Title,
            Slug = a.Slug,
            Body = a.Body,
            IconEmoji = a.IconEmoji,
            Type = a.Type,
            Visibility = a.Visibility,
            CreatedBy = userId,
            LastModifiedBy = a.LastModifiedBy.HasValue ? userId : null,
            CreatedAt = a.CreatedAt,
            ModifiedAt = a.ModifiedAt,
            SessionDate = a.SessionDate,
            InGameDate = a.InGameDate,
            SessionId = MapOptionalGuid(a.SessionId, sessionIdMap),
            PlayerId = a.PlayerId.HasValue ? userId : null,
            SummaryTemplateId = MapSummaryTemplate(a.SummaryTemplateId),
            SummaryCustomPrompt = a.SummaryCustomPrompt,
            SummaryIncludeWebSources = a.SummaryIncludeWebSources,
            AISummary = a.AISummary,
            AISummaryGeneratedAt = a.AISummaryGeneratedAt,
            EffectiveDate = a.EffectiveDate
        }).ToList();

        // Remap embedded wiki-link target GUIDs (and any other copied article GUID references) in rich text.
        foreach (var article in clonedArticles)
        {
            article.Body = RemapArticleIdsInText(article.Body, articleIdMap);
        }

        foreach (var session in clonedSessions)
        {
            session.PublicNotes = RemapArticleIdsInText(session.PublicNotes, articleIdMap);
            session.PrivateNotes = RemapArticleIdsInText(session.PrivateNotes, articleIdMap);
        }

        foreach (var quest in clonedQuests)
        {
            quest.Description = RemapArticleIdsInText(quest.Description, articleIdMap);
        }

        foreach (var questUpdate in clonedQuestUpdates)
        {
            questUpdate.Body = RemapArticleIdsInText(questUpdate.Body, articleIdMap) ?? questUpdate.Body;
        }

        var clonedArticleAliases = templateArticleAliases.Select(aa => new ArticleAlias
        {
            Id = Guid.NewGuid(),
            ArticleId = articleIdMap[aa.ArticleId],
            AliasText = aa.AliasText,
            AliasType = aa.AliasType,
            EffectiveDate = aa.EffectiveDate,
            CreatedAt = aa.CreatedAt
        }).ToList();

        var clonedArticleExternalLinks = templateArticleExternalLinks.Select(ael => new ArticleExternalLink
        {
            Id = Guid.NewGuid(),
            ArticleId = articleIdMap[ael.ArticleId],
            Source = ael.Source,
            ExternalId = ael.ExternalId,
            DisplayTitle = ael.DisplayTitle
        }).ToList();

        var clonedArticleLinks = templateArticleLinks.Select(al => new ArticleLink
        {
            Id = Guid.NewGuid(),
            SourceArticleId = articleIdMap[al.SourceArticleId],
            TargetArticleId = articleIdMap[al.TargetArticleId],
            DisplayText = al.DisplayText,
            Position = al.Position,
            CreatedAt = al.CreatedAt
        }).ToList();

        var clonedWorldLinks = templateWorldLinks.Select(wl => new WorldLink
        {
            Id = Guid.NewGuid(),
            WorldId = newWorldId,
            Url = wl.Url,
            Title = wl.Title,
            Description = wl.Description,
            CreatedAt = wl.CreatedAt
        }).ToList();

        var clonedWorldResourceProviders = templateWorldResourceProviders.Select(wrp => new WorldResourceProvider
        {
            WorldId = newWorldId,
            ResourceProviderCode = wrp.ResourceProviderCode,
            IsEnabled = wrp.IsEnabled,
            ModifiedAt = wrp.ModifiedAt,
            ModifiedByUserId = userId
        }).ToList();

        var ownerMembership = new WorldMember
        {
            Id = Guid.NewGuid(),
            WorldId = newWorldId,
            UserId = userId,
            Role = WorldRole.GM,
            JoinedAt = now,
            InvitedBy = null
        };

        _context.Worlds.Add(clonedWorld);
        if (clonedSummaryTemplates.Count > 0) _context.SummaryTemplates.AddRange(clonedSummaryTemplates);
        if (clonedCampaigns.Count > 0) _context.Campaigns.AddRange(clonedCampaigns);
        if (clonedArcs.Count > 0) _context.Arcs.AddRange(clonedArcs);
        if (clonedSessions.Count > 0) _context.Sessions.AddRange(clonedSessions);
        if (clonedQuests.Count > 0) _context.Quests.AddRange(clonedQuests);
        if (clonedQuestUpdates.Count > 0) _context.QuestUpdates.AddRange(clonedQuestUpdates);
        if (clonedArticles.Count > 0) _context.Articles.AddRange(clonedArticles);
        if (clonedArticleAliases.Count > 0) _context.ArticleAliases.AddRange(clonedArticleAliases);
        if (clonedArticleExternalLinks.Count > 0) _context.ArticleExternalLinks.AddRange(clonedArticleExternalLinks);
        if (clonedArticleLinks.Count > 0) _context.ArticleLinks.AddRange(clonedArticleLinks);
        if (clonedWorldLinks.Count > 0) _context.WorldLinks.AddRange(clonedWorldLinks);
        if (clonedWorldResourceProviders.Count > 0) _context.WorldResourceProviders.AddRange(clonedWorldResourceProviders);
        _context.WorldMembers.Add(ownerMembership);

        await _context.SaveChangesAsync();

        _logger.LogDebug(
            "Provisioned tutorial world {WorldId} from template {TemplateWorldId} for user {UserId} (Campaigns={CampaignCount}, Arcs={ArcCount}, Sessions={SessionCount}, Articles={ArticleCount})",
            newWorldId,
            TutorialTemplateWorldId,
            userId,
            clonedCampaigns.Count,
            clonedArcs.Count,
            clonedSessions.Count,
            clonedArticles.Count);

        return true;
    }

    private async Task<string> GenerateUniqueWorldSlugAsync(string worldName, Guid ownerId)
    {
        var baseSlug = SlugGenerator.GenerateSlug(worldName);
        var existingSlugs = await _context.Worlds
            .AsNoTracking()
            .Where(w => w.OwnerId == ownerId)
            .Select(w => w.Slug)
            .ToHashSetAsync();

        return SlugGenerator.GenerateUniqueSlug(baseSlug, existingSlugs);
    }

    private static string? RemapArticleIdsInText(string? text, IReadOnlyDictionary<Guid, Guid> articleIdMap)
    {
        if (string.IsNullOrEmpty(text) || articleIdMap.Count == 0)
        {
            return text;
        }

        var remapped = text;
        foreach (var kvp in articleIdMap)
        {
            remapped = remapped.Replace(
                kvp.Key.ToString(),
                kvp.Value.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        return remapped;
    }
}
