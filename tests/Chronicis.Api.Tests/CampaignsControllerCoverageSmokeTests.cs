using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class CampaignsControllerCoverageSmokeTests
{
    [Fact]
    public async Task CampaignsController_GetCampaign_ReturnsOk()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var service = Substitute.For<ICampaignService>();
        var campaignId = Guid.NewGuid();
        service.GetCampaignAsync(campaignId, Arg.Any<Guid>())
            .Returns(new CampaignDetailDto { Id = campaignId, Name = "Campaign" });
        var sut = new CampaignsController(service, user, NullLogger<CampaignsController>.Instance);

        var result = await sut.GetCampaign(campaignId);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
