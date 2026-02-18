using Chronicis.Api.Controllers;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Quests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class QuestUpdatesControllerCoverageSmokeTests
{
    [Fact]
    public async Task QuestUpdatesController_GetQuestUpdates_ReturnsOk_OnSuccess()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var service = Substitute.For<IQuestUpdateService>();
        service.GetQuestUpdatesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), 0, 20)
            .Returns(ServiceResult<PagedResult<QuestUpdateEntryDto>>.Success(new PagedResult<QuestUpdateEntryDto>
            {
                Items = [new QuestUpdateEntryDto { Id = Guid.NewGuid(), Body = "Update" }],
                TotalCount = 1,
                Skip = 0,
                Take = 20
            }));
        var sut = new QuestUpdatesController(service, user, NullLogger<QuestUpdatesController>.Instance);

        var result = await sut.GetQuestUpdates(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
