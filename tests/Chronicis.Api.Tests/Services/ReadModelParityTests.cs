using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ReadModelParityTests
{
    [Fact]
    public async Task PublicAndAuthenticatedPathResolution_ReturnEquivalentArticle_ForPublicHierarchy()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var (publicService, articleService) = CreateServices(db);

        var seed = await SeedPublicWorldWithMembersAsync(db);

        var root = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            createdBy: seed.Owner.Id,
            title: "Root",
            slug: "root",
            type: ArticleType.WikiArticle,
            visibility: ArticleVisibility.Public);

        var child = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            parentId: root.Id,
            createdBy: seed.Owner.Id,
            title: "Child",
            slug: "child",
            type: ArticleType.WikiArticle,
            visibility: ArticleVisibility.Public);

        db.Articles.AddRange(root, child);
        await db.SaveChangesAsync();

        var publicArticle = await publicService.GetPublicArticleAsync(seed.World.Slug, "root/child");
        var authenticatedArticle = await articleService.GetArticleByPathAsync($"{seed.World.Slug}/root/child", seed.Member.Id);

        Assert.NotNull(publicArticle);
        Assert.NotNull(authenticatedArticle);
        Assert.Equal(authenticatedArticle!.Id, publicArticle!.Id);
        Assert.Equal(authenticatedArticle.Title, publicArticle.Title);
        Assert.Equal(authenticatedArticle.Slug, publicArticle.Slug);
        Assert.Equal(authenticatedArticle.ParentId, publicArticle.ParentId);
        Assert.Equal(authenticatedArticle.WorldId, publicArticle.WorldId);
        Assert.Equal(authenticatedArticle.CampaignId, publicArticle.CampaignId);
        Assert.Equal(authenticatedArticle.ArcId, publicArticle.ArcId);
        Assert.Equal(authenticatedArticle.SessionId, publicArticle.SessionId);
        Assert.Equal(authenticatedArticle.Type, publicArticle.Type);
        Assert.Equal(authenticatedArticle.Visibility, publicArticle.Visibility);
        Assert.Equal(authenticatedArticle.IconEmoji, publicArticle.IconEmoji);
        Assert.Equal(authenticatedArticle.AISummary, publicArticle.AISummary);
    }

    [Fact]
    public async Task PublicAndAuthenticatedPathResolution_DenyPrivateArticle_ForNonOwnerMember()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var (publicService, articleService) = CreateServices(db);

        var seed = await SeedPublicWorldWithMembersAsync(db);

        var privateArticle = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            createdBy: seed.Owner.Id,
            title: "Private Root",
            slug: "private-root",
            type: ArticleType.WikiArticle,
            visibility: ArticleVisibility.Private);

        db.Articles.Add(privateArticle);
        await db.SaveChangesAsync();

        var publicArticle = await publicService.GetPublicArticleAsync(seed.World.Slug, "private-root");
        var authenticatedArticle = await articleService.GetArticleByPathAsync($"{seed.World.Slug}/private-root", seed.Member.Id);

        Assert.Null(publicArticle);
        Assert.Null(authenticatedArticle);
    }

    [Fact]
    public async Task OwnerPrivateRead_IsIntentionalDivergence_FromPublicRead()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var (publicService, articleService) = CreateServices(db);

        var seed = await SeedPublicWorldWithMembersAsync(db);

        var privateArticle = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            createdBy: seed.Owner.Id,
            title: "Owner Private",
            slug: "owner-private",
            type: ArticleType.WikiArticle,
            visibility: ArticleVisibility.Private);

        db.Articles.Add(privateArticle);
        await db.SaveChangesAsync();

        var publicArticle = await publicService.GetPublicArticleAsync(seed.World.Slug, "owner-private");
        var authenticatedOwnerArticle = await articleService.GetArticleByPathAsync($"{seed.World.Slug}/owner-private", seed.Owner.Id);

        Assert.Null(publicArticle);
        Assert.NotNull(authenticatedOwnerArticle);
        Assert.Equal(privateArticle.Id, authenticatedOwnerArticle!.Id);
    }

    [Fact]
    public async Task LegacySessionPrefixPath_IsRetired_ForPublicAndAuthenticatedReads()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var (publicService, articleService) = CreateServices(db);

        var seed = await SeedPublicWorldWithMembersAsync(db);

        var campaign = TestHelpers.CreateCampaign(worldId: seed.World.Id, name: "Campaign");
        campaign.OwnerId = seed.Owner.Id;
        campaign.CreatedAt = DateTime.UtcNow;
        campaign.IsActive = true;

        var arc = TestHelpers.CreateArc(campaignId: campaign.Id, name: "Arc", sortOrder: 0);
        arc.CreatedBy = seed.Owner.Id;
        arc.CreatedAt = DateTime.UtcNow;

        var session = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = arc.Id,
            Name = "Session 1",
            SessionDate = DateTime.UtcNow.Date,
            CreatedBy = seed.Owner.Id,
            CreatedAt = DateTime.UtcNow
        };

        var legacySessionArticle = TestHelpers.CreateArticle(
            id: session.Id,
            worldId: seed.World.Id,
            campaignId: campaign.Id,
            arcId: arc.Id,
            createdBy: seed.Owner.Id,
            title: session.Name,
            slug: "session-1",
            type: ArticleType.Session,
            visibility: ArticleVisibility.Public);
        legacySessionArticle.ParentId = null;

        var rootSessionNote = TestHelpers.CreateArticle(
            worldId: seed.World.Id,
            campaignId: campaign.Id,
            arcId: arc.Id,
            createdBy: seed.Owner.Id,
            title: "Root Session Note",
            slug: "root-session-note",
            type: ArticleType.SessionNote,
            visibility: ArticleVisibility.Public);
        rootSessionNote.ParentId = null;
        rootSessionNote.SessionId = session.Id;

        db.Campaigns.Add(campaign);
        db.Arcs.Add(arc);
        db.Sessions.Add(session);
        db.Articles.AddRange(legacySessionArticle, rootSessionNote);
        await db.SaveChangesAsync();

        var publicCompatibilityPathArticle =
            await publicService.GetPublicArticleAsync(seed.World.Slug, "session-1/root-session-note");

        var authenticatedCompatibilityPathArticle =
            await articleService.GetArticleByPathAsync(
                $"{seed.World.Slug}/session-1/root-session-note",
                seed.Owner.Id);

        var publicCanonicalPathArticle =
            await publicService.GetPublicArticleAsync(seed.World.Slug, "root-session-note");

        var authenticatedCanonicalPathArticle =
            await articleService.GetArticleByPathAsync(
                $"{seed.World.Slug}/root-session-note",
                seed.Owner.Id);

        Assert.Null(publicCompatibilityPathArticle);
        Assert.Null(authenticatedCompatibilityPathArticle);
        Assert.NotNull(publicCanonicalPathArticle);
        Assert.Equal(rootSessionNote.Id, publicCanonicalPathArticle!.Id);
        Assert.NotNull(authenticatedCanonicalPathArticle);
        Assert.Equal(rootSessionNote.Id, authenticatedCanonicalPathArticle!.Id);
    }

    private static (PublicWorldService PublicWorldService, ArticleService ArticleService) CreateServices(ChronicisDbContext db)
    {
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildBreadcrumbsAsync(Arg.Any<Guid>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new List<BreadcrumbDto>()));

        var blob = Substitute.For<IBlobStorageService>();
        blob.GenerateDownloadSasUrlAsync(Arg.Any<string>())
            .Returns(args => Task.FromResult($"https://cdn.test/{args.Arg<string>()}"));
        var mapBlobStore = Substitute.For<IMapBlobStore>();

        var readAccessPolicy = new ReadAccessPolicyService();

        return (
            new PublicWorldService(
                db,
                NullLogger<PublicWorldService>.Instance,
                hierarchy,
                blob,
                readAccessPolicy,
                mapBlobStore),
            new ArticleService(
                db,
                NullLogger<ArticleService>.Instance,
                hierarchy,
                readAccessPolicy));
    }

    private static async Task<(User Owner, User Member, World World)> SeedPublicWorldWithMembersAsync(ChronicisDbContext db)
    {
        var owner = TestHelpers.CreateUser();
        var member = TestHelpers.CreateUser();

        var world = TestHelpers.CreateWorld(
            ownerId: owner.Id,
            name: "Parity World",
            slug: "parity-public");
        world.IsPublic = true;

        db.Users.AddRange(owner, member);
        db.Worlds.Add(world);
        db.WorldMembers.AddRange(
            TestHelpers.CreateWorldMember(worldId: world.Id, userId: owner.Id, role: WorldRole.GM),
            TestHelpers.CreateWorldMember(worldId: world.Id, userId: member.Id, role: WorldRole.Player));

        await db.SaveChangesAsync();
        return (owner, member, world);
    }
}
