using System.Security.Claims;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class WorldDetailViewModelTests
{
    private sealed record Sut(
        WorldDetailViewModel Vm,
        WorldLinksViewModel LinksVm,
        WorldDocumentsViewModel DocumentsVm,
        WorldSharingViewModel SharingVm,
        IWorldApiService WorldApi,
        ITreeStateService TreeState,
        IBreadcrumbService BreadcrumbService,
        IDialogService DialogService,
        IAppNavigator Navigator,
        IUserNotifier Notifier,
        IPageTitleService TitleService,
        AuthenticationStateProvider AuthProvider);

    private static Sut CreateSut(string? userEmail = "dm@example.com", WorldRole role = WorldRole.GM)
    {
        var worldApi = Substitute.For<IWorldApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var breadcrumb = Substitute.For<IBreadcrumbService>();
        var dialogService = Substitute.For<IDialogService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var titleService = Substitute.For<IPageTitleService>();
        var authProvider = Substitute.For<AuthenticationStateProvider>();
        var linksLogger = Substitute.For<ILogger<WorldLinksViewModel>>();
        var docsLogger = Substitute.For<ILogger<WorldDocumentsViewModel>>();
        var sharingLogger = Substitute.For<ILogger<WorldSharingViewModel>>();
        var vmLogger = Substitute.For<ILogger<WorldDetailViewModel>>();

        // Set up auth state with email claim
        var claims = userEmail != null
            ? new[] { new Claim("email", userEmail) }
            : Array.Empty<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        authProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        var linksVm = new WorldLinksViewModel(worldApi, treeState, notifier, linksLogger);
        var docsVm = new WorldDocumentsViewModel(worldApi, treeState, notifier, dialogService, navigator, docsLogger);
        var sharingVm = new WorldSharingViewModel(worldApi, navigator, notifier, sharingLogger);

        var vm = new WorldDetailViewModel(
            worldApi, treeState, breadcrumb, dialogService,
            navigator, notifier, titleService, authProvider, vmLogger);

        return new Sut(vm, linksVm, docsVm, sharingVm,
            worldApi, treeState, breadcrumb, dialogService,
            navigator, notifier, titleService, authProvider);
    }

    private static WorldDetailDto MakeWorld(string name = "Faerûn", string? email = "dm@example.com") =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "A world of adventure",
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            Members = email != null
                ? new List<WorldMemberDto> { new() { UserId = Guid.NewGuid(), Email = email, Role = WorldRole.GM } }
                : new List<WorldMemberDto>()
        };

    // ---------------------------------------------------------------------------
    // LoadAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_WhenWorldNotFound_NavigatesToDashboard()
    {
        var c = CreateSut();
        c.WorldApi.GetWorldAsync(Arg.Any<Guid>()).Returns((WorldDetailDto?)null);
        c.WorldApi.GetWorldLinksAsync(Arg.Any<Guid>()).Returns(new List<WorldLinkDto>());
        c.WorldApi.GetWorldDocumentsAsync(Arg.Any<Guid>()).Returns(new List<WorldDocumentDto>());

        await c.Vm.LoadAsync(Guid.NewGuid(), c.SharingVm, c.LinksVm, c.DocumentsVm);

        c.Navigator.Received(1).NavigateTo("/dashboard", replace: true);
    }

    [Fact]
    public async Task LoadAsync_WhenWorldFound_SetsProperties()
    {
        var c = CreateSut();
        var world = MakeWorld();
        c.WorldApi.GetWorldAsync(world.Id).Returns(world);
        c.WorldApi.GetWorldLinksAsync(world.Id).Returns(new List<WorldLinkDto>());
        c.WorldApi.GetWorldDocumentsAsync(world.Id).Returns(new List<WorldDocumentDto>());

        await c.Vm.LoadAsync(world.Id, c.SharingVm, c.LinksVm, c.DocumentsVm);

        Assert.False(c.Vm.IsLoading);
        Assert.Equal(world, c.Vm.World);
        Assert.Equal("Faerûn", c.Vm.EditName);
        Assert.False(c.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task LoadAsync_WhenGmEmail_SetsIsCurrentUserGmTrue()
    {
        var c = CreateSut(userEmail: "dm@example.com", role: WorldRole.GM);
        var world = MakeWorld(email: "dm@example.com");
        c.WorldApi.GetWorldAsync(world.Id).Returns(world);
        c.WorldApi.GetWorldLinksAsync(world.Id).Returns(new List<WorldLinkDto>());
        c.WorldApi.GetWorldDocumentsAsync(world.Id).Returns(new List<WorldDocumentDto>());

        await c.Vm.LoadAsync(world.Id, c.SharingVm, c.LinksVm, c.DocumentsVm);

        Assert.True(c.Vm.IsCurrentUserGm);
    }

    [Fact]
    public async Task LoadAsync_WhenApiThrows_ShowsErrorAndClearsLoading()
    {
        var c = CreateSut();
        c.WorldApi.GetWorldAsync(Arg.Any<Guid>()).ThrowsAsync(new Exception("fail"));

        await c.Vm.LoadAsync(Guid.NewGuid(), c.SharingVm, c.LinksVm, c.DocumentsVm);

        Assert.False(c.Vm.IsLoading);
        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    [Fact]
    public async Task LoadAsync_WhenNoEmailClaim_DoesNotSetGmRole()
    {
        var c = CreateSut(userEmail: null);
        var world = MakeWorld(email: "dm@example.com");
        c.WorldApi.GetWorldAsync(world.Id).Returns(world);
        c.WorldApi.GetWorldLinksAsync(world.Id).Returns(new List<WorldLinkDto>());
        c.WorldApi.GetWorldDocumentsAsync(world.Id).Returns(new List<WorldDocumentDto>());

        await c.Vm.LoadAsync(world.Id, c.SharingVm, c.LinksVm, c.DocumentsVm);

        Assert.False(c.Vm.IsCurrentUserGm);
    }

    // ---------------------------------------------------------------------------
    // SaveAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SaveAsync_WhenWorldNull_DoesNothing()
    {
        var c = CreateSut();
        await c.Vm.SaveAsync(c.SharingVm);
        await c.WorldApi.DidNotReceive().UpdateWorldAsync(Arg.Any<Guid>(), Arg.Any<WorldUpdateDto>());
    }

    [Fact]
    public async Task SaveAsync_WhenPublicButSlugUnavailable_WarnsAndDoesNotSave()
    {
        var c = CreateSut();
        var world = MakeWorld();
        c.WorldApi.GetWorldAsync(world.Id).Returns(world);
        c.WorldApi.GetWorldLinksAsync(world.Id).Returns(new List<WorldLinkDto>());
        c.WorldApi.GetWorldDocumentsAsync(world.Id).Returns(new List<WorldDocumentDto>());
        await c.Vm.LoadAsync(world.Id, c.SharingVm, c.LinksVm, c.DocumentsVm);

        c.SharingVm.IsPublic = true;
        // SlugIsAvailable is false by default

        await c.Vm.SaveAsync(c.SharingVm);

        c.Notifier.Received(1).Warning(Arg.Any<string>());
        await c.WorldApi.DidNotReceive().UpdateWorldAsync(Arg.Any<Guid>(), Arg.Any<WorldUpdateDto>());
    }

    [Fact]
    public async Task SaveAsync_OnSuccess_ClearsUnsavedChanges()
    {
        var c = CreateSut();
        var world = MakeWorld();
        c.WorldApi.GetWorldAsync(world.Id).Returns(world);
        c.WorldApi.GetWorldLinksAsync(world.Id).Returns(new List<WorldLinkDto>());
        c.WorldApi.GetWorldDocumentsAsync(world.Id).Returns(new List<WorldDocumentDto>());
        await c.Vm.LoadAsync(world.Id, c.SharingVm, c.LinksVm, c.DocumentsVm);

        c.Vm.EditName = "Updated World";
        var updated = new WorldDto { Id = world.Id, Name = "Updated World" };
        c.WorldApi.UpdateWorldAsync(world.Id, Arg.Any<WorldUpdateDto>()).Returns(updated);

        await c.Vm.SaveAsync(c.SharingVm);

        Assert.False(c.Vm.HasUnsavedChanges);
        Assert.False(c.Vm.IsSaving);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveAsync_WhenApiThrows_ShowsErrorAndClearsSaving()
    {
        var c = CreateSut();
        var world = MakeWorld();
        c.WorldApi.GetWorldAsync(world.Id).Returns(world);
        c.WorldApi.GetWorldLinksAsync(world.Id).Returns(new List<WorldLinkDto>());
        c.WorldApi.GetWorldDocumentsAsync(world.Id).Returns(new List<WorldDocumentDto>());
        await c.Vm.LoadAsync(world.Id, c.SharingVm, c.LinksVm, c.DocumentsVm);
        c.WorldApi.UpdateWorldAsync(Arg.Any<Guid>(), Arg.Any<WorldUpdateDto>()).ThrowsAsync(new Exception("db error"));

        await c.Vm.SaveAsync(c.SharingVm);

        Assert.False(c.Vm.IsSaving);
        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // HasUnsavedChanges auto-flagging
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task EditName_WhenChanged_SetsHasUnsavedChanges()
    {
        var c = CreateSut();
        var world = MakeWorld();
        c.WorldApi.GetWorldAsync(world.Id).Returns(world);
        c.WorldApi.GetWorldLinksAsync(world.Id).Returns(new List<WorldLinkDto>());
        c.WorldApi.GetWorldDocumentsAsync(world.Id).Returns(new List<WorldDocumentDto>());
        await c.Vm.LoadAsync(world.Id, c.SharingVm, c.LinksVm, c.DocumentsVm);

        c.Vm.EditName = "Changed";

        Assert.True(c.Vm.HasUnsavedChanges);
    }

    // ---------------------------------------------------------------------------
    // OnMembersChangedAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task OnMembersChangedAsync_WhenWorldLoaded_UpdatesMemberCountFromApi()
    {
        var c = CreateSut();
        var world = MakeWorld();
        world.MemberCount = 1;
        c.WorldApi.GetWorldAsync(world.Id).Returns(world);
        c.WorldApi.GetWorldLinksAsync(world.Id).Returns(new List<WorldLinkDto>());
        c.WorldApi.GetWorldDocumentsAsync(world.Id).Returns(new List<WorldDocumentDto>());
        await c.Vm.LoadAsync(world.Id, c.SharingVm, c.LinksVm, c.DocumentsVm);

        var refreshed = MakeWorld();
        refreshed.Id = world.Id;
        refreshed.MemberCount = 3;
        c.WorldApi.GetWorldAsync(world.Id).Returns(refreshed);

        await c.Vm.OnMembersChangedAsync();

        Assert.Equal(3, c.Vm.World!.MemberCount);
    }

    [Fact]
    public async Task OnMembersChangedAsync_WhenWorldNull_DoesNothing()
    {
        var c = CreateSut();
        await c.Vm.OnMembersChangedAsync(); // World is null, no exception
        await c.WorldApi.DidNotReceive().GetWorldAsync(Arg.Any<Guid>());
    }
}
