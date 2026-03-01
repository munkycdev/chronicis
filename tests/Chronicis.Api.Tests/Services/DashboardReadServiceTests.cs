using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class DashboardReadServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_BuildsAggregatedDashboard()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var displayName = "Dashboard User";
        var world = TestHelpers.CreateWorld(ownerId: userId, name: "Main World");

        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        var campaignA = TestHelpers.CreateCampaign(worldId: world.Id, name: "Alpha", isActive: true);
        var campaignB = TestHelpers.CreateCampaign(worldId: world.Id, name: "Beta", isActive: false);
        db.Campaigns.AddRange(campaignA, campaignB);

        var activeArc = TestHelpers.CreateArc(campaignId: campaignA.Id, name: "Active Arc", sortOrder: 2);
        activeArc.IsActive = true;
        var inactiveArc = TestHelpers.CreateArc(campaignId: campaignB.Id, name: "Inactive Arc", sortOrder: 1);
        inactiveArc.IsActive = false;
        db.Arcs.AddRange(activeArc, inactiveArc);

        db.Sessions.AddRange(
            new Session
            {
                Id = Guid.NewGuid(),
                ArcId = activeArc.Id,
                Name = "Session A1",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                SessionDate = DateTime.UtcNow.Date
            },
            new Session
            {
                Id = Guid.NewGuid(),
                ArcId = inactiveArc.Id,
                Name = "Session B1",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });

        var rootArticle = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: "Root",
            type: ArticleType.WikiArticle);
        rootArticle.ParentId = null;

        var character = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: "Character",
            type: ArticleType.Character);
        character.PlayerId = userId;

        db.Articles.AddRange(rootArticle, character);
        await db.SaveChangesAsync();

        var promptService = Substitute.For<IPromptService>();
        promptService.GeneratePrompts(Arg.Any<DashboardDto>())
            .Returns([new PromptDto { Key = "p1", Title = "Prompt" }]);

        var sut = new DashboardReadService(db, promptService);

        var dashboard = await sut.GetDashboardAsync(userId, displayName);

        Assert.Equal(displayName, dashboard.UserDisplayName);
        Assert.Single(dashboard.Worlds);
        Assert.Single(dashboard.ClaimedCharacters);
        Assert.Single(dashboard.Prompts);

        var worldDto = dashboard.Worlds.Single();
        Assert.Equal(world.Id, worldDto.Id);
        Assert.Equal(rootArticle.Id, worldDto.WorldRootArticleId);
        Assert.Equal(2, worldDto.Campaigns.Count);
        Assert.Single(worldDto.MyCharacters);

        var activeCampaign = worldDto.Campaigns.First(c => c.Id == campaignA.Id);
        Assert.NotNull(activeCampaign.CurrentArc);
        Assert.Equal(1, activeCampaign.SessionCount);

        var inactiveCampaign = worldDto.Campaigns.First(c => c.Id == campaignB.Id);
        Assert.Null(inactiveCampaign.CurrentArc);
        Assert.Equal(1, inactiveCampaign.SessionCount);
    }

    [Fact]
    public async Task GetDashboardAsync_WithNoMembership_ReturnsEmptyCollections()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var promptService = Substitute.For<IPromptService>();
        promptService.GeneratePrompts(Arg.Any<DashboardDto>()).Returns([]);
        var sut = new DashboardReadService(db, promptService);

        var dashboard = await sut.GetDashboardAsync(Guid.NewGuid(), "Empty");

        Assert.Empty(dashboard.Worlds);
        Assert.Empty(dashboard.ClaimedCharacters);
        Assert.Empty(dashboard.Prompts);
    }
}
