using Chronicis.Api.Services;
using Xunit;

namespace Chronicis.Api.Tests;

public class SummaryAccessServiceTests
{
    [Fact]
    public async Task CanAccessArticleAsync_ReturnsTrueForWorldMember_AndFalseOtherwise()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));
        var article = TestHelpers.CreateArticle(worldId: world.Id, createdBy: userId);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var sut = new SummaryAccessService(db, new ReadAccessPolicyService());

        Assert.True(await sut.CanAccessArticleAsync(article.Id, userId));
        Assert.False(await sut.CanAccessArticleAsync(article.Id, otherUserId));
    }

    [Fact]
    public async Task CanAccessCampaignAsync_ReturnsTrueForWorldMember_AndFalseOtherwise()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));
        var campaign = TestHelpers.CreateCampaign(worldId: world.Id);
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        var sut = new SummaryAccessService(db, new ReadAccessPolicyService());

        Assert.True(await sut.CanAccessCampaignAsync(campaign.Id, userId));
        Assert.False(await sut.CanAccessCampaignAsync(campaign.Id, otherUserId));
    }

    [Fact]
    public async Task CanAccessArcAsync_ReturnsTrueForWorldMember_AndFalseOtherwise()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));
        var campaign = TestHelpers.CreateCampaign(worldId: world.Id);
        db.Campaigns.Add(campaign);
        var arc = TestHelpers.CreateArc(campaignId: campaign.Id);
        db.Arcs.Add(arc);
        await db.SaveChangesAsync();

        var sut = new SummaryAccessService(db, new ReadAccessPolicyService());

        Assert.True(await sut.CanAccessArcAsync(arc.Id, userId));
        Assert.False(await sut.CanAccessArcAsync(arc.Id, otherUserId));
    }

    [Fact]
    public async Task CanAccessArticleAsync_RespectsPrivateOwnership_ForWorldMembers()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var world = TestHelpers.CreateWorld(ownerId: userId);
        db.Worlds.Add(world);
        db.WorldMembers.AddRange(
            TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId),
            TestHelpers.CreateWorldMember(worldId: world.Id, userId: otherUserId));

        var privateArticle = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: otherUserId,
            visibility: Chronicis.Shared.Enums.ArticleVisibility.Private);
        db.Articles.Add(privateArticle);
        await db.SaveChangesAsync();

        var sut = new SummaryAccessService(db, new ReadAccessPolicyService());

        Assert.False(await sut.CanAccessArticleAsync(privateArticle.Id, userId));
        Assert.True(await sut.CanAccessArticleAsync(privateArticle.Id, otherUserId));
    }
}
