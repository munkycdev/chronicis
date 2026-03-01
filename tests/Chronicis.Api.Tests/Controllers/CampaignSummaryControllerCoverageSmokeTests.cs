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
        var summaryService = Substitute.For<ISummaryService>();
        var accessService = Substitute.For<ISummaryAccessService>();
        accessService.CanAccessCampaignAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);
        var sut = new CampaignSummaryController(
            summaryService,
            accessService,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<CampaignSummaryController>.Instance);

        var result = await sut.GetSummary(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
