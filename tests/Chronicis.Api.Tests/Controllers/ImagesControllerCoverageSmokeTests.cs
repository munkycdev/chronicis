using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ImagesControllerCoverageSmokeTests
{
    [Fact]
    public async Task ImagesController_GetImage_ReturnsNotFound_WhenMissing()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var blob = Substitute.For<IBlobStorageService>();
        var sut = new ImagesController(
            db,
            blob,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<ImagesController>.Instance);

        var result = await sut.GetImage(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }
}
