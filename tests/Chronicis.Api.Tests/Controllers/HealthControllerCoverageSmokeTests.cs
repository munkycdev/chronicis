using System.Reflection;
using System.Reflection.Emit;
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
    public void HealthController_GetHealth_ReturnsOkWithVersion()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var healthService = Substitute.For<ISystemHealthService>();
        var readinessService = Substitute.For<IHealthReadinessService>();
        var sut = new HealthController(
            NullLogger<HealthController>.Instance,
            config,
            healthService,
            readinessService);

        var result = sut.GetHealth();

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = ok.Value!.ToString()!;
        Assert.Contains("version", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HealthController_GetApiVersion_ReturnsNonEmptyString()
    {
        // GetApiVersion reads AssemblyInformationalVersion; in tests that resolves
        // to the test assembly version, which is non-empty and non-null.
        var version = HealthController.GetApiVersion();

        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }

    [Fact]
    public void HealthController_GetApiVersion_WithoutInformationalAttribute_ReturnsFallback()
    {
        var assemblyName = new AssemblyName($"NoInfoVersion{Guid.NewGuid():N}");
        var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

        var version = HealthController.GetApiVersion(dynamicAssembly);

        Assert.Equal("0.0.0", version);
    }
}
