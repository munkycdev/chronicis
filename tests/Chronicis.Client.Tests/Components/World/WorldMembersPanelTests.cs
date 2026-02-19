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
