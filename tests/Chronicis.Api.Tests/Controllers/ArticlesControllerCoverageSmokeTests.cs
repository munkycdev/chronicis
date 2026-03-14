using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Api.Services.Articles;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArticlesControllerCoverageSmokeTests
{
    [Fact]
    public async Task ArticlesController_GetRootArticles_ReturnsOk()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var articleService = Substitute.For<IArticleService>();
        articleService.GetRootArticlesAsync(Arg.Any<Guid>(), Arg.Any<Guid?>()).Returns([]);

        var sut = new ArticlesController(
            articleService,
            Substitute.For<IArticleValidationService>(),
            Substitute.For<ILinkSyncService>(),
            Substitute.For<IAutoLinkService>(),
            Substitute.For<IArticleExternalLinkService>(),
            Substitute.For<IArticleHierarchyService>(),
            Substitute.For<IArticleDataAccessService>(),
            Substitute.For<IWorldMapService>(),
            user,
            NullLogger<ArticlesController>.Instance);

        var result = await sut.GetRootArticles(null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateArticle_SessionNote_SyncsMapFeatures()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var validation = Substitute.For<IArticleValidationService>();
        validation.ValidateCreateAsync(Arg.Any<ArticleCreateDto>())
            .Returns(new ValidationResult());
        var dataAccess = Substitute.For<IArticleDataAccessService>();
        var worldMapService = Substitute.For<IWorldMapService>();

        var sut = new ArticlesController(
            Substitute.For<IArticleService>(),
            validation,
            Substitute.For<ILinkSyncService>(),
            Substitute.For<IAutoLinkService>(),
            Substitute.For<IArticleExternalLinkService>(),
            Substitute.For<IArticleHierarchyService>(),
            dataAccess,
            worldMapService,
            user,
            NullLogger<ArticlesController>.Instance);

        var featureId = Guid.NewGuid();
        var dto = new ArticleCreateDto
        {
            WorldId = Guid.NewGuid(),
            Title = "Session 3",
            Type = ArticleType.SessionNote,
            Body = $"<p><span data-type=\"map-feature-link\" data-feature-id=\"{featureId}\" data-map-id=\"{Guid.NewGuid()}\" data-display=\"Blackroot Ford\"></span></p>"
        };

        var result = await sut.CreateArticle(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        await worldMapService.Received(1).SyncSessionNoteMapFeaturesAsync(
            dto.WorldId.Value,
            Arg.Any<Guid>(),
            Arg.Is<IEnumerable<Guid>>(ids => ids.Single() == featureId),
            Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateArticle_SessionNote_WithWhitespaceBody_SyncsEmptyMapFeatures()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var validation = Substitute.For<IArticleValidationService>();
        validation.ValidateCreateAsync(Arg.Any<ArticleCreateDto>())
            .Returns(new ValidationResult());
        var worldMapService = Substitute.For<IWorldMapService>();

        var sut = new ArticlesController(
            Substitute.For<IArticleService>(),
            validation,
            Substitute.For<ILinkSyncService>(),
            Substitute.For<IAutoLinkService>(),
            Substitute.For<IArticleExternalLinkService>(),
            Substitute.For<IArticleHierarchyService>(),
            Substitute.For<IArticleDataAccessService>(),
            worldMapService,
            user,
            NullLogger<ArticlesController>.Instance);

        var dto = new ArticleCreateDto
        {
            WorldId = Guid.NewGuid(),
            Title = "Session 4",
            Type = ArticleType.SessionNote,
            Body = "   "
        };

        var result = await sut.CreateArticle(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        await worldMapService.Received(1).SyncSessionNoteMapFeaturesAsync(
            dto.WorldId.Value,
            Arg.Any<Guid>(),
            Arg.Is<IEnumerable<Guid>>(ids => !ids.Any()),
            Arg.Any<Guid>());
    }

    [Fact]
    public async Task UpdateArticle_NonSessionNote_DoesNotSyncMapFeatures()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var validation = Substitute.For<IArticleValidationService>();
        validation.ValidateUpdateAsync(Arg.Any<Guid>(), Arg.Any<ArticleUpdateDto>())
            .Returns(new ValidationResult());
        var dataAccess = Substitute.For<IArticleDataAccessService>();
        var articleId = Guid.NewGuid();
        dataAccess.FindReadableArticleAsync(articleId, Arg.Any<Guid>()).Returns(new Article
        {
            Id = articleId,
            Title = "Lore",
            Slug = "lore",
            WorldId = Guid.NewGuid(),
            Type = ArticleType.WikiArticle,
            Body = "<p>body</p>"
        });
        var articleService = Substitute.For<IArticleService>();
        articleService.GetArticleDetailAsync(articleId, Arg.Any<Guid>()).Returns(new ArticleDto { Id = articleId });
        var worldMapService = Substitute.For<IWorldMapService>();

        var sut = new ArticlesController(
            articleService,
            validation,
            Substitute.For<ILinkSyncService>(),
            Substitute.For<IAutoLinkService>(),
            Substitute.For<IArticleExternalLinkService>(),
            Substitute.For<IArticleHierarchyService>(),
            dataAccess,
            worldMapService,
            user,
            NullLogger<ArticlesController>.Instance);

        var result = await sut.UpdateArticle(articleId, new ArticleUpdateDto
        {
            Title = "Lore",
            Body = "<p>No map features</p>"
        });

        Assert.IsType<OkObjectResult>(result.Result);
        await worldMapService.DidNotReceiveWithAnyArgs().SyncSessionNoteMapFeaturesAsync(default, default, default!, default);
    }
}
