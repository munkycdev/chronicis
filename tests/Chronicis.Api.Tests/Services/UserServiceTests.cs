using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;


[ExcludeFromCodeCoverage]
public class UserServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly UserService _service;
    private readonly IWorldService _worldService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _worldService = Substitute.For<IWorldService>();
        _worldService.CreateWorldAsync(Arg.Any<WorldCreateDto>(), Arg.Any<Guid>())
            .Returns(call =>
            {
                var userId = call.ArgAt<Guid>(1);
                var dto = call.ArgAt<WorldCreateDto>(0);
                return Task.FromResult(new WorldDto
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    OwnerId = userId
                });
            });
        _service = new UserService(_context, _worldService, NullLogger<UserService>.Instance);
    }

    private bool _disposed = false;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _context.Dispose();
        }

        _disposed = true;
    }

    // ────────────────────────────────────────────────────────────────
    //  GetOrCreateUserAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateUserAsync_NewUser_CreatesUser()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|12345",
            "test@example.com",
            "Test User",
            "https://example.com/avatar.jpg");

        Assert.NotNull(user);
        Assert.Equal("auth0|12345", user.Auth0UserId);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("Test User", user.DisplayName);
        Assert.Equal("https://example.com/avatar.jpg", user.AvatarUrl);
        Assert.False(user.HasCompletedOnboarding);

        // Verify saved to database
        var saved = await _context.Users.FirstOrDefaultAsync(u => u.Auth0UserId == "auth0|12345");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_NewUser_ClonesTutorialTemplateWorld()
    {
        static User SeedUser(Guid id, string auth0Id, string email, string displayName) => new()
        {
            Id = id,
            Auth0UserId = auth0Id,
            Email = email,
            DisplayName = displayName,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLoginAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            HasCompletedOnboarding = true
        };

        var templateWorldId = Guid.Parse("bbcee097-e733-4c55-a72b-91fa2cfa0391");
        var templateOwner = SeedUser(Guid.NewGuid(), "auth0|template-owner", "template-owner@example.com", "Template Owner");
        var templateMember = SeedUser(Guid.NewGuid(), "auth0|template-member", "template-member@example.com", "Template Member");
        _context.Users.AddRange(templateOwner, templateMember);

        _context.ResourceProviders.Add(new ResourceProvider
        {
            Code = "srd14",
            Name = "SRD 2014",
            Description = "Seeded for clone test",
            DocumentationLink = "https://example.com/docs",
            License = "https://example.com/license",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var templateWorld = new World
        {
            Id = templateWorldId,
            Name = "Tutorial World",
            Slug = "tutorial-world",
            Description = "Template tutorial world",
            OwnerId = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            IsPublic = true,
            PublicSlug = "tutorial-template",
            IsTutorial = false
        };
        _context.Worlds.Add(templateWorld);

        var summaryTemplate = new SummaryTemplate
        {
            Id = Guid.NewGuid(),
            WorldId = templateWorldId,
            Name = "Tutorial Summary",
            Description = "World-scoped summary template",
            PromptTemplate = "Summarize {EntityName}",
            IsSystem = false,
            CreatedBy = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 1, 1, 0, 0, DateTimeKind.Utc)
        };
        _context.SummaryTemplates.Add(summaryTemplate);

        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            WorldId = templateWorldId,
            Name = "Starter Campaign",
            Description = "Tutorial campaign",
            OwnerId = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 2, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            SummaryTemplateId = summaryTemplate.Id
        };
        _context.Campaigns.Add(campaign);

        var arc = new Arc
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Name = "Arc One",
            Description = "Tutorial arc",
            SortOrder = 1,
            CreatedAt = new DateTime(2025, 12, 2, 1, 0, 0, DateTimeKind.Utc),
            CreatedBy = templateOwner.Id,
            IsActive = true,
            SummaryTemplateId = summaryTemplate.Id
        };
        _context.Arcs.Add(arc);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Name = "Session One",
            SessionDate = new DateTime(2025, 12, 3, 0, 0, 0, DateTimeKind.Utc),
            PublicNotes = "Template session public notes",
            PrivateNotes = "Template session private notes",
            AiSummary = "Template summary",
            AiSummaryGeneratedAt = new DateTime(2025, 12, 3, 1, 0, 0, DateTimeKind.Utc),
            AiSummaryGeneratedByUserId = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 3, 0, 0, 0, DateTimeKind.Utc),
            ModifiedAt = new DateTime(2025, 12, 3, 2, 0, 0, DateTimeKind.Utc),
            CreatedBy = templateOwner.Id
        };
        _context.Sessions.Add(session);

        var quest = new Quest
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Title = "Find the Relic",
            Description = "Template quest description",
            Status = QuestStatus.Active,
            IsGmOnly = false,
            SortOrder = 1,
            CreatedBy = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 3, 0, 30, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 12, 3, 0, 45, 0, DateTimeKind.Utc),
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };
        _context.Quests.Add(quest);

        var articleAlphaId = Guid.NewGuid();
        var articleBetaId = Guid.NewGuid();
        var sessionNoteId = Guid.NewGuid();

        var articleAlpha = new Article
        {
            Id = articleAlphaId,
            WorldId = templateWorldId,
            Title = "Alpha",
            Slug = "alpha",
            Body = $"<p><span data-type=\"wiki-link\" data-target-id=\"{articleBetaId}\" data-display=\"Beta\">Beta</span> [[{articleBetaId}|Beta]]</p>",
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            CreatedBy = templateOwner.Id,
            LastModifiedBy = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 4, 0, 0, 0, DateTimeKind.Utc),
            ModifiedAt = new DateTime(2025, 12, 4, 1, 0, 0, DateTimeKind.Utc),
            SummaryTemplateId = summaryTemplate.Id,
            EffectiveDate = new DateTime(2025, 12, 4, 0, 0, 0, DateTimeKind.Utc)
        };

        var articleBeta = new Article
        {
            Id = articleBetaId,
            ParentId = articleAlphaId,
            WorldId = templateWorldId,
            Title = "Beta",
            Slug = "beta",
            Body = "<p>Child article</p>",
            Type = ArticleType.Character,
            Visibility = ArticleVisibility.Private,
            CreatedBy = templateOwner.Id,
            LastModifiedBy = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 4, 0, 10, 0, DateTimeKind.Utc),
            ModifiedAt = new DateTime(2025, 12, 4, 1, 10, 0, DateTimeKind.Utc),
            PlayerId = templateOwner.Id,
            SummaryTemplateId = summaryTemplate.Id,
            EffectiveDate = new DateTime(2025, 12, 4, 0, 10, 0, DateTimeKind.Utc)
        };

        var sessionNoteArticle = new Article
        {
            Id = sessionNoteId,
            WorldId = templateWorldId,
            CampaignId = campaign.Id,
            ArcId = arc.Id,
            SessionId = session.Id,
            Title = "Session Note",
            Slug = "session-note",
            Body = $"<p>See {articleAlphaId}</p>",
            Type = ArticleType.SessionNote,
            Visibility = ArticleVisibility.Public,
            CreatedBy = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 4, 2, 0, 0, DateTimeKind.Utc),
            SummaryTemplateId = summaryTemplate.Id,
            EffectiveDate = new DateTime(2025, 12, 4, 2, 0, 0, DateTimeKind.Utc)
        };

        _context.Articles.AddRange(articleAlpha, articleBeta, sessionNoteArticle);

        _context.ArticleAliases.Add(new ArticleAlias
        {
            Id = Guid.NewGuid(),
            ArticleId = articleBetaId,
            AliasText = "Beta Alias",
            AliasType = "Nickname",
            EffectiveDate = new DateTime(2025, 12, 4, 0, 20, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2025, 12, 4, 0, 20, 0, DateTimeKind.Utc)
        });

        _context.ArticleExternalLinks.Add(new ArticleExternalLink
        {
            Id = Guid.NewGuid(),
            ArticleId = articleAlphaId,
            Source = "srd14",
            ExternalId = "/api/2014/spells/acid-arrow",
            DisplayTitle = "Acid Arrow"
        });

        _context.ArticleLinks.Add(new ArticleLink
        {
            Id = Guid.NewGuid(),
            SourceArticleId = articleAlphaId,
            TargetArticleId = articleBetaId,
            DisplayText = "Beta",
            Position = 42,
            CreatedAt = new DateTime(2025, 12, 4, 0, 30, 0, DateTimeKind.Utc)
        });

        _context.QuestUpdates.Add(new QuestUpdate
        {
            Id = Guid.NewGuid(),
            QuestId = quest.Id,
            SessionId = session.Id,
            Body = $"<p>Quest update mentions {articleAlphaId}</p>",
            CreatedBy = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 5, 0, 0, 0, DateTimeKind.Utc)
        });

        _context.WorldLinks.Add(new WorldLink
        {
            Id = Guid.NewGuid(),
            WorldId = templateWorldId,
            Url = "https://example.com/tutorial",
            Title = "Tutorial Link",
            Description = "Template world link",
            CreatedAt = new DateTime(2025, 12, 5, 1, 0, 0, DateTimeKind.Utc)
        });

        _context.WorldResourceProviders.Add(new WorldResourceProvider
        {
            WorldId = templateWorldId,
            ResourceProviderCode = "srd14",
            IsEnabled = true,
            ModifiedAt = new DateTimeOffset(2025, 12, 5, 2, 0, 0, TimeSpan.Zero),
            ModifiedByUserId = templateOwner.Id
        });

        _context.WorldMembers.AddRange(
            new WorldMember
            {
                Id = Guid.NewGuid(),
                WorldId = templateWorldId,
                UserId = templateOwner.Id,
                Role = WorldRole.GM,
                JoinedAt = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new WorldMember
            {
                Id = Guid.NewGuid(),
                WorldId = templateWorldId,
                UserId = templateMember.Id,
                Role = WorldRole.Player,
                JoinedAt = new DateTime(2025, 12, 1, 1, 0, 0, DateTimeKind.Utc),
                InvitedBy = templateOwner.Id
            });

        _context.WorldInvitations.Add(new WorldInvitation
        {
            Id = Guid.NewGuid(),
            WorldId = templateWorldId,
            Code = "ABCD-EFGH",
            Role = WorldRole.Player,
            CreatedBy = templateOwner.Id,
            CreatedAt = new DateTime(2025, 12, 6, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        });

        _context.WorldDocuments.Add(new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = templateWorldId,
            ArticleId = articleAlphaId,
            FileName = "map.png",
            Title = "Map",
            BlobPath = "worlds/template/documents/doc/map.png",
            ContentType = "image/png",
            FileSizeBytes = 1234,
            Description = "Template map",
            UploadedAt = new DateTime(2025, 12, 6, 1, 0, 0, DateTimeKind.Utc),
            UploadedById = templateOwner.Id
        });

        await _context.SaveChangesAsync();

        var newUser = await _service.GetOrCreateUserAsync(
            "auth0|new-user",
            "new-user@example.com",
            "New User",
            null);

        var clonedWorld = await _context.Worlds
            .SingleAsync(w => w.OwnerId == newUser.Id);

        Assert.NotEqual(templateWorldId, clonedWorld.Id);
        Assert.Equal(templateWorld.Name, clonedWorld.Name);
        Assert.Equal(templateWorld.Description, clonedWorld.Description);
        Assert.True(clonedWorld.IsTutorial);
        Assert.False(clonedWorld.IsPublic);
        Assert.Null(clonedWorld.PublicSlug);

        var clonedMemberships = await _context.WorldMembers
            .Where(m => m.WorldId == clonedWorld.Id)
            .ToListAsync();
        Assert.Single(clonedMemberships);
        Assert.Equal(newUser.Id, clonedMemberships[0].UserId);
        Assert.Equal(WorldRole.GM, clonedMemberships[0].Role);

        Assert.Equal(0, await _context.WorldInvitations.CountAsync(i => i.WorldId == clonedWorld.Id));
        Assert.Equal(0, await _context.WorldDocuments.CountAsync(d => d.WorldId == clonedWorld.Id));

        var clonedSummaryTemplate = await _context.SummaryTemplates
            .SingleAsync(st => st.WorldId == clonedWorld.Id);
        Assert.NotEqual(summaryTemplate.Id, clonedSummaryTemplate.Id);
        Assert.Equal(newUser.Id, clonedSummaryTemplate.CreatedBy);

        var clonedCampaign = await _context.Campaigns
            .SingleAsync(c => c.WorldId == clonedWorld.Id);
        Assert.NotEqual(campaign.Id, clonedCampaign.Id);
        Assert.Equal(newUser.Id, clonedCampaign.OwnerId);
        Assert.Equal(clonedSummaryTemplate.Id, clonedCampaign.SummaryTemplateId);

        var clonedArc = await _context.Arcs
            .SingleAsync(a => a.CampaignId == clonedCampaign.Id);
        Assert.NotEqual(arc.Id, clonedArc.Id);
        Assert.Equal(newUser.Id, clonedArc.CreatedBy);
        Assert.Equal(clonedSummaryTemplate.Id, clonedArc.SummaryTemplateId);

        var clonedSession = await _context.Sessions
            .SingleAsync(s => s.ArcId == clonedArc.Id);
        Assert.NotEqual(session.Id, clonedSession.Id);
        Assert.Equal(newUser.Id, clonedSession.CreatedBy);
        Assert.Equal(newUser.Id, clonedSession.AiSummaryGeneratedByUserId);

        var clonedQuest = await _context.Quests
            .SingleAsync(q => q.ArcId == clonedArc.Id);
        Assert.NotEqual(quest.Id, clonedQuest.Id);
        Assert.Equal(newUser.Id, clonedQuest.CreatedBy);

        var clonedQuestUpdate = await _context.QuestUpdates
            .SingleAsync(qu => qu.QuestId == clonedQuest.Id);
        Assert.Equal(clonedSession.Id, clonedQuestUpdate.SessionId);
        Assert.Equal(newUser.Id, clonedQuestUpdate.CreatedBy);

        var clonedArticles = await _context.Articles
            .Where(a => a.WorldId == clonedWorld.Id)
            .ToListAsync();
        Assert.Equal(3, clonedArticles.Count);

        var clonedAlpha = clonedArticles.Single(a => a.Title == "Alpha");
        var clonedBeta = clonedArticles.Single(a => a.Title == "Beta");
        var clonedSessionNote = clonedArticles.Single(a => a.Title == "Session Note");

        Assert.NotEqual(articleAlphaId, clonedAlpha.Id);
        Assert.NotEqual(articleBetaId, clonedBeta.Id);
        Assert.Equal(clonedAlpha.Id, clonedBeta.ParentId);
        Assert.Equal(newUser.Id, clonedAlpha.CreatedBy);
        Assert.Equal(newUser.Id, clonedAlpha.LastModifiedBy);
        Assert.Equal(newUser.Id, clonedBeta.PlayerId);
        Assert.Equal(clonedSummaryTemplate.Id, clonedAlpha.SummaryTemplateId);
        Assert.Equal(clonedSummaryTemplate.Id, clonedBeta.SummaryTemplateId);
        Assert.Equal(clonedSummaryTemplate.Id, clonedSessionNote.SummaryTemplateId);

        Assert.NotNull(clonedAlpha.Body);
        Assert.Contains(clonedBeta.Id.ToString(), clonedAlpha.Body!);
        Assert.DoesNotContain(articleBetaId.ToString(), clonedAlpha.Body!);

        Assert.Equal(clonedCampaign.Id, clonedSessionNote.CampaignId);
        Assert.Equal(clonedArc.Id, clonedSessionNote.ArcId);
        Assert.Equal(clonedSession.Id, clonedSessionNote.SessionId);
        Assert.Contains(clonedAlpha.Id.ToString(), clonedSessionNote.Body!);
        Assert.DoesNotContain(articleAlphaId.ToString(), clonedSessionNote.Body!);

        var clonedAlias = await _context.ArticleAliases.SingleAsync(a => a.ArticleId == clonedBeta.Id);
        Assert.Equal("Beta Alias", clonedAlias.AliasText);

        var clonedExternalLink = await _context.ArticleExternalLinks.SingleAsync(a => a.ArticleId == clonedAlpha.Id);
        Assert.Equal("/api/2014/spells/acid-arrow", clonedExternalLink.ExternalId);

        var clonedArticleLink = await _context.ArticleLinks.SingleAsync(a => a.SourceArticleId == clonedAlpha.Id);
        Assert.Equal(clonedBeta.Id, clonedArticleLink.TargetArticleId);

        var clonedWorldLink = await _context.WorldLinks.SingleAsync(w => w.WorldId == clonedWorld.Id);
        Assert.Equal("Tutorial Link", clonedWorldLink.Title);

        var clonedProvider = await _context.WorldResourceProviders.SingleAsync(w => w.WorldId == clonedWorld.Id);
        Assert.Equal("srd14", clonedProvider.ResourceProviderCode);
        Assert.Equal(newUser.Id, clonedProvider.ModifiedByUserId);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_ExistingUser_ReturnsExisting()
    {
        // Create user first
        var existing = await _service.GetOrCreateUserAsync(
            "auth0|existing",
            "existing@example.com",
            "Existing User",
            null);

        var existingId = existing.Id;

        // Try to create again
        var retrieved = await _service.GetOrCreateUserAsync(
            "auth0|existing",
            "existing@example.com",
            "Existing User",
            null);

        Assert.Equal(existingId, retrieved.Id);
        Assert.Single(await _context.Users.Where(u => u.Auth0UserId == "auth0|existing").ToListAsync());
    }

    [Fact]
    public async Task GetOrCreateUserAsync_ExistingUser_UpdatesChangedInfo()
    {
        // Create user
        await _service.GetOrCreateUserAsync(
            "auth0|update",
            "old@example.com",
            "Old Name",
            null);

        // Update with new info
        var updated = await _service.GetOrCreateUserAsync(
            "auth0|update",
            "new@example.com",
            "New Name",
            "https://example.com/new.jpg");

        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("New Name", updated.DisplayName);
        Assert.Equal("https://example.com/new.jpg", updated.AvatarUrl);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_UpdatesLastLoginTime()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|login",
            "login@example.com",
            "Login User",
            null);

        var firstLogin = user.LastLoginAt;
        await Task.Delay(10); // Small delay

        // Login again
        var loginAgain = await _service.GetOrCreateUserAsync(
            "auth0|login",
            "login@example.com",
            "Login User",
            null);

        Assert.True(loginAgain.LastLoginAt > firstLogin);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetUserByIdAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        var created = await _service.GetOrCreateUserAsync(
            "auth0|find",
            "find@example.com",
            "Find Me",
            null);

        var found = await _service.GetUserByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistent_ReturnsNull()
    {
        var found = await _service.GetUserByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateLastLoginAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLastLoginAsync_ExistingUser_UpdatesTimestamp()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|timestamp",
            "timestamp@example.com",
            "Timestamp User",
            null);

        var original = user.LastLoginAt;
        await Task.Delay(10);

        await _service.UpdateLastLoginAsync(user.Id);

        var updated = await _context.Users.FindAsync(user.Id);
        Assert.True(updated!.LastLoginAt > original);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_NonExistent_DoesNotThrow()
    {
        // Should not throw exception
        await _service.UpdateLastLoginAsync(Guid.NewGuid());
    }

    // ────────────────────────────────────────────────────────────────
    //  GetUserProfileAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserProfileAsync_ExistingUser_ReturnsProfile()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|profile",
            "profile@example.com",
            "Profile User",
            "https://example.com/profile.jpg");

        var profile = await _service.GetUserProfileAsync(user.Id);

        Assert.NotNull(profile);
        Assert.Equal(user.Id, profile!.Id);
        Assert.Equal("profile@example.com", profile.Email);
        Assert.Equal("Profile User", profile.DisplayName);
        Assert.Equal("https://example.com/profile.jpg", profile.AvatarUrl);
        Assert.False(profile.HasCompletedOnboarding);
    }

    [Fact]
    public async Task GetUserProfileAsync_NonExistent_ReturnsNull()
    {
        var profile = await _service.GetUserProfileAsync(Guid.NewGuid());

        Assert.Null(profile);
    }

    // ────────────────────────────────────────────────────────────────
    //  CompleteOnboardingAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteOnboardingAsync_ExistingUser_SetsFlag()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|onboarding",
            "onboarding@example.com",
            "Onboarding User",
            null);

        Assert.False(user.HasCompletedOnboarding);

        var result = await _service.CompleteOnboardingAsync(user.Id);

        Assert.True(result);

        var updated = await _context.Users.FindAsync(user.Id);
        Assert.True(updated!.HasCompletedOnboarding);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_AlreadyCompleted_Succeeds()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|completed",
            "completed@example.com",
            "Completed User",
            null);

        await _service.CompleteOnboardingAsync(user.Id);
        var result = await _service.CompleteOnboardingAsync(user.Id); // Complete again

        Assert.True(result);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_NonExistent_ReturnsFalse()
    {
        var result = await _service.CompleteOnboardingAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public void RemapArticleIdsInText_WhenTextIsNull_ReturnsNull()
    {
        var method = typeof(UserService).GetMethod("RemapArticleIdsInText", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var map = new Dictionary<Guid, Guid> { [sourceId] = destId };

        var result = (string?)method!.Invoke(null, [null, map]);

        Assert.Null(result);
    }

    [Fact]
    public void RemapArticleIdsInText_WhenMapEmpty_ReturnsOriginalText()
    {
        var method = typeof(UserService).GetMethod("RemapArticleIdsInText", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        const string text = "No replacements";
        var result = (string?)method!.Invoke(null, [text, new Dictionary<Guid, Guid>()]);

        Assert.Equal(text, result);
    }
}
