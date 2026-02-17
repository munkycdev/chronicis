using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for WorldCampaignSelector component.
/// Encapsulates selection state and provides simple properties for UI binding.
/// </summary>
public class WorldCampaignSelectorViewModel : IDisposable
{
    private readonly IWorldCampaignFacade _facade;

    public WorldCampaignSelectorViewModel(IWorldCampaignFacade facade)
    {
        _facade = facade;
        _facade.OnContextChanged += OnContextChanged;
        UpdateSelections();
    }

    /// <summary>
    /// Currently selected world ID.
    /// </summary>
    public Guid SelectedWorldId { get; private set; }

    /// <summary>
    /// Currently selected campaign ID (null means world-only view).
    /// </summary>
    public Guid? SelectedCampaignId { get; private set; }

    /// <summary>
    /// Whether the facade is initialized.
    /// </summary>
    public bool IsInitialized => _facade.IsInitialized;

    /// <summary>
    /// Available worlds.
    /// </summary>
    public IReadOnlyList<WorldDto> Worlds => _facade.Worlds;

    /// <summary>
    /// Current world.
    /// </summary>
    public WorldDetailDto? CurrentWorld => _facade.CurrentWorld;

    /// <summary>
    /// Whether there are any campaigns in the current world.
    /// </summary>
    public bool HasCampaigns => CurrentWorld?.Campaigns.Count > 0;

    /// <summary>
    /// Event raised when state changes (for UI re-render).
    /// </summary>
    public event Action? StateChanged;

    private void OnContextChanged()
    {
        UpdateSelections();
        StateChanged?.Invoke();
    }

    private void UpdateSelections()
    {
        SelectedWorldId = _facade.CurrentWorldId ?? Guid.Empty;
        SelectedCampaignId = _facade.CurrentCampaignId;
    }

    /// <summary>
    /// Select a different world.
    /// </summary>
    public async Task SelectWorldAsync(Guid worldId)
    {
        if (worldId != Guid.Empty && worldId != _facade.CurrentWorldId)
        {
            await _facade.SelectWorldAsync(worldId);
        }
    }

    /// <summary>
    /// Select a different campaign.
    /// </summary>
    public async Task SelectCampaignAsync(Guid? campaignId)
    {
        await _facade.SelectCampaignAsync(campaignId);
    }

    /// <summary>
    /// Create a new world.
    /// </summary>
    public async Task CreateWorldAsync()
    {
        await _facade.CreateWorldAsync();
    }

    /// <summary>
    /// Create a new campaign in the current world.
    /// </summary>
    public async Task CreateCampaignAsync()
    {
        if (_facade.CurrentWorldId.HasValue)
        {
            await _facade.CreateCampaignAsync(_facade.CurrentWorldId.Value);
        }
    }

    public void Dispose()
    {
        _facade.OnContextChanged -= OnContextChanged;
    }
}
