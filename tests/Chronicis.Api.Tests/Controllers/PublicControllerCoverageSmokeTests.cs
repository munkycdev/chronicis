using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class PublicControllerCoverageSmokeTests
{
    [Fact]
    public async Task PublicController_GetPublicWorld_EmptySlug_ReturnsBadRequest()
    {
        var service = Substitute.For<IPublicWorldService>();
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicWorld("");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
