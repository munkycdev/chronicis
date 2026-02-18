using Chronicis.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldLinksControllerCoverageSmokeTests
{
    [Fact]
    public async Task WorldLinksController_GetWorldLinks_ReturnsNotFound_WhenNoWorldAccess()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var sut = new WorldLinksController(db, ControllerCoverageTestFixtures.CreateCurrentUserService(), NullLogger<WorldLinksController>.Instance);

        var result = await sut.GetWorldLinks(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
