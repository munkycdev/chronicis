using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class DashboardControllerCoverageSmokeTests
{
    [Fact]
    public async Task DashboardController_GetDashboard_ReturnsOk()
    {
        var userService = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var readService = Substitute.For<IDashboardReadService>();
        readService.GetDashboardAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(new DashboardDto());
        var sut = new DashboardController(
            readService,
            userService,
            NullLogger<DashboardController>.Instance);

        var result = await sut.GetDashboard();

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
