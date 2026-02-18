using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class SummaryControllerCoverageSmokeTests
{
    [Fact]
    public async Task SummaryController_GetTemplates_ReturnsOk()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var service = Substitute.For<ISummaryService>();
        service.GetTemplatesAsync().Returns([]);
        var sut = new SummaryController(service, user, NullLogger<SummaryController>.Instance);

        var result = await sut.GetTemplates();

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
