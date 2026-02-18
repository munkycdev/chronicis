using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class CampaignSummaryControllerCoverageSmokeTests
{
    [Fact]
    public async Task CampaignSummaryController_GetSummary_ReturnsNotFound_WhenNoAccess()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var summaryService = Substitute.For<ISummaryService>();
        var sut = new CampaignSummaryController(
            summaryService,
            db,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<CampaignSummaryController>.Instance);

        var result = await sut.GetSummary(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
