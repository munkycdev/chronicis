using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class CampaignArcsControllerCoverageSmokeTests
{
    [Fact]
    public async Task CampaignArcsController_GetArcsByCampaign_ReturnsOk()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var service = Substitute.For<IArcService>();
        service.GetArcsByCampaignAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns([new ArcDto { Id = Guid.NewGuid(), Name = "Arc" }]);
        var sut = new CampaignArcsController(service, user, NullLogger<CampaignArcsController>.Instance);

        var result = await sut.GetArcsByCampaign(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
