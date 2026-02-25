using Chronicis.Api.Controllers;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
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

    [Fact]
    public async Task SessionsController_GenerateAiSummary_ReturnsOk_OnSuccess()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var sessionService = Substitute.For<ISessionService>();
        var sessionId = Guid.NewGuid();

        sessionService.GenerateAiSummaryAsync(sessionId, Arg.Any<Guid>())
            .Returns(ServiceResult<SummaryGenerationDto>.Success(new SummaryGenerationDto
            {
                Success = true,
                Summary = "Summary"
            }));

        var sut = new SessionsController(sessionService, user, NullLogger<SessionsController>.Instance);

        var result = await sut.GenerateAiSummary(sessionId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task SessionsController_DeleteSession_ReturnsNoContent_OnSuccess()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var sessionService = Substitute.For<ISessionService>();
        var sessionId = Guid.NewGuid();

        sessionService.DeleteSessionAsync(sessionId, Arg.Any<Guid>())
            .Returns(ServiceResult<bool>.Success(true));

        var sut = new SessionsController(sessionService, user, NullLogger<SessionsController>.Instance);

        var result = await sut.DeleteSession(sessionId);

        Assert.IsType<NoContentResult>(result);
    }
}
