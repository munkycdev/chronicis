using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldsControllerCoverageSmokeTests
{
    [Fact]
    public async Task WorldsController_GetWorlds_ReturnsOk()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var worldService = Substitute.For<IWorldService>();
        worldService.GetUserWorldsAsync(Arg.Any<Guid>()).Returns([]);
        var sut = new WorldsController(
            worldService,
            Substitute.For<IWorldMembershipService>(),
            Substitute.For<IWorldInvitationService>(),
            Substitute.For<IWorldPublicSharingService>(),
            Substitute.For<IExportService>(),
            Substitute.For<IArticleHierarchyService>(),
            db,
            user,
            NullLogger<WorldsController>.Instance);

        var result = await sut.GetWorlds();

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
