using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronicis.Api.Tests;

public class ReadAccessPolicyServiceTests
{
    [Fact]
    public async Task ApplyPublicWorldFilter_ReturnsOnlyPublicWorlds()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var sut = new ReadAccessPolicyService();

        var publicWorld = TestHelpers.CreateWorld(name: "Public", slug: "shared-world");
        publicWorld.IsPublic = true;

        var privateWorld = TestHelpers.CreateWorld(name: "Private", slug: "internal-private");
        privateWorld.IsPublic = false;

        db.Worlds.AddRange(publicWorld, privateWorld);
        await db.SaveChangesAsync();

        var publicWorldIds = await sut.ApplyPublicWorldFilter(db.Worlds.AsNoTracking())
            .Select(w => w.Id)
            .ToListAsync();

        Assert.Single(publicWorldIds);
        Assert.Contains(publicWorld.Id, publicWorldIds);
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

    // ────────────────────────────────────────────────────────────────
    //  CanReadWorld / CanReadMemberScopedEntity
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void CanReadWorld_PublicWorld_ReturnsTrue()
    {
        var sut = new ReadAccessPolicyService();
        Assert.True(sut.CanReadWorld(isPublic: true, userIsMember: false));
    }

    [Fact]
    public void CanReadWorld_PrivateWorldAndMember_ReturnsTrue()
    {
        var sut = new ReadAccessPolicyService();
        Assert.True(sut.CanReadWorld(isPublic: false, userIsMember: true));
    }

    [Fact]
    public void CanReadWorld_PrivateWorldNonMember_ReturnsFalse()
    {
        var sut = new ReadAccessPolicyService();
        Assert.False(sut.CanReadWorld(isPublic: false, userIsMember: false));
    }

    [Fact]
    public void CanReadMemberScopedEntity_Member_ReturnsTrue()
    {
        var sut = new ReadAccessPolicyService();
        Assert.True(sut.CanReadMemberScopedEntity(userIsMember: true));
    }

    [Fact]
    public void CanReadMemberScopedEntity_NonMember_ReturnsFalse()
    {
        var sut = new ReadAccessPolicyService();
        Assert.False(sut.CanReadMemberScopedEntity(userIsMember: false));
    }
}
