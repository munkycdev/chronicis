using Chronicis.Api.Controllers;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ImagesControllerCoverageSmokeTests
{
    [Fact]
    public async Task ImagesController_GetImage_ReturnsNotFound_WhenMissing()
    {
        var imageAccess = Substitute.For<IImageAccessService>();
        imageAccess.GetImageDownloadUrlAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(ServiceResult<string>.NotFound());

        var sut = new ImagesController(
            imageAccess,
            ControllerCoverageTestFixtures.CreateCurrentUserService());

        var result = await sut.GetImage(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }
}
