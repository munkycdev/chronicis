using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;

namespace Chronicis.Client.ViewModels;

public sealed class WorldSharingViewModel : ViewModelBase
{
    private readonly IWorldApiService _worldApi;
    private readonly IAppNavigator _navigator;
    private readonly IUserNotifier _notifier;
    private readonly ILogger<WorldSharingViewModel> _logger;

    private bool _isPublic;
    private string _pendingSlug = string.Empty;
    private bool _isRenamingSlug;
    private string? _slugRenameError;

    /// <summary>Raised when a change is made that requires the parent world to be saved.</summary>
    public event Action? UnsavedChangesOccurred;

    public WorldSharingViewModel(
        IWorldApiService worldApi,
        IAppNavigator navigator,
        IUserNotifier notifier,
        ILogger<WorldSharingViewModel> logger)
    {
        _worldApi = worldApi;
        _navigator = navigator;
        _notifier = notifier;
        _logger = logger;
    }

    public bool IsPublic
    {
        get => _isPublic;
        set => SetField(ref _isPublic, value);
    }

    public string PendingSlug
    {
        get => _pendingSlug;
        set => SetField(ref _pendingSlug, value);
    }

    public bool IsRenamingSlug
    {
        get => _isRenamingSlug;
        private set => SetField(ref _isRenamingSlug, value);
    }

    public string? SlugRenameError
    {
        get => _slugRenameError;
        private set => SetField(ref _slugRenameError, value);
    }

    /// <summary>Initialises sharing state from a loaded world.</summary>
    public void InitializeFrom(WorldDetailDto world)
    {
        IsPublic = world.IsPublic;
        PendingSlug = world.Slug;
        SlugRenameError = null;
    }

    public void OnPublicToggleChanged()
    {
        UnsavedChangesOccurred?.Invoke();
    }

    /// <summary>
    /// Submits a slug rename. Returns the resolved slug on success, or null on failure.
    /// </summary>
    public async Task<string?> SaveSlugAsync(Guid worldId)
    {
        if (string.IsNullOrWhiteSpace(PendingSlug))
        {
            SlugRenameError = "Slug cannot be empty";
            return null;
        }

        IsRenamingSlug = true;
        SlugRenameError = null;

        try
        {
            var result = await _worldApi.UpdateSlugAsync(worldId, PendingSlug.Trim().ToLowerInvariant());
            if (result != null)
            {
                PendingSlug = result.Slug;
                _notifier.Success("World URL updated");
                return result.Slug;
            }

            SlugRenameError = "Failed to update URL";
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error updating slug for world {WorldId}", worldId);
            _notifier.Error("Failed to update URL");
            SlugRenameError = "Failed to update URL";
            return null;
        }
        finally
        {
            IsRenamingSlug = false;
        }
    }

    public async Task CopyPublicUrlAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            _navigator.NavigateTo($"javascript:navigator.clipboard.writeText('{url}')");
            _notifier.Success("Public URL copied to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error copying public URL");
            _notifier.Error("Failed to copy URL");
        }

        await Task.CompletedTask;
    }

    public string GetPublicUrlBase(string baseUri) =>
        $"{baseUri.TrimEnd('/')}/w/";

    public string GetFullPublicUrl(string baseUri, WorldDetailDto? world)
    {
        if (world == null || !world.IsPublic || string.IsNullOrEmpty(world.Slug))
            return string.Empty;
        return $"{GetPublicUrlBase(baseUri)}{world.Slug}";
    }

    public bool ShouldShowPublicPreview(WorldDetailDto? world) =>
        world is not null && world.IsPublic && !string.IsNullOrEmpty(world.Slug);

    /// <summary>Generates a URL-safe slug from a world name.</summary>
    public static string GenerateSlugFromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var slug = name.Trim().ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s_]+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug.Length >= 3 ? slug : slug.PadRight(3, '0');
    }
}
