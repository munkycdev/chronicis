using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ResourceProvidersControllerCoverageSmokeTests
{
    [Fact]
    public async Task ResourceProvidersController_GetWorldProviders_ReturnsOk()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var service = Substitute.For<IResourceProviderService>();
        service.GetWorldProvidersAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(
            [
                (new ResourceProvider
                {
                    Code = "srd",
                    Name = "SRD",
                    Description = "desc",
                    DocumentationLink = "https://example.test/docs",
                    License = "OGL"
                }, true)
            ]);

        var sut = new ResourceProvidersController(service, user, NullLogger<ResourceProvidersController>.Instance);

        var result = await sut.GetWorldProviders(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result);
    }
}
