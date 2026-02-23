using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class WorldLinksViewModelTests
{
    private sealed record Sut(
        WorldLinksViewModel Vm,
        IWorldApiService WorldApi,
        ITreeStateService TreeState,
        IUserNotifier Notifier);

    private static Sut CreateSut()
    {
        var worldApi = Substitute.For<IWorldApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var notifier = Substitute.For<IUserNotifier>();
        var logger = Substitute.For<ILogger<WorldLinksViewModel>>();
        var vm = new WorldLinksViewModel(worldApi, treeState, notifier, logger);
        return new Sut(vm, worldApi, treeState, notifier);
    }

    private static WorldLinkDto MakeLink(string title = "Roll20", string url = "https://roll20.net") =>
        new() { Id = Guid.NewGuid(), Title = title, Url = url };

    // ---------------------------------------------------------------------------
    // LoadAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_PopulatesLinks()
    {
        var c = CreateSut();
        var links = new List<WorldLinkDto> { MakeLink() };
        c.WorldApi.GetWorldLinksAsync(Arg.Any<Guid>()).Returns(links);

        await c.Vm.LoadAsync(Guid.NewGuid());

        Assert.Equal(links, c.Vm.Links);
    }

    // ---------------------------------------------------------------------------
    // StartAddLink / CancelAddLink
    // ---------------------------------------------------------------------------

    [Fact]
    public void StartAddLink_SetsIsAddingLinkTrue()
    {
        var c = CreateSut();
        c.Vm.StartAddLink();
        Assert.True(c.Vm.IsAddingLink);
    }

    [Fact]
    public void CancelAddLink_ClearsStateAndSetsIsAddingLinkFalse()
    {
        var c = CreateSut();
        c.Vm.StartAddLink();
        c.Vm.NewLinkTitle = "X";
        c.Vm.CancelAddLink();

        Assert.False(c.Vm.IsAddingLink);
        Assert.Empty(c.Vm.NewLinkTitle);
    }

    // ---------------------------------------------------------------------------
    // SaveNewLinkAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SaveNewLinkAsync_WhenTitleEmpty_WarnsAndDoesNotCallApi()
    {
        var c = CreateSut();
        await c.Vm.LoadAsync(Guid.NewGuid());
        c.Vm.NewLinkTitle = "";
        c.Vm.NewLinkUrl = "https://roll20.net";

        await c.Vm.SaveNewLinkAsync();

        c.Notifier.Received(1).Warning(Arg.Any<string>());
        await c.WorldApi.DidNotReceive().CreateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<WorldLinkCreateDto>());
    }

    [Fact]
    public async Task SaveNewLinkAsync_WhenInvalidUrl_WarnsAndDoesNotCallApi()
    {
        var c = CreateSut();
        await c.Vm.LoadAsync(Guid.NewGuid());
        c.Vm.NewLinkTitle = "Title";
        c.Vm.NewLinkUrl = "not-a-url";

        await c.Vm.SaveNewLinkAsync();

        c.Notifier.Received(1).Warning(Arg.Any<string>());
        await c.WorldApi.DidNotReceive().CreateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<WorldLinkCreateDto>());
    }

    [Fact]
    public async Task SaveNewLinkAsync_WhenApiReturnsNull_ShowsError()
    {
        var c = CreateSut();
        await c.Vm.LoadAsync(Guid.NewGuid());
        c.Vm.NewLinkTitle = "Title";
        c.Vm.NewLinkUrl = "https://roll20.net";
        c.WorldApi.CreateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<WorldLinkCreateDto>()).Returns((WorldLinkDto?)null);

        await c.Vm.SaveNewLinkAsync();

        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveNewLinkAsync_OnSuccess_ReloadsLinksAndClearsState()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        await c.Vm.LoadAsync(worldId);
        c.Vm.NewLinkTitle = "Roll20";
        c.Vm.NewLinkUrl = "https://roll20.net";
        c.WorldApi.CreateWorldLinkAsync(worldId, Arg.Any<WorldLinkCreateDto>()).Returns(MakeLink());
        var refreshedLinks = new List<WorldLinkDto> { MakeLink("Roll20"), MakeLink("D&D Beyond", "https://dndbeyond.com") };
        c.WorldApi.GetWorldLinksAsync(worldId).Returns(refreshedLinks);

        await c.Vm.SaveNewLinkAsync();

        Assert.False(c.Vm.IsAddingLink);
        Assert.Equal(refreshedLinks, c.Vm.Links);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveNewLinkAsync_WhenApiThrows_ShowsError()
    {
        var c = CreateSut();
        await c.Vm.LoadAsync(Guid.NewGuid());
        c.Vm.NewLinkTitle = "Roll20";
        c.Vm.NewLinkUrl = "https://roll20.net";
        c.WorldApi.CreateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<WorldLinkCreateDto>()).ThrowsAsync(new Exception("boom"));

        await c.Vm.SaveNewLinkAsync();

        Assert.False(c.Vm.IsSavingLink);
        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // StartEditLink / CancelEditLink
    // ---------------------------------------------------------------------------

    [Fact]
    public void StartEditLink_SetsEditingState()
    {
        var c = CreateSut();
        var link = new WorldLinkDto { Id = Guid.NewGuid(), Title = "Roll20", Url = "https://roll20.net", Description = "desc" };
        c.Vm.StartEditLink(link);

        Assert.Equal(link.Id, c.Vm.EditingLinkId);
        Assert.Equal("Roll20", c.Vm.EditLinkTitle);
        Assert.Equal("https://roll20.net", c.Vm.EditLinkUrl);
        Assert.Equal("desc", c.Vm.EditLinkDescription);
    }

    [Fact]
    public void CancelEditLink_ClearsEditingState()
    {
        var c = CreateSut();
        var link = MakeLink();
        c.Vm.StartEditLink(link);
        c.Vm.CancelEditLink();

        Assert.Null(c.Vm.EditingLinkId);
        Assert.Empty(c.Vm.EditLinkTitle);
    }

    // ---------------------------------------------------------------------------
    // SaveEditLinkAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SaveEditLinkAsync_WhenNoEditingId_DoesNothing()
    {
        var c = CreateSut();
        await c.Vm.SaveEditLinkAsync();
        await c.WorldApi.DidNotReceive().UpdateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<WorldLinkUpdateDto>());
    }

    [Fact]
    public async Task SaveEditLinkAsync_WhenTitleEmpty_WarnsAndDoesNotCallApi()
    {
        var c = CreateSut();
        c.Vm.StartEditLink(MakeLink());
        c.Vm.EditLinkTitle = "";

        await c.Vm.SaveEditLinkAsync();

        c.Notifier.Received(1).Warning(Arg.Any<string>());
        await c.WorldApi.DidNotReceive().UpdateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<WorldLinkUpdateDto>());
    }

    [Fact]
    public async Task SaveEditLinkAsync_OnSuccess_ReloadsLinksAndClearsState()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        await c.Vm.LoadAsync(worldId);
        var link = MakeLink();
        c.Vm.StartEditLink(link);
        c.WorldApi.UpdateWorldLinkAsync(worldId, link.Id, Arg.Any<WorldLinkUpdateDto>()).Returns(link);
        var refreshed = new List<WorldLinkDto> { link };
        c.WorldApi.GetWorldLinksAsync(worldId).Returns(refreshed);

        await c.Vm.SaveEditLinkAsync();

        Assert.Null(c.Vm.EditingLinkId);
        Assert.Equal(refreshed, c.Vm.Links);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // DeleteLinkAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task DeleteLinkAsync_OnSuccess_RemovesLinkFromList()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        var link = MakeLink();
        c.WorldApi.GetWorldLinksAsync(worldId).Returns(new List<WorldLinkDto> { link });
        await c.Vm.LoadAsync(worldId);
        c.WorldApi.DeleteWorldLinkAsync(worldId, link.Id).Returns(true);

        await c.Vm.DeleteLinkAsync(link);

        Assert.Empty(c.Vm.Links);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteLinkAsync_WhenApiFails_ShowsError()
    {
        var c = CreateSut();
        var link = MakeLink();
        c.WorldApi.GetWorldLinksAsync(Arg.Any<Guid>()).Returns(new List<WorldLinkDto> { link });
        await c.Vm.LoadAsync(Guid.NewGuid());
        c.WorldApi.DeleteWorldLinkAsync(Arg.Any<Guid>(), link.Id).Returns(false);

        await c.Vm.DeleteLinkAsync(link);

        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // GetFaviconUrl
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetFaviconUrl_ValidUrl_ReturnsGoogleFaviconUrl()
    {
        var result = WorldLinksViewModel.GetFaviconUrl("https://roll20.net/path");
        Assert.Contains("roll20.net", result);
        Assert.StartsWith("https://www.google.com/s2/favicons", result);
    }

    [Fact]
    public void GetFaviconUrl_InvalidUrl_ReturnsEmpty()
    {
        var result = WorldLinksViewModel.GetFaviconUrl("not-a-url");
        Assert.Empty(result);
    }

    // ---------------------------------------------------------------------------
    // IsValidUrl (internal)
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("https://roll20.net", true)]
    [InlineData("http://example.com", true)]
    [InlineData("ftp://files.com", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void IsValidUrl_ReturnsExpectedResult(string url, bool expected)
    {
        Assert.Equal(expected, WorldLinksViewModel.IsValidUrl(url));
    }
}
