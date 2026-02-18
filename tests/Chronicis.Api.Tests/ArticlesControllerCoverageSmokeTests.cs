using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Api.Services.Articles;
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
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
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
            db,
            user,
            Substitute.For<IWorldDocumentService>(),
            NullLogger<ArticlesController>.Instance);

        var result = await sut.GetRootArticles(null);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
