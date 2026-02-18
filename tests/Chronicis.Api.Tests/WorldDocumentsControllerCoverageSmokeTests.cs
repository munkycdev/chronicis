using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldDocumentsControllerCoverageSmokeTests
{
    [Fact]
    public async Task WorldDocumentsController_RequestDocumentUpload_NullRequest_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldDocumentService>();
        var sut = new WorldDocumentsController(
            service,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<WorldDocumentsController>.Instance);

        var result = await sut.RequestDocumentUpload(Guid.NewGuid(), null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
