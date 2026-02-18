using Chronicis.Api.Controllers;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Quests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class QuestsControllerCoverageSmokeTests
{
    [Fact]
    public async Task QuestsController_GetQuest_ReturnsOk_OnSuccess()
    {
        var user = ControllerCoverageTestFixtures.CreateCurrentUserService();
        var questService = Substitute.For<IQuestService>();
        var questId = Guid.NewGuid();
        questService.GetQuestAsync(questId, Arg.Any<Guid>())
            .Returns(ServiceResult<QuestDto>.Success(new QuestDto { Id = questId, Title = "Quest" }));
        var sut = new QuestsController(questService, user, NullLogger<QuestsController>.Instance);

        var result = await sut.GetQuest(questId);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
