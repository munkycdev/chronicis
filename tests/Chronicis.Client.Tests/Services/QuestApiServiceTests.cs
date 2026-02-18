using System.Net;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Quests;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class QuestApiServiceTests
{
    [Fact]
    public async Task BasicMethods_UseExpectedRoutes()
    {
        var calls = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add($"{req.Method} {req.RequestUri!.PathAndQuery.TrimStart('/')}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var snackbar = Substitute.For<ISnackbar>();
        var sut = new QuestApiService(http, NullLogger<QuestApiService>.Instance, snackbar);
        var id = Guid.NewGuid();

        await sut.GetArcQuestsAsync(id);
        await sut.GetQuestAsync(id);
        await sut.CreateQuestAsync(id, new QuestCreateDto { Title = "Q" });
        await sut.DeleteQuestAsync(id);
        await sut.GetQuestUpdatesAsync(id, 1, 2);
        await sut.AddQuestUpdateAsync(id, new QuestUpdateCreateDto { Body = "u" });
        await sut.DeleteQuestUpdateAsync(id, Guid.NewGuid());

        Assert.Contains(calls, c => c.Contains($"GET arcs/{id}/quests"));
        Assert.Contains(calls, c => c.Contains($"GET quests/{id}"));
        Assert.Contains(calls, c => c.Contains($"POST arcs/{id}/quests"));
        Assert.Contains(calls, c => c.Contains($"DELETE quests/{id}"));
        Assert.Contains(calls, c => c.Contains($"GET quests/{id}/updates?skip=1&take=2"));
        Assert.Contains(calls, c => c.Contains($"POST quests/{id}/updates"));
    }

    [Fact]
    public async Task UpdateQuestAsync_HandlesConflict_WithCurrentQuest()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.Conflict, "{}");
        var snackbar = Substitute.For<ISnackbar>();
        var sut = new QuestApiService(http, NullLogger<QuestApiService>.Instance, snackbar);

        var result = await sut.UpdateQuestAsync(Guid.NewGuid(), new QuestEditDto { Title = "x" });

        Assert.NotNull(result);
        snackbar.Received().Add(Arg.Any<string>(), Severity.Warning);
    }

    [Fact]
    public async Task UpdateQuestAsync_HandlesConflict_WithNullBody()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.Conflict, "null");
        var snackbar = Substitute.For<ISnackbar>();
        var sut = new QuestApiService(http, NullLogger<QuestApiService>.Instance, snackbar);

        var result = await sut.UpdateQuestAsync(Guid.NewGuid(), new QuestEditDto { Title = "x" });

        Assert.Null(result);
        snackbar.Received().Add(Arg.Any<string>(), Severity.Error);
    }

    [Fact]
    public async Task UpdateQuestAsync_HandlesException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var snackbar = Substitute.For<ISnackbar>();
        var sut = new QuestApiService(http, NullLogger<QuestApiService>.Instance, snackbar);

        var result = await sut.UpdateQuestAsync(Guid.NewGuid(), new QuestEditDto { Title = "x" });

        Assert.Null(result);
        snackbar.Received().Add(Arg.Any<string>(), Severity.Error);
    }

    [Fact]
    public async Task UpdateQuestAsync_ReturnsUpdatedQuest_OnSuccess()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{}");
        var snackbar = Substitute.For<ISnackbar>();
        var sut = new QuestApiService(http, NullLogger<QuestApiService>.Instance, snackbar);

        var result = await sut.UpdateQuestAsync(Guid.NewGuid(), new QuestEditDto { Title = "x" });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetQuestUpdatesAsync_ReturnsEmptyPagedResult_WhenApiReturnsNull()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null");
        var snackbar = Substitute.For<ISnackbar>();
        var sut = new QuestApiService(http, NullLogger<QuestApiService>.Instance, snackbar);

        var result = await sut.GetQuestUpdatesAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}

