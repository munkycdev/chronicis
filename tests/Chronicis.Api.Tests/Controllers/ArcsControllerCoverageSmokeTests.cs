using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArcsControllerCoverageSmokeTests
{
    [Fact]
    public async Task ArcsController_GetArc_ReturnsOk()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var service = Substitute.For<IArcService>();
        var arcId = Guid.NewGuid();
        service.GetArcAsync(arcId, Arg.Any<Guid>()).Returns(new ArcDto { Id = arcId, Name = "Arc" });
        var sut = new ArcsController(service, user, NullLogger<ArcsController>.Instance);

        var result = await sut.GetArc(arcId);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
