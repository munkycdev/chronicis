using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class CharactersControllerCoverageSmokeTests
{
    [Fact]
    public async Task CharactersController_GetClaimStatus_ReturnsNotFound_WhenMissing()
    {
        var claimService = Substitute.For<ICharacterClaimService>();
        claimService.GetClaimStatusAsync(Arg.Any<Guid>()).Returns((false, (Guid?)null, (string?)null));
        var sut = new CharactersController(
            claimService,
            ControllerCoverageTestFixtures.CreateCurrentUserService(),
            NullLogger<CharactersController>.Instance);

        var result = await sut.GetClaimStatus(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
