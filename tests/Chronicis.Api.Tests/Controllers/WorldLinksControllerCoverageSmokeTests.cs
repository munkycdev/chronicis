using Chronicis.Api.Controllers;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldLinksControllerCoverageSmokeTests
{
    [Fact]
    public async Task WorldLinksController_GetWorldLinks_ReturnsNotFound_WhenNoWorldAccess()
    {
        var worldLinkService = Substitute.For<IWorldLinkService>();
        worldLinkService.GetWorldLinksAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(ServiceResult<List<WorldLinkDto>>.NotFound());

        var sut = new WorldLinksController(
            worldLinkService,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<WorldLinksController>.Instance);

        var result = await sut.GetWorldLinks(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
