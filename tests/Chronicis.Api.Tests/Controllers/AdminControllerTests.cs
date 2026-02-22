using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class AdminControllerTests
{
    private readonly IAdminService _adminService;
    private readonly AdminController _sut;

    public AdminControllerTests()
    {
        _adminService = Substitute.For<IAdminService>();
        _sut = new AdminController(_adminService, NullLogger<AdminController>.Instance);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetWorlds
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWorlds_ReturnsOk_WithSummaryList()
    {
        var summaries = new List<AdminWorldSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "World 1" },
            new() { Id = Guid.NewGuid(), Name = "World 2" }
        };
        _adminService.GetAllWorldSummariesAsync().Returns(summaries);

        var result = await _sut.GetWorlds();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(summaries, ok.Value);
    }

    [Fact]
    public async Task GetWorlds_ReturnsForbid_WhenUnauthorizedAccessException()
    {
        _adminService.GetAllWorldSummariesAsync()
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _sut.GetWorlds();

        Assert.IsType<ForbidResult>(result.Result);
    }

    // ────────────────────────────────────────────────────────────────
    //  DeleteWorld
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteWorld_ReturnsNoContent_WhenDeleted()
    {
        var worldId = Guid.NewGuid();
        _adminService.DeleteWorldAsync(worldId).Returns(true);

        var result = await _sut.DeleteWorld(worldId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteWorld_ReturnsNotFound_WhenWorldDoesNotExist()
    {
        var worldId = Guid.NewGuid();
        _adminService.DeleteWorldAsync(worldId).Returns(false);

        var result = await _sut.DeleteWorld(worldId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteWorld_ReturnsForbid_WhenUnauthorizedAccessException()
    {
        var worldId = Guid.NewGuid();
        _adminService.DeleteWorldAsync(worldId)
            .ThrowsAsync(new UnauthorizedAccessException());

        var result = await _sut.DeleteWorld(worldId);

        Assert.IsType<ForbidResult>(result);
    }
}
