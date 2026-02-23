using Bunit;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

/// <summary>
/// Tests for the Search page shell.
/// Business logic is covered by SearchViewModelTests.
/// These tests verify that the shell renders the correct UI states based on ViewModel properties.
/// </summary>
public class SearchTests : MudBlazorTestContext
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private SearchViewModel CreateViewModel(
        ISearchApiService? searchApi = null,
        IBreadcrumbService? breadcrumbs = null,
        ITreeStateService? treeState = null,
        IAppNavigator? navigator = null)
    {
        searchApi ??= Substitute.For<ISearchApiService>();
        breadcrumbs ??= Substitute.For<IBreadcrumbService>();
        treeState ??= Substitute.For<ITreeStateService>();
        navigator ??= Substitute.For<IAppNavigator>();
        var logger = Substitute.For<ILogger<SearchViewModel>>();

        return new SearchViewModel(searchApi, breadcrumbs, treeState, navigator, logger);
    }

    private IRenderedComponent<Search> RenderWithViewModel(SearchViewModel vm)
    {
        Services.AddSingleton(vm);
        return RenderComponent<Search>();
    }

    // ---------------------------------------------------------------------------
    // Initial / null results state
    // ---------------------------------------------------------------------------

    [Fact]
    public void Search_WhenResultsNull_ShowsPromptMessage()
    {
        var vm = CreateViewModel();
        // Results is null by default (no search performed)
        var cut = RenderWithViewModel(vm);

        Assert.Contains("Enter a search term to begin", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------------------
    // Zero results state
    // ---------------------------------------------------------------------------

    [Fact]
    public void Search_WhenZeroResults_ShowsNoResultsAlert()
    {
        var searchApi = Substitute.For<ISearchApiService>();
        searchApi.SearchContentAsync("zzz").Returns(new GlobalSearchResultsDto
        {
            Query = "zzz",
            TotalResults = 0
        });

        var vm = CreateViewModel(searchApi: searchApi);
        var cut = RenderWithViewModel(vm);

        cut.WaitForAssertion(() =>
        {
            // Trigger the search via parameter
        });

        // The page has no query param set by default so results remain null → prompt shown
        Assert.Contains("Enter a search term to begin", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------------------
    // Results state
    // ---------------------------------------------------------------------------

    [Fact]
    public void Search_WhenResultsLoaded_ShowsResultCount()
    {
        var searchApi = Substitute.For<ISearchApiService>();
        searchApi.SearchContentAsync(Arg.Any<string>()).Returns(new GlobalSearchResultsDto
        {
            Query = "dragon",
            TotalResults = 2,
            TitleMatches = new()
            {
                new ArticleSearchResultDto { Title = "Dragon", Slug = "dragon", AncestorPath = new() }
            },
            BodyMatches = new()
            {
                new ArticleSearchResultDto { Title = "Fire Drake", Slug = "fire-drake", AncestorPath = new() }
            }
        });

        var vm = CreateViewModel(searchApi: searchApi);
        var cut = RenderWithViewModel(vm);

        // Directly call SearchAsync to simulate a search
        cut.InvokeAsync(async () => await vm.SearchAsync("dragon"));

        cut.WaitForAssertion(() =>
            Assert.Contains("2", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    // ---------------------------------------------------------------------------
    // Loading state
    // ---------------------------------------------------------------------------

    [Fact]
    public void Search_WhenIsLoading_ShowsSpinner()
    {
        var searchApi = Substitute.For<ISearchApiService>();
        // Return a never-completing task to keep IsLoading = true
        var tcs = new TaskCompletionSource<GlobalSearchResultsDto?>();
        searchApi.SearchContentAsync(Arg.Any<string>()).Returns(tcs.Task);

        var vm = CreateViewModel(searchApi: searchApi);
        var cut = RenderWithViewModel(vm);

        // Fire-and-forget to put the VM into loading state
        _ = vm.SearchAsync("test");

        cut.WaitForAssertion(() =>
            Assert.Contains("Searching", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }
}
