using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for managing application context (current World/Campaign)
/// </summary>
public interface IAppContextService
{
    /// <summary>
    /// Currently selected world ID
    /// </summary>
    Guid? CurrentWorldId { get; }

    /// <summary>
    /// Currently selected campaign ID (optional - user may be in Wiki/Characters without campaign)
    /// </summary>
    Guid? CurrentCampaignId { get; }

    /// <summary>
    /// Currently selected world details
    /// </summary>
    WorldDetailDto? CurrentWorld { get; }

    /// <summary>
    /// Currently selected campaign details
    /// </summary>
    CampaignDto? CurrentCampaign { get; }

    /// <summary>
    /// All worlds available to the user
    /// </summary>
    List<WorldDto> Worlds { get; }

    /// <summary>
    /// Whether the context has been initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Event fired when context changes
    /// </summary>
    event Action? OnContextChanged;

    /// <summary>
    /// Initialize the context (load worlds, restore saved selection)
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Select a world (and optionally a campaign)
    /// </summary>
    Task SelectWorldAsync(Guid worldId, Guid? campaignId = null);

    /// <summary>
    /// Select a campaign within the current world
    /// </summary>
    Task SelectCampaignAsync(Guid? campaignId);

    /// <summary>
    /// Refresh the current world's data (after creating a campaign, etc.)
    /// </summary>
    Task RefreshCurrentWorldAsync();

    /// <summary>
    /// Refresh the list of worlds (after creating a new world)
    /// </summary>
    Task RefreshWorldsAsync();
}
