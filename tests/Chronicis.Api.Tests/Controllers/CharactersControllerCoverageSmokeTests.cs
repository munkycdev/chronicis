using Chronicis.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class CharactersControllerCoverageSmokeTests
{
    [Fact]
    public async Task CharactersController_GetClaimStatus_ReturnsNotFound_WhenMissing()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var sut = new CharactersController(db, ControllerCoverageTestFixtures.CreateCurrentUserService(), NullLogger<CharactersController>.Instance);

        var result = await sut.GetClaimStatus(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
