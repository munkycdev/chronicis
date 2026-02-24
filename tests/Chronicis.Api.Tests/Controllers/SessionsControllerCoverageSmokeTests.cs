using Chronicis.Api.Controllers;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class SessionsControllerCoverageSmokeTests
{
    [Fact]
    public async Task SessionsController_UpdateSessionNotes_ReturnsOk_OnSuccess()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var sessionService = Substitute.For<ISessionService>();
        var sessionId = Guid.NewGuid();

        sessionService.UpdateSessionNotesAsync(sessionId, Arg.Any<SessionUpdateDto>(), Arg.Any<Guid>())
            .Returns(ServiceResult<SessionDto>.Success(new SessionDto
            {
                Id = sessionId,
                ArcId = Guid.NewGuid(),
                Name = "Session"
            }));

        var sut = new SessionsController(sessionService, user, NullLogger<SessionsController>.Instance);

        var result = await sut.UpdateSessionNotes(sessionId, new SessionUpdateDto());

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
