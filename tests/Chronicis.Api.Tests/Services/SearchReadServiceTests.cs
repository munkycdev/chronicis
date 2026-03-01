using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class SearchReadServiceTests
{
    [Fact]
    public async Task SearchAsync_RespectsUnifiedReadPolicy_ForPrivateAndMembershipRules()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildBreadcrumbsAsync(Arg.Any<Guid>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new List<BreadcrumbDto>()));

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var readableWorld = TestHelpers.CreateWorld(ownerId: userId);
        var unreadableWorld = TestHelpers.CreateWorld(ownerId: Guid.NewGuid());
        db.Worlds.AddRange(readableWorld, unreadableWorld);

        db.WorldMembers.AddRange(
            TestHelpers.CreateWorldMember(worldId: readableWorld.Id, userId: userId),
            TestHelpers.CreateWorldMember(worldId: readableWorld.Id, userId: otherUserId));

        const string term = "sharedterm";

        var publicReadable = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: otherUserId,
            title: $"{term}-public",
            visibility: ArticleVisibility.Public,
            type: ArticleType.WikiArticle);

        var privateOwned = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: userId,
            title: $"{term}-private-owned",
            visibility: ArticleVisibility.Private,
            type: ArticleType.WikiArticle);

        var privateNotOwned = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: otherUserId,
            title: $"{term}-private-other",
            visibility: ArticleVisibility.Private,
            type: ArticleType.WikiArticle);

        var unreadableWorldArticle = TestHelpers.CreateArticle(
            worldId: unreadableWorld.Id,
            createdBy: unreadableWorld.OwnerId,
            title: $"{term}-other-world",
            visibility: ArticleVisibility.Public,
            type: ArticleType.WikiArticle);

        var tutorial = TestHelpers.CreateArticle(
            worldId: Guid.Empty,
            createdBy: otherUserId,
            title: $"{term}-tutorial",
            visibility: ArticleVisibility.Public,
            type: ArticleType.Tutorial);

        db.Articles.AddRange(publicReadable, privateOwned, privateNotOwned, unreadableWorldArticle, tutorial);
        await db.SaveChangesAsync();

        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());

        var result = await sut.SearchAsync(term, userId);
        var resultIds = result.TitleMatches.Select(m => m.Id).ToList();

        Assert.Contains(publicReadable.Id, resultIds);
        Assert.Contains(privateOwned.Id, resultIds);
        Assert.DoesNotContain(privateNotOwned.Id, resultIds);
        Assert.DoesNotContain(unreadableWorldArticle.Id, resultIds);
        Assert.DoesNotContain(tutorial.Id, resultIds);
    }
}
