using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Api.Services.Articles;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArticlesControllerCascadeTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static readonly Guid ArticleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static (ArticlesController Sut,
                    IArticleDataAccessService DataAccess,
                    IArticleService ArticleService,
                    IArticleRenameCascadeService CascadeService)
        BuildSut(Article article)
    {
        var user        = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var validation  = Substitute.For<IArticleValidationService>();
        validation.ValidateUpdateAsync(Arg.Any<Guid>(), Arg.Any<ArticleUpdateDto>())
                  .Returns(new ValidationResult());

        var dataAccess  = Substitute.For<IArticleDataAccessService>();
        dataAccess.FindReadableArticleAsync(ArticleId, Arg.Any<Guid>()).Returns(article);

        var articleService = Substitute.For<IArticleService>();
        articleService.GetArticleDetailAsync(ArticleId, Arg.Any<Guid>())
                      .Returns(new ArticleDto { Id = ArticleId });

        var cascadeService = Substitute.For<IArticleRenameCascadeService>();

        var sut = new ArticlesController(
            articleService,
            validation,
            Substitute.For<ILinkSyncService>(),
            Substitute.For<IAutoLinkService>(),
            Substitute.For<IArticleExternalLinkService>(),
            Substitute.For<IArticleHierarchyService>(),
            dataAccess,
            Substitute.For<IWorldMapService>(),
            user,
            cascadeService,
            NullLogger<ArticlesController>.Instance);

        return (sut, dataAccess, articleService, cascadeService);
    }

    private static Article WikiArticle(string title) => new()
    {
        Id             = ArticleId,
        Title          = title,
        Slug           = "slug",
        WorldId        = Guid.NewGuid(),
        Type           = ArticleType.WikiArticle,
        Visibility     = ArticleVisibility.MembersOnly,
        Body           = "<p>body</p>",
        CreatedBy      = Guid.NewGuid(),
        CreatedAt      = DateTime.UtcNow,
        ModifiedAt     = DateTime.UtcNow,
        LastModifiedBy = Guid.NewGuid(),
    };
    // ── cascade triggered ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateArticle_CallsCascadeService_WhenTitleChanges()
    {
        var (sut, _, _, cascade) = BuildSut(WikiArticle("Old Title"));

        var result = await sut.UpdateArticle(ArticleId, new ArticleUpdateDto { Title = "New Title" });

        Assert.IsType<OkObjectResult>(result.Result);
        await cascade.Received(1).CascadeTitleChangeAsync(
            ArticleId,
            "Old Title",
            "New Title",
            Arg.Any<CancellationToken>());
    }

    // ── cascade suppressed ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateArticle_DoesNotCallCascade_WhenTitleUnchanged()
    {
        var (sut, _, _, cascade) = BuildSut(WikiArticle("Same Title"));

        await sut.UpdateArticle(ArticleId, new ArticleUpdateDto { Title = "Same Title" });

        await cascade.DidNotReceiveWithAnyArgs()
                     .CascadeTitleChangeAsync(default, default!, default!, default);
    }

    [Fact]
    public async Task UpdateArticle_DoesNotCallCascade_WhenDtoTitleIsNull()
    {
        // Title is non-nullable in ArticleUpdateDto but the defensive null guard
        // in the controller exists for JSON deserialization edge cases (null token).
        var (sut, _, _, cascade) = BuildSut(WikiArticle("Any Title"));

        await sut.UpdateArticle(ArticleId, new ArticleUpdateDto { Title = null! });

        await cascade.DidNotReceiveWithAnyArgs()
                     .CascadeTitleChangeAsync(default, default!, default!, default);
    }

    [Fact]
    public async Task UpdateArticle_DoesNotCallCascade_WhenOnlyCaseDiffers()
    {
        // Chosen semantics: OrdinalIgnoreCase — case-only renames do NOT cascade.
        var (sut, _, _, cascade) = BuildSut(WikiArticle("dragon keep"));

        await sut.UpdateArticle(ArticleId, new ArticleUpdateDto { Title = "Dragon Keep" });

        await cascade.DidNotReceiveWithAnyArgs()
                     .CascadeTitleChangeAsync(default, default!, default!, default);
    }

    // ── failure mode ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateArticle_Returns500_WhenCascadeThrows()
    {
        // The title rename is already committed; cascade failure propagates as 500.
        // This is the accepted POC failure mode.
        var (sut, _, _, cascade) = BuildSut(WikiArticle("Old Title"));
        cascade.CascadeTitleChangeAsync(default, default!, default!, default)
               .ThrowsAsyncForAnyArgs(new InvalidOperationException("cascade boom"));

        var result = await sut.UpdateArticle(ArticleId, new ArticleUpdateDto { Title = "New Title" });

        Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, ((ObjectResult)result.Result!).StatusCode);
    }
}
