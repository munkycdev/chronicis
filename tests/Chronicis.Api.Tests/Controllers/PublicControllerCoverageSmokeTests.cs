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

    [Fact]
    public async Task PublicController_GetPublicDocumentContent_NotFound_ReturnsNotFound()
    {
        var service = Substitute.For<IPublicWorldService>();
        service.GetPublicDocumentDownloadUrlAsync(Arg.Any<Guid>()).Returns((string?)null);
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicDocumentContent(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task PublicController_GetPublicDocumentContent_Found_ReturnsRedirect()
    {
        var service = Substitute.For<IPublicWorldService>();
        service.GetPublicDocumentDownloadUrlAsync(Arg.Any<Guid>()).Returns("https://cdn.test/image.png");
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicDocumentContent(Guid.NewGuid());

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://cdn.test/image.png", redirect.Url);
    }
}
