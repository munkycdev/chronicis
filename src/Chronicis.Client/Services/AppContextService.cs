using Blazored.LocalStorage;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for managing application context (current World/Campaign)
/// </summary>
public class AppContextService : IAppContextService
{
    private readonly IWorldApiService _worldApi;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AppContextService> _logger;

    private const string WorldIdKey = "chronicis_current_world_id";
    private const string CampaignIdKey = "chronicis_current_campaign_id";

    public Guid? CurrentWorldId { get; private set; }
    public Guid? CurrentCampaignId { get; private set; }
    public WorldDetailDto? CurrentWorld { get; private set; }
    public CampaignDto? CurrentCampaign { get; private set; }
    public List<WorldDto> Worlds { get; private set; } = new();
    public bool IsInitialized { get; private set; }

    public event Action? OnContextChanged;

    public AppContextService(
        IWorldApiService worldApi,
        ILocalStorageService localStorage,
        ILogger<AppContextService> logger)
    {
        _worldApi = worldApi;
        _localStorage = localStorage;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        _logger.LogDebug("Initializing app context");

        // Load all worlds
        Worlds = await _worldApi.GetWorldsAsync();

        if (Worlds.Count == 0)
        {
            _logger.LogWarning("No worlds found for user");
            IsInitialized = true;
            return;
        }

        // Try to restore saved selection
        Guid? savedWorldId = null;
        Guid? savedCampaignId = null;

        try
        {
            var savedWorldIdStr = await _localStorage.GetItemAsStringAsync(WorldIdKey);
            var savedCampaignIdStr = await _localStorage.GetItemAsStringAsync(CampaignIdKey);

            if (!string.IsNullOrEmpty(savedWorldIdStr) && Guid.TryParse(savedWorldIdStr.Trim('"'), out var parsedWorldId))
                savedWorldId = parsedWorldId;

            if (!string.IsNullOrEmpty(savedCampaignIdStr) && Guid.TryParse(savedCampaignIdStr.Trim('"'), out var parsedCampaignId))
                savedCampaignId = parsedCampaignId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read saved context from localStorage");
        }

        // Validate saved world exists
        if (savedWorldId.HasValue && Worlds.Any(w => w.Id == savedWorldId.Value))
        {
            await SelectWorldAsync(savedWorldId.Value, savedCampaignId);
        }
        else
        {
            // Default to first world
            await SelectWorldAsync(Worlds[0].Id);
        }

        IsInitialized = true;
        _logger.LogDebug("App context initialized. World: {WorldId}, Campaign: {CampaignId}", 
            CurrentWorldId, CurrentCampaignId);
    }

    public async Task SelectWorldAsync(Guid worldId, Guid? campaignId = null)
    {
        _logger.LogDebug("Selecting world {WorldId}", worldId);

        // Load world details
        var world = await _worldApi.GetWorldAsync(worldId);
        if (world == null)
        {
            _logger.LogWarning("Failed to load world {WorldId}", worldId);
            return;
        }

        CurrentWorldId = worldId;
        CurrentWorld = world;

        // Save to localStorage
        await _localStorage.SetItemAsStringAsync(WorldIdKey, worldId.ToString());

        // Select campaign
        if (campaignId.HasValue && world.Campaigns.Any(c => c.Id == campaignId.Value))
        {
            await SelectCampaignAsync(campaignId.Value);
        }
        else if (world.Campaigns.Count > 0)
        {
            // Default to first campaign if available
            await SelectCampaignAsync(world.Campaigns[0].Id);
        }
        else
        {
            await SelectCampaignAsync(null);
        }

        OnContextChanged?.Invoke();
    }

    public async Task SelectCampaignAsync(Guid? campaignId)
    {
        _logger.LogDebug("Selecting campaign {CampaignId}", campaignId);

        CurrentCampaignId = campaignId;

        if (campaignId.HasValue && CurrentWorld != null)
        {
            CurrentCampaign = CurrentWorld.Campaigns.FirstOrDefault(c => c.Id == campaignId.Value);
            await _localStorage.SetItemAsStringAsync(CampaignIdKey, campaignId.Value.ToString());
        }
        else
        {
            CurrentCampaign = null;
            await _localStorage.RemoveItemAsync(CampaignIdKey);
        }

        OnContextChanged?.Invoke();
    }

    public async Task RefreshCurrentWorldAsync()
    {
        if (!CurrentWorldId.HasValue)
            return;

        var world = await _worldApi.GetWorldAsync(CurrentWorldId.Value);
        if (world != null)
        {
            CurrentWorld = world;

            // Update campaign reference if still valid
            if (CurrentCampaignId.HasValue)
            {
                CurrentCampaign = world.Campaigns.FirstOrDefault(c => c.Id == CurrentCampaignId.Value);
            }

            OnContextChanged?.Invoke();
        }
    }

    public async Task RefreshWorldsAsync()
    {
        Worlds = await _worldApi.GetWorldsAsync();
        OnContextChanged?.Invoke();
    }
}
