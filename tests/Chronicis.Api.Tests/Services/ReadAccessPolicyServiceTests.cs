using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronicis.Api.Tests;

public class ReadAccessPolicyServiceTests
{
    [Fact]
    public void NormalizePublicSlug_TrimsAndLowercases()
    {
        var sut = new ReadAccessPolicyService();

        Assert.Equal("my-public-world", sut.NormalizePublicSlug("  My-Public-World  "));
    }

    [Fact]
    public async Task ApplyPublicWorldFilters_ReturnOnlyPublicWorldsAndMatchingSlug()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var sut = new ReadAccessPolicyService();

        var publicWorld = TestHelpers.CreateWorld(name: "Public", slug: "internal-public");
        publicWorld.IsPublic = true;
        publicWorld.PublicSlug = "shared-world";

        var privateWorld = TestHelpers.CreateWorld(name: "Private", slug: "internal-private");
        privateWorld.IsPublic = false;
        privateWorld.PublicSlug = "private-world";

        db.Worlds.AddRange(publicWorld, privateWorld);
        await db.SaveChangesAsync();

        var publicWorldIds = await sut.ApplyPublicWorldFilter(db.Worlds.AsNoTracking())
            .Select(w => w.Id)
            .ToListAsync();

        var slugMatch = await sut.ApplyPublicWorldSlugFilter(db.Worlds.AsNoTracking(), "  SHARED-WORLD  ")
            .Select(w => w.Id)
            .SingleOrDefaultAsync();

        Assert.Single(publicWorldIds);
        Assert.Contains(publicWorld.Id, publicWorldIds);
        Assert.Equal(publicWorld.Id, slugMatch);
    }

    [Fact]
    public async Task ApplyPublicArticleFilters_RespectWorldAndVisibility()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var sut = new ReadAccessPolicyService();

        var world = TestHelpers.CreateWorld(ownerId: Guid.NewGuid());
        db.Worlds.Add(world);

        var publicInWorld = TestHelpers.CreateArticle(
            worldId: world.Id,
            visibility: ArticleVisibility.Public,
            title: "Public In World");

        var privateInWorld = TestHelpers.CreateArticle(
            worldId: world.Id,
            visibility: ArticleVisibility.Private,
            title: "Private In World");

        var publicInOtherWorld = TestHelpers.CreateArticle(
            worldId: Guid.NewGuid(),
            visibility: ArticleVisibility.Public,
            title: "Public Other World");

        db.Articles.AddRange(publicInWorld, privateInWorld, publicInOtherWorld);
        await db.SaveChangesAsync();

        var publicVisibleIds = await sut.ApplyPublicVisibilityFilter(db.Articles.AsNoTracking())
            .Select(a => a.Id)
            .ToListAsync();

        var worldScopedIds = await sut.ApplyPublicArticleFilter(db.Articles.AsNoTracking(), world.Id)
            .Select(a => a.Id)
            .ToListAsync();

        Assert.Contains(publicInWorld.Id, publicVisibleIds);
        Assert.DoesNotContain(privateInWorld.Id, publicVisibleIds);

        Assert.Single(worldScopedIds);
        Assert.Contains(publicInWorld.Id, worldScopedIds);
        Assert.DoesNotContain(publicInOtherWorld.Id, worldScopedIds);
    }

    [Fact]
    public async Task ApplyAuthenticatedArticleFilters_RespectMembershipPrivateOwnershipAndTutorials()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var sut = new ReadAccessPolicyService();

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var readableWorld = TestHelpers.CreateWorld(ownerId: userId);
        var unreadableWorld = TestHelpers.CreateWorld(ownerId: Guid.NewGuid());
        db.Worlds.AddRange(readableWorld, unreadableWorld);

        db.WorldMembers.AddRange(
            TestHelpers.CreateWorldMember(worldId: readableWorld.Id, userId: userId),
            TestHelpers.CreateWorldMember(worldId: readableWorld.Id, userId: otherUserId));

        var publicReadable = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: otherUserId,
            visibility: ArticleVisibility.Public,
            title: "Public Readable");

        var privateOwned = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: userId,
            visibility: ArticleVisibility.Private,
            title: "Private Owned");

        var privateNotOwned = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: otherUserId,
            visibility: ArticleVisibility.Private,
            title: "Private Not Owned");

        var unreadableWorldArticle = TestHelpers.CreateArticle(
            worldId: unreadableWorld.Id,
            createdBy: unreadableWorld.OwnerId,
            visibility: ArticleVisibility.Public,
            title: "Unreadable World Article");

        var tutorial = TestHelpers.CreateArticle(
            worldId: Guid.Empty,
            type: ArticleType.Tutorial,
            createdBy: otherUserId,
            title: "Tutorial");

        db.Articles.AddRange(publicReadable, privateOwned, privateNotOwned, unreadableWorldArticle, tutorial);
        await db.SaveChangesAsync();

        var worldScopedIds = await sut.ApplyAuthenticatedWorldArticleFilter(db.Articles.AsNoTracking(), userId)
            .Select(a => a.Id)
            .ToListAsync();

        var readableIds = await sut.ApplyAuthenticatedReadableArticleFilter(db.Articles.AsNoTracking(), userId)
            .Select(a => a.Id)
            .ToListAsync();

        Assert.Contains(publicReadable.Id, worldScopedIds);
        Assert.Contains(privateOwned.Id, worldScopedIds);
        Assert.DoesNotContain(privateNotOwned.Id, worldScopedIds);
        Assert.DoesNotContain(unreadableWorldArticle.Id, worldScopedIds);
        Assert.DoesNotContain(tutorial.Id, worldScopedIds);

        Assert.Contains(tutorial.Id, readableIds);
    }

    [Fact]
    public async Task ApplyAuthenticatedCampaignAndArcFilters_RespectMembership()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var sut = new ReadAccessPolicyService();

        var userId = Guid.NewGuid();

        var readableWorld = TestHelpers.CreateWorld(ownerId: userId);
        var unreadableWorld = TestHelpers.CreateWorld(ownerId: Guid.NewGuid());
        db.Worlds.AddRange(readableWorld, unreadableWorld);

        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: readableWorld.Id, userId: userId));

        var readableCampaign = TestHelpers.CreateCampaign(worldId: readableWorld.Id);
        var unreadableCampaign = TestHelpers.CreateCampaign(worldId: unreadableWorld.Id);
        db.Campaigns.AddRange(readableCampaign, unreadableCampaign);

        var readableArc = TestHelpers.CreateArc(campaignId: readableCampaign.Id);
        var unreadableArc = TestHelpers.CreateArc(campaignId: unreadableCampaign.Id);
        db.Arcs.AddRange(readableArc, unreadableArc);

        await db.SaveChangesAsync();

        var campaignIds = await sut.ApplyAuthenticatedCampaignFilter(db.Campaigns.AsNoTracking(), userId)
            .Select(c => c.Id)
            .ToListAsync();

        var arcIds = await sut.ApplyAuthenticatedArcFilter(db.Arcs.AsNoTracking(), userId)
            .Select(a => a.Id)
            .ToListAsync();

        Assert.Single(campaignIds);
        Assert.Contains(readableCampaign.Id, campaignIds);
        Assert.DoesNotContain(unreadableCampaign.Id, campaignIds);

        Assert.Single(arcIds);
        Assert.Contains(readableArc.Id, arcIds);
        Assert.DoesNotContain(unreadableArc.Id, arcIds);
    }
}
