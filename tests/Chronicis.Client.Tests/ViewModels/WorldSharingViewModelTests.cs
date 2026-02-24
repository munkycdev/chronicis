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
        var world = new WorldDetailDto { IsPublic = true, PublicSlug = "my-world" };
        c.Vm.InitializeFrom(world);

        Assert.True(c.Vm.IsPublic);
        Assert.Equal("my-world", c.Vm.PublicSlug);
        Assert.True(c.Vm.SlugIsAvailable); // IsPublic=true means slug was already valid
        Assert.Null(c.Vm.SlugError);
    }

    [Fact]
    public void InitializeFrom_WhenNotPublic_SlugIsAvailableFalse()
    {
        var c = CreateSut();
        var world = new WorldDetailDto { IsPublic = false };
        c.Vm.InitializeFrom(world);

        Assert.False(c.Vm.IsPublic);
        Assert.False(c.Vm.SlugIsAvailable);
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

        c.Vm.OnPublicToggleChanged(null);

        Assert.True(fired);
    }

    [Fact]
    public void OnPublicToggleChanged_WhenPublicAndSlugEmpty_GeneratesSlug()
    {
        var c = CreateSut();
        c.Vm.IsPublic = true;
        c.Vm.PublicSlug = string.Empty;
        c.WorldApi.CheckPublicSlugAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(new PublicSlugCheckResultDto { IsAvailable = true });

        var world = new WorldDetailDto { Id = Guid.NewGuid(), Name = "My Awesome World" };
        c.Vm.OnPublicToggleChanged(world);

        Assert.NotEmpty(c.Vm.PublicSlug);
        Assert.Contains("my", c.Vm.PublicSlug);
    }

    [Fact]
    public void OnPublicToggleChanged_WhenSlugAlreadySet_DoesNotOverwrite()
    {
        var c = CreateSut();
        c.Vm.IsPublic = true;
        c.Vm.PublicSlug = "existing-slug";
        var world = new WorldDetailDto { Id = Guid.NewGuid(), Name = "My World" };

        c.Vm.OnPublicToggleChanged(world);

        Assert.Equal("existing-slug", c.Vm.PublicSlug);
    }

    [Fact]
    public void OnPublicToggleChanged_WhenPublicAndSlugEmpty_AndWorldNull_UsesEmptyFallbacks()
    {
        var c = CreateSut();
        c.Vm.IsPublic = true;
        c.Vm.PublicSlug = string.Empty;

        c.Vm.OnPublicToggleChanged(null);

        Assert.Equal(string.Empty, c.Vm.PublicSlug);
    }

    [Fact]
    public void OnPublicToggleChanged_WhenWorldNameNull_GeneratesFromEmptyName()
    {
        var c = CreateSut();
        c.Vm.IsPublic = true;
        c.Vm.PublicSlug = string.Empty;
        c.WorldApi.CheckPublicSlugAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(new PublicSlugCheckResultDto { IsAvailable = true });

        var world = new WorldDetailDto { Id = Guid.NewGuid(), Name = null! };

        c.Vm.OnPublicToggleChanged(world);

        Assert.Equal(string.Empty, c.Vm.PublicSlug);
    }

    // ---------------------------------------------------------------------------
    // CheckSlugAvailabilityAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task CheckSlugAvailabilityAsync_WhenSlugEmpty_ResetsState()
    {
        var c = CreateSut();
        c.Vm.PublicSlug = string.Empty;

        await c.Vm.CheckSlugAvailabilityAsync(Guid.NewGuid());

        Assert.False(c.Vm.SlugIsAvailable);
        Assert.Null(c.Vm.SlugError);
        Assert.Null(c.Vm.SlugHelperText);
    }

    [Fact]
    public async Task CheckSlugAvailabilityAsync_WhenApiReturnsNull_SetsError()
    {
        var c = CreateSut();
        c.Vm.PublicSlug = "my-world";
        c.WorldApi.CheckPublicSlugAsync(Arg.Any<Guid>(), "my-world").Returns((PublicSlugCheckResultDto?)null);

        await c.Vm.CheckSlugAvailabilityAsync(Guid.NewGuid());

        Assert.False(c.Vm.SlugIsAvailable);
        Assert.NotNull(c.Vm.SlugError);
    }

    [Fact]
    public async Task CheckSlugAvailabilityAsync_WhenValidationError_SetsErrorWithSuggestion()
    {
        var c = CreateSut();
        c.Vm.PublicSlug = "bad slug!";
        c.WorldApi.CheckPublicSlugAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(
            new PublicSlugCheckResultDto { ValidationError = "Invalid characters", SuggestedSlug = "bad-slug" });

        await c.Vm.CheckSlugAvailabilityAsync(Guid.NewGuid());

        Assert.False(c.Vm.SlugIsAvailable);
        Assert.Equal("Invalid characters", c.Vm.SlugError);
        Assert.Contains("bad-slug", c.Vm.SlugHelperText);
    }

    [Fact]
    public async Task CheckSlugAvailabilityAsync_WhenSlugTaken_SetsErrorWithSuggestion()
    {
        var c = CreateSut();
        c.Vm.PublicSlug = "taken-slug";
        c.WorldApi.CheckPublicSlugAsync(Arg.Any<Guid>(), "taken-slug").Returns(
            new PublicSlugCheckResultDto { IsAvailable = false, SuggestedSlug = "taken-slug-2" });

        await c.Vm.CheckSlugAvailabilityAsync(Guid.NewGuid());

        Assert.False(c.Vm.SlugIsAvailable);
        Assert.NotNull(c.Vm.SlugError);
        Assert.Contains("taken-slug-2", c.Vm.SlugHelperText);
    }

    [Fact]
    public async Task CheckSlugAvailabilityAsync_WhenAvailable_SetsSlugIsAvailableAndHelperText()
    {
        var c = CreateSut();
        c.Vm.PublicSlug = "my-world";
        c.WorldApi.CheckPublicSlugAsync(Arg.Any<Guid>(), "my-world").Returns(
            new PublicSlugCheckResultDto { IsAvailable = true });

        await c.Vm.CheckSlugAvailabilityAsync(Guid.NewGuid());

        Assert.True(c.Vm.SlugIsAvailable);
        Assert.Null(c.Vm.SlugError);
        Assert.Equal("Available!", c.Vm.SlugHelperText);
    }

    [Fact]
    public async Task CheckSlugAvailabilityAsync_WhenApiThrows_SetsError()
    {
        var c = CreateSut();
        c.Vm.PublicSlug = "my-world";
        c.WorldApi.CheckPublicSlugAsync(Arg.Any<Guid>(), Arg.Any<string>()).ThrowsAsync(new Exception("network"));

        await c.Vm.CheckSlugAvailabilityAsync(Guid.NewGuid());

        Assert.False(c.Vm.IsCheckingSlug);
        Assert.False(c.Vm.SlugIsAvailable);
        Assert.NotNull(c.Vm.SlugError);
    }

    [Fact]
    public async Task CheckSlugAvailabilityAsync_FiresUnsavedChangesOnSuccess()
    {
        var c = CreateSut();
        c.Vm.PublicSlug = "my-world";
        c.WorldApi.CheckPublicSlugAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(
            new PublicSlugCheckResultDto { IsAvailable = true });
        var fired = false;
        c.Vm.UnsavedChangesOccurred += () => fired = true;

        await c.Vm.CheckSlugAvailabilityAsync(Guid.NewGuid());

        Assert.True(fired);
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
        Assert.Empty(c.Vm.GetFullPublicUrl("https://app/", new WorldDetailDto { PublicSlug = null }));
    }

    [Fact]
    public void GetFullPublicUrl_WhenSlugSet_ReturnsFullUrl()
    {
        var c = CreateSut();
        var world = new WorldDetailDto { IsPublic = true, PublicSlug = "my-world" };
        var result = c.Vm.GetFullPublicUrl("https://chronicis.app/", world);
        Assert.Equal("https://chronicis.app/w/my-world", result);
    }

    [Fact]
    public void ShouldShowPublicPreview_WhenPublicWithSlug_ReturnsTrue()
    {
        var c = CreateSut();
        var world = new WorldDetailDto { IsPublic = true, PublicSlug = "my-world" };
        Assert.True(c.Vm.ShouldShowPublicPreview(world));
    }

    [Theory]
    [InlineData(false, "slug")]
    [InlineData(true, "")]
    [InlineData(true, null)]
    public void ShouldShowPublicPreview_WhenNotPublicOrNoSlug_ReturnsFalse(bool isPublic, string? slug)
    {
        var c = CreateSut();
        var world = new WorldDetailDto { IsPublic = isPublic, PublicSlug = slug };
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
