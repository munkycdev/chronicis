using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Chronicis.Client.Components.Admin;

/// <summary>
/// SysAdmin panel for managing tutorial page mappings and opening tutorial articles in the editor.
/// </summary>
public partial class AdminTutorialsPanel : ComponentBase
{
    [Inject] private IAdminApiService AdminApi { get; set; } = default!;
    [Inject] private IArticleApiService ArticleApi { get; set; } = default!;
    [Inject] private IBreadcrumbService BreadcrumbService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<AdminTutorialsPanel> Logger { get; set; } = default!;

    private readonly IReadOnlyList<TutorialPageTypeOption> _pageTypeOptions = TutorialPageTypes.All;

    private List<TutorialMappingDto> _mappings = new();
    private bool _isLoading;
    private bool _isCreating;
    private string? _loadError;

    private string? _selectedPageType;
    private string _pageTypeName = string.Empty;
    private string _tutorialTitle = string.Empty;

    protected override async Task OnInitializedAsync()
        => await LoadMappingsAsync();

    internal async Task LoadMappingsAsync()
    {
        _isLoading = true;
        _loadError = null;
        StateHasChanged();

        try
        {
            _mappings = await AdminApi.GetTutorialMappingsAsync();
        }
        catch (Exception ex)
        {
            _loadError = "Failed to load tutorial mappings.";
            Logger.LogError(ex, "Error loading tutorial mappings");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OnPageTypeChanged(string? pageType)
    {
        _selectedPageType = pageType;

        var selectedOption = TutorialPageTypes.Find(pageType);
        if (selectedOption == null)
        {
            return;
        }

        _pageTypeName = selectedOption.DefaultName;
        _tutorialTitle = selectedOption.PageType == "Page:Default"
            ? "Default Tutorial"
            : $"{selectedOption.DefaultName} Tutorial";
    }

    private async Task CreateTutorialAsync()
    {
        var pageType = _selectedPageType?.Trim();
        var pageTypeName = _pageTypeName.Trim();
        var tutorialTitle = _tutorialTitle.Trim();

        if (string.IsNullOrWhiteSpace(pageType))
        {
            Snackbar.Add("Select a page type.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(pageTypeName))
        {
            Snackbar.Add("Page type name is required.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(tutorialTitle))
        {
            Snackbar.Add("Tutorial article title is required.", Severity.Warning);
            return;
        }

        _isCreating = true;
        try
        {
            var created = await AdminApi.CreateTutorialMappingAsync(new TutorialMappingCreateDto
            {
                PageType = pageType,
                PageTypeName = pageTypeName,
                Title = tutorialTitle,
                Body = "<p>Write tutorial content here.</p>"
            });

            if (created == null)
            {
                Snackbar.Add("Failed to create tutorial mapping. Check for duplicate page type or authorization.", Severity.Error);
                return;
            }

            Snackbar.Add($"Created tutorial mapping for {created.PageType}.", Severity.Success);
            ResetCreateForm();
            await LoadMappingsAsync();
            await OpenTutorialEditorByArticleIdAsync(created.ArticleId);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Unexpected error creating tutorial mapping.", Severity.Error);
            Logger.LogError(ex, "Error creating tutorial mapping for {PageType}", pageType);
        }
        finally
        {
            _isCreating = false;
        }
    }

    private async Task OpenTutorialEditorAsync(string pageType, Guid articleId)
    {
        Logger.LogInformation(
            "Opening tutorial editor from admin row {PageType} -> {ArticleId}",
            pageType,
            articleId);

        await OpenTutorialEditorByArticleIdAsync(articleId);
    }

    private async Task OpenTutorialEditorByArticleIdAsync(Guid articleId)
    {
        try
        {
            var article = await ArticleApi.GetArticleDetailAsync(articleId);
            if (article == null)
            {
                Snackbar.Add("Tutorial article could not be loaded.", Severity.Error);
                return;
            }

            if (article.Breadcrumbs == null || article.Breadcrumbs.Count == 0)
            {
                Snackbar.Add("Tutorial article path could not be resolved.", Severity.Error);
                return;
            }

            if (article.Type == ArticleType.Tutorial &&
                article.WorldId == Guid.Empty &&
                !string.IsNullOrWhiteSpace(article.Slug))
            {
                // Tutorial articles are global/system content. Navigating by slug avoids
                // edge cases in breadcrumb-derived paths for synthetic system-world rows.
                var tutorialUrl = $"/article/system-tutorial/{Uri.EscapeDataString(article.Slug)}";
                Navigation.NavigateTo(tutorialUrl, forceLoad: true);
                return;
            }

            var articleUrl = BreadcrumbService.BuildArticleUrl(article.Breadcrumbs);

            // Tutorial articles are intentionally excluded from the tree. Force a reload so
            // the /article page can bootstrap selection from the route before tree init completes.
            Navigation.NavigateTo(articleUrl, forceLoad: true);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to open tutorial article editor.", Severity.Error);
            Logger.LogError(ex, "Error opening tutorial article editor for {ArticleId}", articleId);
        }
    }

    private void ResetCreateForm()
    {
        _selectedPageType = null;
        _pageTypeName = string.Empty;
        _tutorialTitle = string.Empty;
    }
}
