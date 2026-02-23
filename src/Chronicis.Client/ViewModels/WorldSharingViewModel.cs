using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel managing public slug and sharing settings for a world.
/// </summary>
public sealed class WorldSharingViewModel : ViewModelBase
{
    private readonly IWorldApiService _worldApi;
    private readonly IAppNavigator _navigator;
    private readonly IUserNotifier _notifier;
    private readonly ILogger<WorldSharingViewModel> _logger;

    private bool _isPublic;
    private string _publicSlug = string.Empty;
    private bool _isCheckingSlug;
    private bool _slugIsAvailable;
    private string? _slugError;
    private string? _slugHelperText;

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

    public string PublicSlug
    {
        get => _publicSlug;
        set => SetField(ref _publicSlug, value);
    }

    public bool IsCheckingSlug
    {
        get => _isCheckingSlug;
        private set => SetField(ref _isCheckingSlug, value);
    }

    public bool SlugIsAvailable
    {
        get => _slugIsAvailable;
        private set => SetField(ref _slugIsAvailable, value);
    }

    public string? SlugError
    {
        get => _slugError;
        private set => SetField(ref _slugError, value);
    }

    public string? SlugHelperText
    {
        get => _slugHelperText;
        private set => SetField(ref _slugHelperText, value);
    }

    /// <summary>Initialises sharing state from a loaded world.</summary>
    public void InitializeFrom(WorldDetailDto world)
    {
        IsPublic = world.IsPublic;
        PublicSlug = world.PublicSlug ?? string.Empty;
        SlugIsAvailable = world.IsPublic;
        SlugError = null;
        SlugHelperText = null;
    }

    public void OnPublicToggleChanged(WorldDetailDto? world)
    {
        UnsavedChangesOccurred?.Invoke();

        if (IsPublic && string.IsNullOrEmpty(PublicSlug))
        {
            PublicSlug = GenerateSlugFromName(world?.Name ?? string.Empty);
            _ = CheckSlugAvailabilityAsync(world?.Id ?? Guid.Empty);
        }
    }

    public async Task CheckSlugAvailabilityAsync(Guid worldId)
    {
        if (string.IsNullOrWhiteSpace(PublicSlug))
        {
            SlugIsAvailable = false;
            SlugError = null;
            SlugHelperText = null;
            return;
        }

        IsCheckingSlug = true;
        SlugError = null;
        SlugHelperText = null;

        try
        {
            var result = await _worldApi.CheckPublicSlugAsync(worldId, PublicSlug);

            if (result == null)
            {
                SlugError = "Failed to check availability";
                SlugIsAvailable = false;
            }
            else if (!string.IsNullOrEmpty(result.ValidationError))
            {
                SlugError = result.ValidationError;
                SlugIsAvailable = false;
                if (!string.IsNullOrEmpty(result.SuggestedSlug))
                    SlugHelperText = $"Try: {result.SuggestedSlug}";
            }
            else if (!result.IsAvailable)
            {
                SlugError = "This slug is already taken";
                SlugIsAvailable = false;
                if (!string.IsNullOrEmpty(result.SuggestedSlug))
                    SlugHelperText = $"Try: {result.SuggestedSlug}";
            }
            else
            {
                SlugIsAvailable = true;
                SlugHelperText = "Available!";
            }

            UnsavedChangesOccurred?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Error checking slug availability for world {WorldId}", worldId);
            SlugError = $"Error: {ex.Message}";
            SlugIsAvailable = false;
        }
        finally
        {
            IsCheckingSlug = false;
        }
    }

    public async Task CopyPublicUrlAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            // Navigation.BaseUri is passed in by the page to keep IJSRuntime out of the VM
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
        if (world == null || string.IsNullOrEmpty(world.PublicSlug))
            return string.Empty;
        return $"{GetPublicUrlBase(baseUri)}{world.PublicSlug}";
    }

    public bool ShouldShowPublicPreview(WorldDetailDto? world) =>
        world is not null && world.IsPublic && !string.IsNullOrEmpty(world.PublicSlug);

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
