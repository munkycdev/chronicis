using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class TutorialsControllersCoverageTests
{
    [Fact]
    public void TutorialsController_Constructor_CoversLines()
    {
        var service = Substitute.For<ITutorialService>();

        var sut = new TutorialsController(service, NullLogger<TutorialsController>.Instance);

        Assert.NotNull(sut);
    }

    [Fact]
    public void SysAdminTutorialsController_MapInvalidOperation_ReturnsConflict_WhenDuplicateMessage()
    {
        var service = Substitute.For<ITutorialService>();
        var sut = new SysAdminTutorialsController(service, NullLogger<SysAdminTutorialsController>.Instance);
        var method = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(SysAdminTutorialsController), "MapInvalidOperation");

        var result = (ObjectResult)method.Invoke(sut, [new InvalidOperationException("Mapping already exists")])!;

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void SysAdminTutorialsController_MapInvalidOperation_ReturnsBadRequest_WhenOtherMessage()
    {
        var service = Substitute.For<ITutorialService>();
        var sut = new SysAdminTutorialsController(service, NullLogger<SysAdminTutorialsController>.Instance);
        var method = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(SysAdminTutorialsController), "MapInvalidOperation");

        var result = (ObjectResult)method.Invoke(sut, [new InvalidOperationException("Validation failed")])!;

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
