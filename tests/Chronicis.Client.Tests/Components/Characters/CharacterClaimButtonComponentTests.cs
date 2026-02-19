using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Characters;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Characters;

[ExcludeFromCodeCoverage]
public class CharacterClaimButtonComponentTests : MudBlazorTestContext
{
    private readonly ICharacterApiService _characterApi = Substitute.For<ICharacterApiService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly ILogger<CharacterClaimButton> _logger = NullLogger<CharacterClaimButton>.Instance;

    public CharacterClaimButtonComponentTests()
    {
        Services.AddSingleton(_characterApi);
        Services.AddSingleton(_snackbar);
        Services.AddSingleton(_logger);
    }

    [Fact]
    public void OnParametersSetAsync_LoadsClaimStatus()
    {
        var characterId = Guid.NewGuid();
        _characterApi.GetClaimStatusAsync(characterId).Returns(new CharacterClaimStatusDto
        {
            CharacterId = characterId,
            IsClaimed = false,
            IsClaimedByMe = false
        });

        var cut = RenderComponent<CharacterClaimButton>(p => p.Add(x => x.CharacterId, characterId));

        cut.WaitForAssertion(() =>
        {
            _characterApi.Received(1).GetClaimStatusAsync(characterId);
            Assert.False(GetField<bool>(cut.Instance, "_isLoading"));
        });
    }

    [Fact]
    public async Task ClaimCharacter_WhenSuccess_InvokesCallbackAndReloads()
    {
        var characterId = Guid.NewGuid();
        var callbackInvoked = false;
        _characterApi.GetClaimStatusAsync(characterId).Returns(new CharacterClaimStatusDto
        {
            CharacterId = characterId,
            IsClaimed = false,
            IsClaimedByMe = false
        });
        _characterApi.ClaimCharacterAsync(characterId).Returns(true);

        var cut = RenderComponent<CharacterClaimButton>(p => p
            .Add(x => x.CharacterId, characterId)
            .Add(x => x.OnClaimChanged, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        await InvokePrivateOnRendererAsync(cut, "ClaimCharacter");

        await _characterApi.Received(1).ClaimCharacterAsync(characterId);
        await _characterApi.Received(2).GetClaimStatusAsync(characterId);
        Assert.True(callbackInvoked);
        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("claimed", StringComparison.OrdinalIgnoreCase)), Severity.Success);
        Assert.False(GetField<bool>(cut.Instance, "_isProcessing"));
    }

    [Fact]
    public async Task ClaimCharacter_WhenFailure_ShowsError()
    {
        var characterId = Guid.NewGuid();
        _characterApi.GetClaimStatusAsync(characterId).Returns(new CharacterClaimStatusDto
        {
            CharacterId = characterId,
            IsClaimed = false,
            IsClaimedByMe = false
        });
        _characterApi.ClaimCharacterAsync(characterId).Returns(false);

        var cut = RenderComponent<CharacterClaimButton>(p => p.Add(x => x.CharacterId, characterId));

        await InvokePrivateOnRendererAsync(cut, "ClaimCharacter");

        _snackbar.Received().Add("Failed to claim character", Severity.Error);
    }

    [Fact]
    public async Task UnclaimCharacter_WhenSuccess_ShowsInfoAndReloads()
    {
        var characterId = Guid.NewGuid();
        _characterApi.GetClaimStatusAsync(characterId).Returns(new CharacterClaimStatusDto
        {
            CharacterId = characterId,
            IsClaimed = true,
            IsClaimedByMe = true,
            ClaimedByName = "Test User"
        });
        _characterApi.UnclaimCharacterAsync(characterId).Returns(true);

        var cut = RenderComponent<CharacterClaimButton>(p => p.Add(x => x.CharacterId, characterId));

        await InvokePrivateOnRendererAsync(cut, "UnclaimCharacter");

        await _characterApi.Received(1).UnclaimCharacterAsync(characterId);
        _snackbar.Received().Add("Character unclaimed", Severity.Info);
    }

    [Fact]
    public async Task UnclaimCharacter_WhenFailure_ShowsError()
    {
        var characterId = Guid.NewGuid();
        _characterApi.GetClaimStatusAsync(characterId).Returns(new CharacterClaimStatusDto
        {
            CharacterId = characterId,
            IsClaimed = true,
            IsClaimedByMe = true
        });
        _characterApi.UnclaimCharacterAsync(characterId).Returns(false);

        var cut = RenderComponent<CharacterClaimButton>(p => p.Add(x => x.CharacterId, characterId));

        await InvokePrivateOnRendererAsync(cut, "UnclaimCharacter");

        _snackbar.Received().Add("Failed to unclaim character", Severity.Error);
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<CharacterClaimButton> cut, string methodName, params object[] args)
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
}
