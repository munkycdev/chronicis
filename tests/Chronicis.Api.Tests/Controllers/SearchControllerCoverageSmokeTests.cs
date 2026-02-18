using Chronicis.Api.Controllers;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class SearchControllerCoverageSmokeTests
{
    [Fact]
    public async Task SearchController_ShortQuery_ReturnsEmptyPayload()
    {
        using var db = ControllerCoverageTestFixtures.CreateDbContext();
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        var sut = new SearchController(db, user, NullLogger<SearchController>.Instance, hierarchy);

        var result = await sut.Search("a");
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<GlobalSearchResultsDto>(ok.Value);
        Assert.Equal(0, payload.TotalResults);
    }
}
