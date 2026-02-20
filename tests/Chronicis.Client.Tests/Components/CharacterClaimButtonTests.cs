using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Characters;
using Chronicis.Client.Services; // For CharacterClaimStatusDto
using Microsoft.AspNetCore.Components; // For EventCallback
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the REFACTORED CharacterClaimButton component.
/// This component now accepts data as parameters - trivially easy to test!
///
/// Following the same pattern as BacklinksPanel, OutgoingLinksPanel, and ExternalLinksPanel.
/// </summary>
[ExcludeFromCodeCoverage]
public class CharacterClaimButtonTests : MudBlazorTestContext
{
    [Fact]
    public void CharacterClaimButton_ShowsLoadingIndicator_WhenLoading()
    {
        // Arrange & Act
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        var progress = cut.FindComponent<MudProgressCircular>();
        Assert.NotNull(progress);
    }

    [Fact]
    public void CharacterClaimButton_ShowsMyCharacter_WhenClaimedByMe()
    {
        // Arrange
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = true,
            IsClaimedByMe = true,
            ClaimedByName = "Test User"
        };

        // Act
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false));

        // Assert
        Assert.Contains("My Character", cut.Markup);
        var button = cut.FindComponent<MudButton>();
        Assert.Equal(Color.Success, button.Instance.Color);
    }

    [Fact]
    public void CharacterClaimButton_ShowsClaimedByOther_WhenClaimedByAnotherUser()
    {
        EnsurePopoverProvider();
        // Arrange
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = true,
            IsClaimedByMe = false,
            ClaimedByName = "Alice"
        };

        // Act
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false));

        // Assert
        Assert.Contains("Alice's Character", cut.Markup);
        var chip = cut.FindComponent<MudChip<string>>();
        Assert.NotNull(chip);
    }

    [Fact]
    public void CharacterClaimButton_ShowsClaimButton_WhenUnclaimed()
    {
        // Arrange
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = false,
            IsClaimedByMe = false,
            ClaimedByName = null
        };

        // Act
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false));

        // Assert
        Assert.Contains("Claim as My Character", cut.Markup);
        var button = cut.FindComponent<MudButton>();
        Assert.Equal(Color.Primary, button.Instance.Color);
    }

    [Fact]
    public async Task CharacterClaimButton_TriggersOnClaim_WhenClaimButtonClicked()
    {
        // Arrange
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = false,
            IsClaimedByMe = false
        };

        var claimInvoked = false;

        // Act
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false)
            .Add(p => p.OnClaim, EventCallback.Factory.Create(this, () => claimInvoked = true)));

        var button = cut.FindComponent<MudButton>();
        await cut.InvokeAsync(async () => await button.Instance.OnClick.InvokeAsync());

        // Assert
        Assert.True(claimInvoked, "OnClaim callback should have been invoked");
    }

    [Fact]
    public async Task CharacterClaimButton_TriggersOnUnclaim_WhenUnclaimButtonClicked()
    {
        // Arrange
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = true,
            IsClaimedByMe = true,
            ClaimedByName = "Test User"
        };

        var unclaimInvoked = false;

        // Act
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false)
            .Add(p => p.OnUnclaim, EventCallback.Factory.Create(this, () => unclaimInvoked = true)));

        var button = cut.FindComponent<MudButton>();
        await cut.InvokeAsync(async () => await button.Instance.OnClick.InvokeAsync());

        // Assert
        Assert.True(unclaimInvoked, "OnUnclaim callback should have been invoked");
    }

    [Fact]
    public void CharacterClaimButton_DisablesButton_WhenProcessing()
    {
        // Arrange
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = false,
            IsClaimedByMe = false
        };

        // Act
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false)
            .Add(p => p.IsProcessing, true));

        // Assert
        var button = cut.FindComponent<MudButton>();
        Assert.True(button.Instance.Disabled, "Button should be disabled when processing");
    }

    [Fact]
    public void CharacterClaimButton_ShowsProcessingIndicator_WhenProcessing()
    {
        // Arrange
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = false,
            IsClaimedByMe = false
        };

        // Act
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false)
            .Add(p => p.IsProcessing, true));

        // Assert
        var progressIndicators = cut.FindComponents<MudProgressCircular>();
        Assert.True(progressIndicators.Count > 0, "Should show processing indicator");
    }

    [Fact]
    public void CharacterClaimButton_ShowsTooltip_WhenClaimedByOther()
    {
        EnsurePopoverProvider();
        // Arrange
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = true,
            IsClaimedByMe = false,
            ClaimedByName = "Bob"
        };

        // Act
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false));

        // Assert
        var tooltip = cut.FindComponent<MudTooltip>();
        Assert.Equal("Claimed by Bob", tooltip.Instance.Text);
    }

    [Fact]
    public void CharacterClaimButton_WithNullClaimStatus_RendersNoAction()
    {
        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, (CharacterClaimStatusDto?)null)
            .Add(p => p.IsLoading, false));

        Assert.DoesNotContain("Claim as My Character", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("My Character", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void CharacterClaimButton_WhenClaimedByMeAndProcessing_ShowsSpinner()
    {
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = true,
            IsClaimedByMe = true,
            ClaimedByName = "Self"
        };

        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false)
            .Add(p => p.IsProcessing, true));

        Assert.NotEmpty(cut.FindAll(".mud-progress-circular"));
    }

    [Fact]
    public void CharacterClaimButton_WhenClaimedByOtherWithNoName_ShowsFallbackText()
    {
        EnsurePopoverProvider();
        var claimStatus = new CharacterClaimStatusDto
        {
            CharacterId = Guid.NewGuid(),
            IsClaimed = true,
            IsClaimedByMe = false,
            ClaimedByName = null
        };

        var cut = RenderComponent<CharacterClaimButton_REFACTORED>(parameters => parameters
            .Add(p => p.ClaimStatus, claimStatus)
            .Add(p => p.IsLoading, false));

        Assert.Contains("Character", cut.Markup, StringComparison.Ordinal);
    }

    private void EnsurePopoverProvider()
    {
        _ = RenderComponent<MudPopoverProvider>();
    }
}
