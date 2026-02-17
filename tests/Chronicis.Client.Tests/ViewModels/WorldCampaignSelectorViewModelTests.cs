using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

/// <summary>
/// Tests for WorldCampaignSelectorViewModel.
/// This ViewModel wraps a facade, so we mock the facade for testing.
/// </summary>
public class WorldCampaignSelectorViewModelTests
{
    private readonly IWorldCampaignFacade _mockFacade;

    public WorldCampaignSelectorViewModelTests()
    {
        _mockFacade = Substitute.For<IWorldCampaignFacade>();
    }

    [Fact]
    public void ViewModel_InitializesWithFacadeState()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        _mockFacade.CurrentWorldId.Returns(worldId);
        _mockFacade.CurrentCampaignId.Returns(campaignId);

        // Act
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Assert
        Assert.Equal(worldId, viewModel.SelectedWorldId);
        Assert.Equal(campaignId, viewModel.SelectedCampaignId);
    }

    [Fact]
    public void ViewModel_IsInitialized_ReflectsFacade()
    {
        // Arrange
        _mockFacade.IsInitialized.Returns(true);

        // Act
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Assert
        Assert.True(viewModel.IsInitialized);
    }

    [Fact]
    public void ViewModel_Worlds_ReflectsFacade()
    {
        // Arrange
        var worlds = new List<WorldDto>
        {
            new() { Id = Guid.NewGuid(), Name = "World 1" },
            new() { Id = Guid.NewGuid(), Name = "World 2" }
        };
        _mockFacade.Worlds.Returns(worlds);

        // Act
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Assert
        Assert.Equal(2, viewModel.Worlds.Count);
        Assert.Equal("World 1", viewModel.Worlds[0].Name);
    }

    [Fact]
    public void ViewModel_HasCampaigns_ReturnsTrueWhenCampaignsExist()
    {
        // Arrange
        var world = new WorldDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Test World",
            Campaigns = new List<CampaignDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Campaign 1" }
            }
        };
        _mockFacade.CurrentWorld.Returns(world);

        // Act
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Assert
        Assert.True(viewModel.HasCampaigns);
    }

    [Fact]
    public void ViewModel_HasCampaigns_ReturnsFalseWhenNoCampaigns()
    {
        // Arrange
        var world = new WorldDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Test World",
            Campaigns = new List<CampaignDto>()
        };
        _mockFacade.CurrentWorld.Returns(world);

        // Act
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Assert
        Assert.False(viewModel.HasCampaigns);
    }

    [Fact]
    public async Task ViewModel_SelectWorldAsync_CallsFacade()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Act
        await viewModel.SelectWorldAsync(worldId);

        // Assert
        await _mockFacade.Received(1).SelectWorldAsync(worldId);
    }

    [Fact]
    public async Task ViewModel_SelectWorldAsync_DoesNotCallFacade_WhenEmptyGuid()
    {
        // Arrange
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Act
        await viewModel.SelectWorldAsync(Guid.Empty);

        // Assert
        await _mockFacade.DidNotReceive().SelectWorldAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ViewModel_SelectWorldAsync_DoesNotCallFacade_WhenSameAsCurrentWorld()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        _mockFacade.CurrentWorldId.Returns(worldId);
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Act
        await viewModel.SelectWorldAsync(worldId);

        // Assert
        await _mockFacade.DidNotReceive().SelectWorldAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ViewModel_SelectCampaignAsync_CallsFacade()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Act
        await viewModel.SelectCampaignAsync(campaignId);

        // Assert
        await _mockFacade.Received(1).SelectCampaignAsync(campaignId);
    }

    [Fact]
    public async Task ViewModel_CreateWorldAsync_CallsFacade()
    {
        // Arrange
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Act
        await viewModel.CreateWorldAsync();

        // Assert
        await _mockFacade.Received(1).CreateWorldAsync();
    }

    [Fact]
    public async Task ViewModel_CreateCampaignAsync_CallsFacade_WhenWorldSelected()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        _mockFacade.CurrentWorldId.Returns(worldId);
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Act
        await viewModel.CreateCampaignAsync();

        // Assert
        await _mockFacade.Received(1).CreateCampaignAsync(worldId);
    }

    [Fact]
    public async Task ViewModel_CreateCampaignAsync_DoesNotCallFacade_WhenNoWorldSelected()
    {
        // Arrange
        _mockFacade.CurrentWorldId.Returns((Guid?)null);
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);

        // Act
        await viewModel.CreateCampaignAsync();

        // Assert
        await _mockFacade.DidNotReceive().CreateCampaignAsync(Arg.Any<Guid>());
    }

    [Fact]
    public void ViewModel_RaisesStateChanged_WhenFacadeContextChanges()
    {
        // Arrange
        var stateChangedRaised = false;
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);
        viewModel.StateChanged += () => stateChangedRaised = true;

        // Act
        _mockFacade.OnContextChanged += Raise.Event<Action>();

        // Assert
        Assert.True(stateChangedRaised);
    }

    [Fact]
    public void ViewModel_UpdatesSelections_WhenFacadeContextChanges()
    {
        // Arrange
        var initialWorldId = Guid.NewGuid();
        var newWorldId = Guid.NewGuid();
        _mockFacade.CurrentWorldId.Returns(initialWorldId);
        var viewModel = new WorldCampaignSelectorViewModel(_mockFacade);
        Assert.Equal(initialWorldId, viewModel.SelectedWorldId);

        // Act
        _mockFacade.CurrentWorldId.Returns(newWorldId);
        _mockFacade.OnContextChanged += Raise.Event<Action>();

        // Assert
        Assert.Equal(newWorldId, viewModel.SelectedWorldId);
    }
}
