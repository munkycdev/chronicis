using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class WorldSharingViewModelTests
{
    private sealed record Sut(
        WorldSharingViewModel Vm,
        IWorldApiService WorldApi,
        IAppNavigator Navigator,
        IUserNotifier Notifier);

    private static Sut CreateSut()
    {
        var worldApi = Substitute.For<IWorldApiService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var logger = Substitute.For<ILogger<WorldSharingViewModel>>();
        var vm = new WorldSharingViewModel(worldApi, navigator, notifier, logger);
        return new Sut(vm, worldApi, navigator, notifier);
    }

    // ---------------------------------------------------------------------------
    // InitializeFrom
    // ---------------------------------------------------------------------------

    [Fact]
    public void InitializeFrom_SetsPropertiesFromWorld()
    {
        var c = CreateSut();
        var world = new WorldDetailDto { IsPublic = true, Slug = "my-world" };
        c.Vm.InitializeFrom(world);

        Assert.True(c.Vm.IsPublic);
        Assert.Equal("my-world", c.Vm.PendingSlug);
        Assert.Null(c.Vm.SlugRenameError);
    }

    [Fact]
    public void InitializeFrom_WhenNotPublic_SetsSlugFromWorld()
    {
        var c = CreateSut();
        var world = new WorldDetailDto { IsPublic = false, Slug = "my-world" };
        c.Vm.InitializeFrom(world);

        Assert.False(c.Vm.IsPublic);
        Assert.Equal("my-world", c.Vm.PendingSlug);
    }

    // ---------------------------------------------------------------------------
    // OnPublicToggleChanged
    // ---------------------------------------------------------------------------

    [Fact]
    public void OnPublicToggleChanged_FiresUnsavedChangesEvent()
    {
        var c = CreateSut();
        var fired = false;
        c.Vm.UnsavedChangesOccurred += () => fired = true;

        c.Vm.OnPublicToggleChanged();

        Assert.True(fired);
    }

    [Fact]
    public void OnPublicToggleChanged_WhenNoSubscribers_DoesNotThrow()
    {
        var c = CreateSut();
        var ex = Record.Exception(() => c.Vm.OnPublicToggleChanged());
        Assert.Null(ex);
    }

    // ---------------------------------------------------------------------------
    // SaveSlugAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SaveSlugAsync_WhenSlugEmpty_SetsErrorAndReturnsNull()
    {
        var c = CreateSut();
        c.Vm.PendingSlug = string.Empty;

        var result = await c.Vm.SaveSlugAsync(Guid.NewGuid());

        Assert.Null(result);
        Assert.NotNull(c.Vm.SlugRenameError);
        Assert.False(c.Vm.IsRenamingSlug);
    }

    [Fact]
    public async Task SaveSlugAsync_OnSuccess_UpdatesPendingSlugAndReturnsNewSlug()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        c.Vm.PendingSlug = "my-world";
        c.WorldApi.UpdateSlugAsync(worldId, "my-world")
            .Returns(new SlugUpdateResponseDto { Slug = "my-world" });

        var result = await c.Vm.SaveSlugAsync(worldId);

        Assert.Equal("my-world", result);
        Assert.Equal("my-world", c.Vm.PendingSlug);
        Assert.False(c.Vm.IsRenamingSlug);
        Assert.Null(c.Vm.SlugRenameError);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveSlugAsync_WhenApiReturnsNull_SetsErrorAndReturnsNull()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        c.Vm.PendingSlug = "my-world";
        c.WorldApi.UpdateSlugAsync(worldId, "my-world").Returns((SlugUpdateResponseDto?)null);

        var result = await c.Vm.SaveSlugAsync(worldId);

        Assert.Null(result);
        Assert.NotNull(c.Vm.SlugRenameError);
        Assert.False(c.Vm.IsRenamingSlug);
    }

    [Fact]
    public async Task SaveSlugAsync_WhenApiThrows_SetsErrorAndReturnsNull()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        c.Vm.PendingSlug = "my-world";
        c.WorldApi.UpdateSlugAsync(worldId, Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("network error"));

        var result = await c.Vm.SaveSlugAsync(worldId);

        Assert.Null(result);
        Assert.NotNull(c.Vm.SlugRenameError);
        Assert.False(c.Vm.IsRenamingSlug);
        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // GetPublicUrlBase / GetFullPublicUrl / ShouldShowPublicPreview
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetPublicUrlBase_TrimsTrailingSlashAndAppendsWPath()
    {
        var c = CreateSut();
        var result = c.Vm.GetPublicUrlBase("https://chronicis.app/");
        Assert.Equal("https://chronicis.app/w/", result);
    }

    [Fact]
    public void GetFullPublicUrl_WhenWorldNullOrNoSlug_ReturnsEmpty()
    {
        var c = CreateSut();
        Assert.Empty(c.Vm.GetFullPublicUrl("https://app/", null));
        Assert.Empty(c.Vm.GetFullPublicUrl("https://app/", new WorldDetailDto { IsPublic = false }));
    }

    [Fact]
    public void GetFullPublicUrl_WhenSlugSet_ReturnsFullUrl()
    {
        var c = CreateSut();
        var world = new WorldDetailDto { IsPublic = true, Slug = "my-world" };
        var result = c.Vm.GetFullPublicUrl("https://chronicis.app/", world);
        Assert.Equal("https://chronicis.app/w/my-world", result);
    }

    [Fact]
    public void ShouldShowPublicPreview_WhenPublicWithSlug_ReturnsTrue()
    {
        var c = CreateSut();
        var world = new WorldDetailDto { IsPublic = true, Slug = "my-world" };
        Assert.True(c.Vm.ShouldShowPublicPreview(world));
    }

    [Theory]
    [InlineData(false, "slug")]
    [InlineData(true, "")]
    [InlineData(true, null)]
    public void ShouldShowPublicPreview_WhenNotPublicOrNoSlug_ReturnsFalse(bool isPublic, string? slug)
    {
        var c = CreateSut();
        var world = new WorldDetailDto { IsPublic = isPublic, Slug = slug ?? string.Empty };
        Assert.False(c.Vm.ShouldShowPublicPreview(world));
    }

    [Fact]
    public void ShouldShowPublicPreview_WhenWorldNull_ReturnsFalse()
    {
        var c = CreateSut();
        Assert.False(c.Vm.ShouldShowPublicPreview(null));
    }

    // ---------------------------------------------------------------------------
    // GenerateSlugFromName (static)
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("My World", "my-world")]
    [InlineData("The Sword Coast!", "the-sword-coast")]
    [InlineData("  Spaces  ", "spaces")]
    [InlineData("ab", "ab0")] // padded to 3 chars
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void GenerateSlugFromName_ReturnsExpectedSlug(string name, string expected)
    {
        Assert.Equal(expected, WorldSharingViewModel.GenerateSlugFromName(name));
    }
}
