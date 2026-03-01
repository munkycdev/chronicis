using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArcSummaryControllerCoverageSmokeTests
{
    [Fact]
    public async Task ArcSummaryController_GetSummary_ReturnsNotFound_WhenNoAccess()
    {
        var summaryService = Substitute.For<ISummaryService>();
        var accessService = Substitute.For<ISummaryAccessService>();
        accessService.CanAccessArcAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        var sut = new ArcSummaryController(
            summaryService,
            accessService,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<ArcSummaryController>.Instance);

        var result = await sut.GetSummary(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
