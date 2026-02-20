using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.World;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.World;

[ExcludeFromCodeCoverage]
public class WorldMembersPanelTests : MudBlazorTestContext
{
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();

    public WorldMembersPanelTests()
    {
        _worldApi.GetMembersAsync(Arg.Any<Guid>()).Returns(new List<WorldMemberDto>());
        _worldApi.GetInvitationsAsync(Arg.Any<Guid>()).Returns(new List<WorldInvitationDto>());

        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_dialogService);
        Services.AddSingleton(_snackbar);
    }

    [Theory]
    [InlineData(WorldRole.GM, Color.Warning)]
    [InlineData(WorldRole.Player, Color.Primary)]
    [InlineData(WorldRole.Observer, Color.Default)]
    [InlineData((WorldRole)999, Color.Default)]
    public void GetRoleColor_ReturnsExpectedColor(WorldRole role, Color expected)
    {
        var method = typeof(WorldMembersPanel)
            .GetMethod("GetRoleColor", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var result = (Color?)method!.Invoke(null, [role]);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void OnParametersSetAsync_WhenGm_LoadsMembersAndInvitations()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        _worldApi.GetMembersAsync(worldId).Returns(new List<WorldMemberDto> { CreateMember("GM") });
        _worldApi.GetInvitationsAsync(worldId).Returns(new List<WorldInvitationDto> { CreateInvitation() });

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        cut.WaitForAssertion(() =>
        {
            _worldApi.Received(1).GetMembersAsync(worldId);
            _worldApi.Received(1).GetInvitationsAsync(worldId);
        });
    }

    [Fact]
    public void OnParametersSetAsync_WhenNotGm_DoesNotLoadInvitations()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        _worldApi.GetMembersAsync(worldId).Returns(new List<WorldMemberDto> { CreateMember("Player") });

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, false));

        cut.WaitForAssertion(() =>
        {
            _worldApi.Received(1).GetMembersAsync(worldId);
            _worldApi.DidNotReceive().GetInvitationsAsync(worldId);
        });
    }

    [Fact]
    public async Task UpdateMemberRole_WhenSuccess_UpdatesRole()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var member = CreateMember("Player");
        var callbackInvoked = false;

        _worldApi.UpdateMemberRoleAsync(worldId, member.Id, Arg.Any<WorldMemberUpdateDto>())
            .Returns(new WorldMemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                DisplayName = member.DisplayName,
                Email = member.Email,
                Role = WorldRole.GM
            });

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true)
            .Add(x => x.OnMembersChanged, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        await InvokePrivateOnRendererAsync(cut, "UpdateMemberRole", member, WorldRole.GM);

        Assert.Equal(WorldRole.GM, member.Role);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task CreateInvitation_WhenSuccess_InsertsInvitation()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var invitation = CreateInvitation();
        _worldApi.CreateInvitationAsync(worldId, Arg.Any<WorldInvitationCreateDto>()).Returns(invitation);

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "CreateInvitation");

        var invitations = GetField<List<WorldInvitationDto>>(cut.Instance, "_invitations");
        Assert.Single(invitations);
        Assert.Equal(invitation.Code, invitations[0].Code);
    }

    [Fact]
    public async Task LoadDataAsync_WhenApiThrows_ShowsError()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        _worldApi.GetMembersAsync(worldId).Returns<Task<List<WorldMemberDto>>>(_ => throw new InvalidOperationException("boom"));

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "LoadDataAsync");

        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Failed to load members", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }

    [Fact]
    public async Task UpdateMemberRole_WhenApiReturnsNull_ShowsWarning()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var member = CreateMember("Player");
        _worldApi.UpdateMemberRoleAsync(worldId, member.Id, Arg.Any<WorldMemberUpdateDto>())
            .Returns((WorldMemberDto?)null);

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "UpdateMemberRole", member, WorldRole.Observer);

        _snackbar.Received().Add("Failed to update role. Cannot demote the last GM.", Severity.Warning);
        await _worldApi.Received().GetMembersAsync(worldId);
    }

    [Fact]
    public async Task UpdateMemberRole_WhenApiThrows_ShowsError()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var member = CreateMember("Player");
        _worldApi.UpdateMemberRoleAsync(worldId, member.Id, Arg.Any<WorldMemberUpdateDto>())
            .Returns<Task<WorldMemberDto?>>(_ => throw new InvalidOperationException("boom"));

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "UpdateMemberRole", member, WorldRole.Observer);

        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Failed to update role", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }

    [Fact]
    public async Task RemoveMember_WhenCanceled_DoesNotCallApi()
    {
        EnsurePopoverProvider();
        var member = CreateMember("ToRemove");
        _dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(false));

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, Guid.NewGuid())
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "RemoveMember", member);

        await _worldApi.DidNotReceive().RemoveMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task RemoveMember_WhenConfirmedAndSuccess_RemovesFromList()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var member = CreateMember("Removable");
        _dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        _worldApi.RemoveMemberAsync(worldId, member.Id).Returns(true);

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));
        SetField(cut.Instance, "_members", new List<WorldMemberDto> { member });

        await InvokePrivateOnRendererAsync(cut, "RemoveMember", member);

        var members = GetField<List<WorldMemberDto>>(cut.Instance, "_members");
        Assert.Empty(members);
    }

    [Fact]
    public async Task RemoveMember_WhenConfirmedAndApiReturnsFalse_ShowsWarning()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var member = CreateMember("Stays");
        _dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        _worldApi.RemoveMemberAsync(worldId, member.Id).Returns(false);

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "RemoveMember", member);

        _snackbar.Received().Add("Failed to remove member. Cannot remove the last GM.", Severity.Warning);
    }

    [Fact]
    public async Task RemoveMember_WhenApiThrows_ShowsError()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var member = CreateMember("Throws");
        _dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        _worldApi.RemoveMemberAsync(worldId, member.Id).Returns<Task<bool>>(_ => throw new InvalidOperationException("boom"));

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "RemoveMember", member);

        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Failed to remove member", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }

    [Fact]
    public async Task CreateInvitation_WhenApiReturnsNull_ShowsError()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        _worldApi.CreateInvitationAsync(worldId, Arg.Any<WorldInvitationCreateDto>())
            .Returns((WorldInvitationDto?)null);

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "CreateInvitation");

        _snackbar.Received().Add("Failed to create invitation", Severity.Error);
        Assert.False(GetField<bool>(cut.Instance, "_isCreatingInvitation"));
    }

    [Fact]
    public async Task CreateInvitation_WhenApiThrows_ShowsError()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        _worldApi.CreateInvitationAsync(worldId, Arg.Any<WorldInvitationCreateDto>())
            .Returns<Task<WorldInvitationDto?>>(_ => throw new InvalidOperationException("boom"));

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "CreateInvitation");

        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Failed to create invitation", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }

    [Fact]
    public async Task RevokeInvitation_WhenCanceled_DoesNotCallApi()
    {
        EnsurePopoverProvider();
        var invitation = CreateInvitation();
        _dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(false));

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, Guid.NewGuid())
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "RevokeInvitation", invitation);

        await _worldApi.DidNotReceive().RevokeInvitationAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task RevokeInvitation_WhenSuccess_MarksInactive()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var invitation = CreateInvitation();
        _dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        _worldApi.RevokeInvitationAsync(worldId, invitation.Id).Returns(true);

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "RevokeInvitation", invitation);

        Assert.False(invitation.IsActive);
        _snackbar.Received().Add("Invitation revoked", Severity.Success);
    }

    [Fact]
    public async Task RevokeInvitation_WhenApiReturnsFalse_ShowsError()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var invitation = CreateInvitation();
        _dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        _worldApi.RevokeInvitationAsync(worldId, invitation.Id).Returns(false);

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "RevokeInvitation", invitation);

        _snackbar.Received().Add("Failed to revoke invitation", Severity.Error);
    }

    [Fact]
    public async Task RevokeInvitation_WhenApiThrows_ShowsError()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var invitation = CreateInvitation();
        _dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        _worldApi.RevokeInvitationAsync(worldId, invitation.Id).Returns<Task<bool>>(_ => throw new InvalidOperationException("boom"));

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "RevokeInvitation", invitation);

        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Failed to revoke invitation", StringComparison.OrdinalIgnoreCase)), Severity.Error);
    }

    [Fact]
    public async Task CopyInvitationCode_WhenJsThrows_ShowsFailure()
    {
        EnsurePopoverProvider();
        JSInterop.Mode = JSRuntimeMode.Strict;

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, Guid.NewGuid())
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        await InvokePrivateOnRendererAsync(cut, "CopyInvitationCode", "CODE-1");

        _snackbar.Received().Add("Failed to copy code", Severity.Error);
    }

    [Fact]
    public void Render_WhenNoMembers_ShowsEmptyMembersMessage()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        _worldApi.GetMembersAsync(worldId).Returns(new List<WorldMemberDto>());

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        cut.WaitForAssertion(() =>
            Assert.Contains("No members yet. Create an invitation to add players.", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Render_WhenGmWithMembers_ShowsRoleSelectAndRemoveAction()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var otherMember = CreateMember("Other");
        otherMember.UserId = Guid.NewGuid();
        otherMember.AvatarUrl = "https://example.com/avatar.png";
        var selfMember = CreateMember("Self");
        selfMember.UserId = currentUserId;
        selfMember.AvatarUrl = null;

        _worldApi.GetMembersAsync(worldId).Returns(new List<WorldMemberDto> { otherMember, selfMember });

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, currentUserId)
            .Add(x => x.IsCurrentUserGM, true));

        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll(".mud-select"));
            Assert.Contains("Remove member", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("avatar.png", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Render_WhenNotGm_ShowsRoleChipAndHidesInvitationSection()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var member = CreateMember("PlayerOne");
        _worldApi.GetMembersAsync(worldId).Returns(new List<WorldMemberDto> { member });

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, false));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Player", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Create Invitation", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Render_WhenGmWithInvitations_ShowsActiveInvitationVariantsOnly()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        _worldApi.GetMembersAsync(worldId).Returns(new List<WorldMemberDto> { CreateMember("GM") });
        _worldApi.GetInvitationsAsync(worldId).Returns(new List<WorldInvitationDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = worldId,
                Code = "ACTIVE-LIMITED",
                Role = WorldRole.Player,
                IsActive = true,
                UsedCount = 1,
                MaxUses = 3,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = worldId,
                Code = "ACTIVE-OPEN",
                Role = WorldRole.Observer,
                IsActive = true,
                UsedCount = 0,
                MaxUses = null,
                ExpiresAt = null,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                WorldId = worldId,
                Code = "INACTIVE-HIDDEN",
                Role = WorldRole.Player,
                IsActive = false,
                UsedCount = 0,
                CreatedAt = DateTime.UtcNow
            }
        });

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("ACTIVE-LIMITED", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("ACTIVE-OPEN", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("INACTIVE-HIDDEN", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("1 / 3", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("0 / ∞", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Never", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task RenderedControls_WhenInvoked_ExecuteBoundHandlers()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var member = CreateMember("Handler");
        var invitation = CreateInvitation();

        _worldApi.GetMembersAsync(worldId).Returns(new List<WorldMemberDto> { member });
        _worldApi.GetInvitationsAsync(worldId).Returns(new List<WorldInvitationDto> { invitation });
        _worldApi.UpdateMemberRoleAsync(worldId, member.Id, Arg.Any<WorldMemberUpdateDto>())
            .Returns(new WorldMemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                DisplayName = member.DisplayName,
                Email = member.Email,
                Role = WorldRole.Observer
            });
        _dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(false));

        var cut = RenderComponent<WorldMembersPanel>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.CurrentUserId, Guid.NewGuid())
            .Add(x => x.IsCurrentUserGM, true));

        var roleSelect = cut.FindComponent<MudSelect<WorldRole>>();
        await cut.InvokeAsync(async () => await roleSelect.Instance.ValueChanged.InvokeAsync(WorldRole.Observer));

        var removeButton = cut.Find("button[title='Remove member']");
        var copyButton = cut.Find("button[title='Copy code']");
        var revokeButton = cut.Find("button[title='Revoke invitation']");
        await cut.InvokeAsync(() => removeButton.Click());
        await cut.InvokeAsync(() => copyButton.Click());
        await cut.InvokeAsync(() => revokeButton.Click());

        await _worldApi.Received(1).UpdateMemberRoleAsync(worldId, member.Id, Arg.Any<WorldMemberUpdateDto>());
        await _worldApi.DidNotReceive().RemoveMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
        await _worldApi.DidNotReceive().RevokeInvitationAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    private static WorldMemberDto CreateMember(string name)
    {
        return new WorldMemberDto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DisplayName = name,
            Email = $"{name}@example.com",
            Role = WorldRole.Player,
            JoinedAt = DateTime.UtcNow
        };
    }

    private static WorldInvitationDto CreateInvitation()
    {
        return new WorldInvitationDto
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            Code = "FROG-AXLE",
            Role = WorldRole.Player,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<WorldMembersPanel> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }

    private void EnsurePopoverProvider()
    {
        _ = RenderComponent<MudPopoverProvider>();
    }
}
