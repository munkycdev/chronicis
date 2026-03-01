using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldLinkSuggestionServiceTests
{
    [Fact]
    public async Task GetSuggestionsAsync_ReturnsForbidden_WhenUserHasNoMembership()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        var sut = new WorldLinkSuggestionService(db, hierarchy);

        var result = await sut.GetSuggestionsAsync(Guid.NewGuid(), "query", Guid.NewGuid());

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsTitleAndAliasMatches_WithDisplayPaths()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        var titleMatch = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: "Dragon Lord",
            type: ArticleType.WikiArticle);

        var aliasMatch = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: "Mage Scholar",
            type: ArticleType.WikiArticle);

        var tutorial = TestHelpers.CreateArticle(
            worldId: Guid.Empty,
            createdBy: userId,
            title: "Dragon Tutorial",
            type: ArticleType.Tutorial);

        db.Articles.AddRange(titleMatch, aliasMatch, tutorial);
        db.ArticleAliases.AddRange(
            new ArticleAlias
            {
                Id = Guid.NewGuid(),
                ArticleId = aliasMatch.Id,
                AliasText = "dragon mage",
                CreatedAt = DateTime.UtcNow
            },
            new ArticleAlias
            {
                Id = Guid.NewGuid(),
                ArticleId = titleMatch.Id,
                AliasText = "dragon duplicate",
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildDisplayPathAsync(Arg.Any<Guid>()).Returns(call => $"path:{call.Arg<Guid>()}");

        var sut = new WorldLinkSuggestionService(db, hierarchy);
        var result = await sut.GetSuggestionsAsync(world.Id, "dragon", userId);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);

        var titleSuggestion = result.Value.Single(s => s.ArticleId == titleMatch.Id);
        Assert.Null(titleSuggestion.MatchedAlias);
        Assert.Equal($"path:{titleMatch.Id}", titleSuggestion.DisplayPath);

        var aliasSuggestion = result.Value.Single(s => s.ArticleId == aliasMatch.Id);
        Assert.Equal("dragon mage", aliasSuggestion.MatchedAlias);
        Assert.Equal($"path:{aliasMatch.Id}", aliasSuggestion.DisplayPath);
    }
}
