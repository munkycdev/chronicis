using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class UsersControllerCoverageSmokeTests
{
    [Fact]
    public async Task UsersController_GetCurrentUserProfile_ReturnsOk()
    {
        var userService = Substitute.For<IUserService>();
        var currentUser = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var knownUser = await currentUser.GetRequiredUserAsync();
        userService.GetUserProfileAsync(knownUser.Id).Returns(new UserProfileDto
        {
            Id = knownUser.Id,
            DisplayName = knownUser.DisplayName,
            Email = knownUser.Email
        });
        var sut = new UsersController(userService, currentUser, NullLogger<UsersController>.Instance);

        var result = await sut.GetCurrentUserProfile();

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
