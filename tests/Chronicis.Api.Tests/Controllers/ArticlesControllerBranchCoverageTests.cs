using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArticlesControllerBranchCoverageTests
{
    [Fact]
    public void ArticlesController_ParseAliases_CoversBranches()
    {
        var parse = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ArticlesController), "ParseAliases");

        var empty = (List<string>)parse.Invoke(null, [null])!;
        Assert.Empty(empty);

        var parsed = (List<string>)parse.Invoke(null, ["  one, two,One,   ," + new string('x', 201)])!;
        Assert.Equal(2, parsed.Count);
        Assert.Contains("one", parsed, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("two", parsed, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArticleDataAccessService_ResolveReadableLinks_IncludesReadableWorldArticlesAndTutorials()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var currentUser = await user.GetRequiredUserAsync();
        var otherUserId = Guid.NewGuid();
        var readableWorldId = Guid.NewGuid();
        var unreadableWorldId = Guid.NewGuid();

        db.Worlds.AddRange(
            new World { Id = readableWorldId, Name = "Readable", OwnerId = currentUser.Id, Slug = "readable", CreatedAt = DateTime.UtcNow },
            new World { Id = unreadableWorldId, Name = "Unreadable", OwnerId = otherUserId, Slug = "unreadable", CreatedAt = DateTime.UtcNow });
        db.WorldMembers.Add(new WorldMember { Id = Guid.NewGuid(), WorldId = readableWorldId, UserId = currentUser.Id, Role = WorldRole.GM, JoinedAt = DateTime.UtcNow });

        var readableArticle = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Readable World Article",
            Slug = "readable-world-article",
            WorldId = readableWorldId,
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            CreatedBy = currentUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        var unreadableArticle = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Unreadable World Article",
            Slug = "unreadable-world-article",
            WorldId = unreadableWorldId,
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            CreatedBy = otherUserId,
            CreatedAt = DateTime.UtcNow
        };

        var tutorialArticle = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Tutorial Article",
            Slug = "tutorial-article",
            WorldId = Guid.Empty,
            Type = ArticleType.Tutorial,
            Visibility = ArticleVisibility.Public,
            CreatedBy = currentUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        db.Articles.AddRange(
            readableArticle,
            unreadableArticle,
            tutorialArticle);
        db.SaveChanges();

        var service = new ArticleDataAccessService(db, NSubstitute.Substitute.For<IWorldDocumentService>());

        var result = await service.ResolveReadableLinksAsync(
            [readableArticle.Id, unreadableArticle.Id, tutorialArticle.Id],
            currentUser.Id);

        var titles = result.Select(r => r.Title).ToList();
        Assert.Contains("Readable World Article", titles);
        Assert.Contains("Tutorial Article", titles);
        Assert.DoesNotContain("Unreadable World Article", titles);
    }
}
