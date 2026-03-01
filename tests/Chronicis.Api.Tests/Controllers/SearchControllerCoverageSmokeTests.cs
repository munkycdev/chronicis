using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class SearchControllerCoverageSmokeTests
{
    [Fact]
    public async Task SearchController_ShortQuery_ReturnsEmptyPayload()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        var searchReadService = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());
        var sut = new SearchController(searchReadService, user, NullLogger<SearchController>.Instance);

        var result = await sut.Search("a");
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<GlobalSearchResultsDto>(ok.Value);
        Assert.Equal(0, payload.TotalResults);
    }

    [Fact]
    public async Task SearchController_ExcludesTutorialArticles_FromSearchResults()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var currentUserService = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var currentUser = await currentUserService.GetRequiredUserAsync();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildBreadcrumbsAsync(Arg.Any<Guid>(), Arg.Any<HierarchyWalkOptions>())
            .Returns(new List<BreadcrumbDto>());

        var tutorialWorld = TestHelpers.CreateWorld(id: Guid.Empty, ownerId: currentUser.Id, name: "Tutorial System");
        var normalWorld = TestHelpers.CreateWorld(ownerId: currentUser.Id, name: "Normal World");
        db.Worlds.AddRange(tutorialWorld, normalWorld);
        db.WorldMembers.AddRange(
            TestHelpers.CreateWorldMember(worldId: Guid.Empty, userId: currentUser.Id),
            TestHelpers.CreateWorldMember(worldId: normalWorld.Id, userId: currentUser.Id));

        db.Articles.AddRange(
            TestHelpers.CreateArticle(
                worldId: Guid.Empty,
                createdBy: currentUser.Id,
                title: "Root Tutorial Search",
                slug: "root-tutorial-search",
                type: ArticleType.Tutorial),
            TestHelpers.CreateArticle(
                worldId: normalWorld.Id,
                createdBy: currentUser.Id,
                title: "Root Normal Search",
                slug: "root-normal-search",
                type: ArticleType.WikiArticle));
        await db.SaveChangesAsync();

        var searchReadService = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());
        var sut = new SearchController(searchReadService, currentUserService, NullLogger<SearchController>.Instance);

        var result = await sut.Search("root");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<GlobalSearchResultsDto>(ok.Value);
        Assert.DoesNotContain(payload.TitleMatches, m => m.Title.Contains("Tutorial", StringComparison.Ordinal));
        Assert.Contains(payload.TitleMatches, m => m.Title.Contains("Normal", StringComparison.Ordinal));
    }
}
