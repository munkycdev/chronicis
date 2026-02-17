using System.Diagnostics.CodeAnalysis;
using Blazored.LocalStorage;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class AppContextServiceTests
{
    private readonly IWorldApiService _worldApi;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AppContextService> _logger;
    private readonly AppContextService _sut;

    public AppContextServiceTests()
    {
        _worldApi = Substitute.For<IWorldApiService>();
        _localStorage = Substitute.For<ILocalStorageService>();
        _logger = Substitute.For<ILogger<AppContextService>>();
        _sut = new AppContextService(_worldApi, _localStorage, _logger);
    }

    [Fact]
    public async Task InitializeAsync_WhenNoWorlds_SetsInitializedWithoutError()
    {
        // Arrange
        _worldApi.GetWorldsAsync().Returns(new List<WorldDto>());

        // Act
        await _sut.InitializeAsync();

        // Assert
        Assert.True(_sut.IsInitialized);
        Assert.Empty(_sut.Worlds);
        Assert.Null(_sut.CurrentWorldId);
    }

    [Fact]
    public async Task InitializeAsync_WhenWorldsExist_LoadsFirstWorld()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var worlds = new List<WorldDto>
        {
            new WorldDto { Id = worldId, Name = "Test World" }
        };
        var worldDetail = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>()
        };

        _worldApi.GetWorldsAsync().Returns(worlds);
        _worldApi.GetWorldAsync(worldId).Returns(worldDetail);

        // Act
        await _sut.InitializeAsync();

        // Assert
        Assert.True(_sut.IsInitialized);
        Assert.Equal(worldId, _sut.CurrentWorldId);
        Assert.Equal(worldDetail, _sut.CurrentWorld);
    }

    [Fact]
    public async Task InitializeAsync_RestoresSavedWorldFromLocalStorage()
    {
        // Arrange
        var world1Id = Guid.NewGuid();
        var world2Id = Guid.NewGuid();
        var worlds = new List<WorldDto>
        {
            new WorldDto { Id = world1Id, Name = "World 1" },
            new WorldDto { Id = world2Id, Name = "World 2" }
        };
        var world2Detail = new WorldDetailDto
        {
            Id = world2Id,
            Name = "World 2",
            Campaigns = new List<CampaignDto>()
        };

        _worldApi.GetWorldsAsync().Returns(worlds);
        _worldApi.GetWorldAsync(world2Id).Returns(world2Detail);
        _localStorage.GetItemAsStringAsync("chronicis_current_world_id")
            .Returns($"\"{world2Id}\"");

        // Act
        await _sut.InitializeAsync();

        // Assert
        Assert.Equal(world2Id, _sut.CurrentWorldId);
    }

    [Fact]
    public async Task InitializeAsync_WhenSavedWorldDoesNotExist_SelectsFirstWorld()
    {
        // Arrange
        var world1Id = Guid.NewGuid();
        var nonExistentWorldId = Guid.NewGuid();
        var worlds = new List<WorldDto>
        {
            new WorldDto { Id = world1Id, Name = "World 1" }
        };
        var world1Detail = new WorldDetailDto
        {
            Id = world1Id,
            Name = "World 1",
            Campaigns = new List<CampaignDto>()
        };

        _worldApi.GetWorldsAsync().Returns(worlds);
        _worldApi.GetWorldAsync(world1Id).Returns(world1Detail);
        _localStorage.GetItemAsStringAsync("chronicis_current_world_id")
            .Returns($"\"{nonExistentWorldId}\"");

        // Act
        await _sut.InitializeAsync();

        // Assert
        Assert.Equal(world1Id, _sut.CurrentWorldId);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_OnlyInitializesOnce()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var worlds = new List<WorldDto>
        {
            new WorldDto { Id = worldId, Name = "Test World" }
        };
        var worldDetail = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>()
        };

        _worldApi.GetWorldsAsync().Returns(worlds);
        _worldApi.GetWorldAsync(worldId).Returns(worldDetail);

        // Act
        await _sut.InitializeAsync();
        await _sut.InitializeAsync();

        // Assert
        await _worldApi.Received(1).GetWorldsAsync();
    }

    [Fact]
    public async Task SelectWorldAsync_LoadsWorldDetailsAndSavesToLocalStorage()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var worldDetail = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>()
        };

        _worldApi.GetWorldAsync(worldId).Returns(worldDetail);

        // Act
        await _sut.SelectWorldAsync(worldId);

        // Assert
        Assert.Equal(worldId, _sut.CurrentWorldId);
        Assert.Equal(worldDetail, _sut.CurrentWorld);
        await _localStorage.Received(1).SetItemAsStringAsync("chronicis_current_world_id", worldId.ToString());
    }

    [Fact]
    public async Task SelectWorldAsync_WhenWorldHasCampaigns_SelectsFirstCampaign()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var worldDetail = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>
            {
                new CampaignDto { Id = campaignId, Name = "Campaign 1" }
            }
        };

        _worldApi.GetWorldAsync(worldId).Returns(worldDetail);

        // Act
        await _sut.SelectWorldAsync(worldId);

        // Assert
        Assert.Equal(campaignId, _sut.CurrentCampaignId);
        Assert.NotNull(_sut.CurrentCampaign);
    }

    [Fact]
    public async Task SelectWorldAsync_WithSpecificCampaignId_SelectsThatCampaign()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var campaign1Id = Guid.NewGuid();
        var campaign2Id = Guid.NewGuid();
        var worldDetail = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>
            {
                new CampaignDto { Id = campaign1Id, Name = "Campaign 1" },
                new CampaignDto { Id = campaign2Id, Name = "Campaign 2" }
            }
        };

        _worldApi.GetWorldAsync(worldId).Returns(worldDetail);

        // Act
        await _sut.SelectWorldAsync(worldId, campaign2Id);

        // Assert
        Assert.Equal(campaign2Id, _sut.CurrentCampaignId);
        Assert.Equal("Campaign 2", _sut.CurrentCampaign?.Name);
    }

    [Fact]
    public async Task SelectWorldAsync_WhenWorldLoadFails_DoesNotChangeState()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        _worldApi.GetWorldAsync(worldId).Returns((WorldDetailDto?)null);

        // Act
        await _sut.SelectWorldAsync(worldId);

        // Assert
        Assert.Null(_sut.CurrentWorldId);
        Assert.Null(_sut.CurrentWorld);
    }

    [Fact]
    public async Task SelectWorldAsync_RaisesOnContextChangedEvent()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var worldDetail = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>()
        };

        _worldApi.GetWorldAsync(worldId).Returns(worldDetail);

        var eventRaised = false;
        _sut.OnContextChanged += () => eventRaised = true;

        // Act
        await _sut.SelectWorldAsync(worldId);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task SelectCampaignAsync_WithValidCampaignId_UpdatesCurrentCampaign()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var worldDetail = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>
            {
                new CampaignDto { Id = campaignId, Name = "Campaign 1" }
            }
        };

        _worldApi.GetWorldAsync(worldId).Returns(worldDetail);
        await _sut.SelectWorldAsync(worldId);

        // Act
        await _sut.SelectCampaignAsync(campaignId);

        // Assert
        Assert.Equal(campaignId, _sut.CurrentCampaignId);
        Assert.NotNull(_sut.CurrentCampaign);
        await _localStorage.Received().SetItemAsStringAsync("chronicis_current_campaign_id", campaignId.ToString());
    }

    [Fact]
    public async Task SelectCampaignAsync_WithNull_ClearsCurrentCampaign()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var worldDetail = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>
            {
                new CampaignDto { Id = campaignId, Name = "Campaign 1" }
            }
        };

        _worldApi.GetWorldAsync(worldId).Returns(worldDetail);
        await _sut.SelectWorldAsync(worldId, campaignId);

        // Act
        await _sut.SelectCampaignAsync(null);

        // Assert
        Assert.Null(_sut.CurrentCampaignId);
        Assert.Null(_sut.CurrentCampaign);
        await _localStorage.Received().RemoveItemAsync("chronicis_current_campaign_id");
    }

    [Fact]
    public async Task SelectCampaignAsync_RaisesOnContextChangedEvent()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var worldDetail = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>
            {
                new CampaignDto { Id = campaignId, Name = "Campaign 1" }
            }
        };

        _worldApi.GetWorldAsync(worldId).Returns(worldDetail);
        await _sut.SelectWorldAsync(worldId);

        var eventRaised = false;
        _sut.OnContextChanged += () => eventRaised = true;

        // Act
        await _sut.SelectCampaignAsync(campaignId);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task RefreshCurrentWorldAsync_WhenWorldIdNotSet_DoesNothing()
    {
        // Act
        await _sut.RefreshCurrentWorldAsync();

        // Assert
        await _worldApi.DidNotReceive().GetWorldAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task RefreshCurrentWorldAsync_UpdatesWorldDetails()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var originalWorld = new WorldDetailDto
        {
            Id = worldId,
            Name = "Original Name",
            Campaigns = new List<CampaignDto>()
        };
        var updatedWorld = new WorldDetailDto
        {
            Id = worldId,
            Name = "Updated Name",
            Campaigns = new List<CampaignDto>()
        };

        _worldApi.GetWorldAsync(worldId).Returns(originalWorld, updatedWorld);
        await _sut.SelectWorldAsync(worldId);

        // Act
        await _sut.RefreshCurrentWorldAsync();

        // Assert
        Assert.Equal("Updated Name", _sut.CurrentWorld?.Name);
    }

    [Fact]
    public async Task RefreshCurrentWorldAsync_WhenCampaignNoLongerExists_ClearsCampaign()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var originalWorld = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>
            {
                new CampaignDto { Id = campaignId, Name = "Campaign 1" }
            }
        };
        var updatedWorld = new WorldDetailDto
        {
            Id = worldId,
            Name = "Test World",
            Campaigns = new List<CampaignDto>() // Campaign removed
        };

        _worldApi.GetWorldAsync(worldId).Returns(originalWorld, updatedWorld);
        await _sut.SelectWorldAsync(worldId, campaignId);

        // Act
        await _sut.RefreshCurrentWorldAsync();

        // Assert
        Assert.Null(_sut.CurrentCampaign);
    }

    [Fact]
    public async Task RefreshWorldsAsync_UpdatesWorldsList()
    {
        // Arrange
        var originalWorlds = new List<WorldDto>
        {
            new WorldDto { Id = Guid.NewGuid(), Name = "World 1" }
        };
        var updatedWorlds = new List<WorldDto>
        {
            new WorldDto { Id = Guid.NewGuid(), Name = "World 1" },
            new WorldDto { Id = Guid.NewGuid(), Name = "World 2" }
        };

        _worldApi.GetWorldsAsync().Returns(originalWorlds, updatedWorlds);
        await _sut.InitializeAsync();

        // Act
        await _sut.RefreshWorldsAsync();

        // Assert
        Assert.Equal(2, _sut.Worlds.Count);
    }

    [Fact]
    public async Task RefreshWorldsAsync_RaisesOnContextChangedEvent()
    {
        // Arrange
        _worldApi.GetWorldsAsync().Returns(new List<WorldDto>());

        var eventRaised = false;
        _sut.OnContextChanged += () => eventRaised = true;

        // Act
        await _sut.RefreshWorldsAsync();

        // Assert
        Assert.True(eventRaised);
    }
}
