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
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var promptService = Substitute.For<IPromptService>();
        promptService.GeneratePrompts(Arg.Any<DashboardDto>()).Returns([]);
        var sut = new DashboardController(
            db,
            promptService,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<DashboardController>.Instance);

        var result = await sut.GetDashboard();

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
