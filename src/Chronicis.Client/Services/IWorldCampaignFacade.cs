using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Facade service for world and campaign selection operations.
/// Simplifies component dependencies by wrapping AppContext, Dialog, and Snackbar services.
/// </summary>
public interface IWorldCampaignFacade
{
    /// <summary>
    /// Whether the context has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Available worlds.
    /// </summary>
    IReadOnlyList<WorldDto> Worlds { get; }

    /// <summary>
    /// Current world ID (if any).
    /// </summary>
    Guid? CurrentWorldId { get; }

    /// <summary>
    /// Current campaign ID (if any).
    /// </summary>
    Guid? CurrentCampaignId { get; }

    /// <summary>
    /// Current world (if any).
    /// </summary>
    WorldDetailDto? CurrentWorld { get; }

    /// <summary>
    /// Event raised when context changes (world/campaign selection).
    /// </summary>
    event Action? OnContextChanged;

    /// <summary>
    /// Select a world.
    /// </summary>
    Task SelectWorldAsync(Guid worldId);

    /// <summary>
    /// Select a campaign (or null for world-only view).
    /// </summary>
    Task SelectCampaignAsync(Guid? campaignId);

    /// <summary>
    /// Show create world dialog and handle result.
    /// Returns the newly created world if successful, null otherwise.
    /// </summary>
    Task<WorldDto?> CreateWorldAsync();

    /// <summary>
    /// Show create campaign dialog and handle result.
    /// Returns the newly created campaign if successful, null otherwise.
    /// </summary>
    Task<CampaignDto?> CreateCampaignAsync(Guid worldId);
}
