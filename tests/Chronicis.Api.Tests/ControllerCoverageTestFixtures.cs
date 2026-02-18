using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Chronicis.Api.Tests;

internal static class ControllerCoverageTestFixtures
{
    internal static ChronicisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase($"controllers-coverage-{Guid.NewGuid()}")
            .Options;
        return new ChronicisDbContext(options);
    }

    internal static ICurrentUserService CreateCurrentUserService()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test User",
            Email = "test@example.com",
            Auth0UserId = "auth0|test-user"
        };

        var service = Substitute.For<ICurrentUserService>();
        service.GetRequiredUserAsync().Returns(user);
        service.GetCurrentUserAsync().Returns(user);
        return service;
    }
}
