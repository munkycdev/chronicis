using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Api.Services.Articles;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
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
    public async Task ArticlesController_GetReadableArticlesQuery_IncludesReadableWorldArticlesAndTutorials()
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
        db.Articles.AddRange(
            new Article
            {
                Id = Guid.NewGuid(),
                Title = "Readable World Article",
                Slug = "readable-world-article",
                WorldId = readableWorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                CreatedBy = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Article
            {
                Id = Guid.NewGuid(),
                Title = "Unreadable World Article",
                Slug = "unreadable-world-article",
                WorldId = unreadableWorldId,
                Type = ArticleType.WikiArticle,
                Visibility = ArticleVisibility.Public,
                CreatedBy = otherUserId,
                CreatedAt = DateTime.UtcNow
            },
            new Article
            {
                Id = Guid.NewGuid(),
                Title = "Tutorial Article",
                Slug = "tutorial-article",
                WorldId = Guid.Empty,
                Type = ArticleType.Tutorial,
                Visibility = ArticleVisibility.Public,
                CreatedBy = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            });
        db.SaveChanges();

        var sut = new ArticlesController(
            Substitute.For<IArticleService>(),
            Substitute.For<IArticleValidationService>(),
            Substitute.For<ILinkSyncService>(),
            Substitute.For<IAutoLinkService>(),
            Substitute.For<IArticleExternalLinkService>(),
            Substitute.For<IArticleHierarchyService>(),
            db,
            user,
            Substitute.For<IWorldDocumentService>(),
            NullLogger<ArticlesController>.Instance);

        var method = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(ArticlesController), "GetReadableArticlesQuery");
        var query = (IQueryable<Article>)method.Invoke(sut, [currentUser.Id])!;
        var ids = query.AsNoTracking().Select(a => a.Title).ToList();

        Assert.Contains("Readable World Article", ids);
        Assert.Contains("Tutorial Article", ids);
        Assert.DoesNotContain("Unreadable World Article", ids);
    }
}
