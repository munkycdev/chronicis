using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldActiveContextControllerCoverageSmokeTests
{
    [Fact]
    public async Task WorldActiveContextController_GetActiveContext_ReturnsOk()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var service = Substitute.For<ICampaignService>();
        service.GetActiveContextAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new ActiveContextDto());
        var sut = new WorldActiveContextController(service, user, NullLogger<WorldActiveContextController>.Instance);

        var result = await sut.GetActiveContext(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
