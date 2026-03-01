using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArticleSummaryControllerCoverageSmokeTests
{
    [Fact]
    public async Task ArticleSummaryController_GetSummary_ReturnsNotFound_WhenNoAccess()
    {
        var summaryService = Substitute.For<ISummaryService>();
        var accessService = Substitute.For<ISummaryAccessService>();
        accessService.CanAccessArticleAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        var sut = new ArticleSummaryController(
            summaryService,
            accessService,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<ArticleSummaryController>.Instance);

        var result = await sut.GetSummary(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
