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
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var summaryService = Substitute.For<ISummaryService>();
        var sut = new ArcSummaryController(
            summaryService,
            db,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<ArcSummaryController>.Instance);

        var result = await sut.GetSummary(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
