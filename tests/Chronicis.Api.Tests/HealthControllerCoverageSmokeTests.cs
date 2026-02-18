using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class HealthControllerCoverageSmokeTests
{
    [Fact]
    public void HealthController_GetHealth_ReturnsOk()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var healthService = Substitute.For<ISystemHealthService>();
        var sut = new HealthController(db, NullLogger<HealthController>.Instance, config, healthService);

        var result = sut.GetHealth();

        Assert.IsType<OkObjectResult>(result);
    }
}
