using Bunit;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class WikiLinkAutocompleteTests : Bunit.TestContext
{
    private readonly IWikiLinkAutocompleteService _service;

    public WikiLinkAutocompleteTests()
    {
        _service = Substitute.For<IWikiLinkAutocompleteService>();
        _service.Suggestions.Returns([]);
        _service.Query.Returns(string.Empty);
        _service.Position.Returns((10d, 20d));
        Services.AddSingleton(_service);
    }

    [Fact]
    public void HiddenAutocomplete_AppliesDisplayNone()
    {
        _service.IsVisible.Returns(false);

        var cut = RenderComponent<WikiLinkAutocomplete>();

        Assert.Contains("display: none;", cut.Find(".wiki-link-autocomplete").GetAttribute("style"));
    }

    [Fact]
    public void LoadingState_RendersSpinnerText()
    {
        _service.IsVisible.Returns(true);
        _service.IsLoading.Returns(true);

        var cut = RenderComponent<WikiLinkAutocomplete>();

        Assert.Contains("Searching...", cut.Markup);
    }

    [Fact]
    public void EmptyQuery_RendersTypeToSearchMessage()
    {
        _service.IsVisible.Returns(true);
        _service.IsLoading.Returns(false);
        _service.Query.Returns(string.Empty);

        var cut = RenderComponent<WikiLinkAutocomplete>();

        Assert.Contains("Type to search...", cut.Markup);
    }

    [Fact]
    public void NonEmptyQueryWithoutResults_RendersNoResultsMessage()
    {
        _service.IsVisible.Returns(true);
        _service.IsLoading.Returns(false);
        _service.Query.Returns("acid");

        var cut = RenderComponent<WikiLinkAutocomplete>();

        Assert.Contains("No results found", cut.Markup);
    }

    [Fact]
    public async Task SuggestionClick_InvokesCallbackAndHidesAutocomplete()
    {
        var selected = (WikiLinkAutocompleteItem?)null;
        var suggestion = new WikiLinkAutocompleteItem
        {
            DisplayText = "Acid Arrow",
            Tooltip = "Spells",
            IsExternal = false
        };

        _service.IsVisible.Returns(true);
        _service.IsLoading.Returns(false);
        _service.Suggestions.Returns([suggestion]);
        _service.SelectedIndex.Returns(0);

        var cut = RenderComponent<WikiLinkAutocomplete>(parameters => parameters
            .Add(x => x.OnSuggestionSelected, item => selected = item));

        await cut.Find(".wiki-link-suggestion").ClickAsync(new());

        Assert.Same(suggestion, selected);
        _service.Received(1).Hide();
    }

    [Fact]
    public async Task MouseEnter_UpdatesSelectedIndex()
    {
        var suggestion = new WikiLinkAutocompleteItem { DisplayText = "Acid Arrow" };
        _service.IsVisible.Returns(true);
        _service.Suggestions.Returns([suggestion]);

        var cut = RenderComponent<WikiLinkAutocomplete>();

        await cut.Find(".wiki-link-suggestion").TriggerEventAsync("onmouseenter", new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        _service.Received(1).SetSelectedIndex(0);
    }

    [Fact]
    public void ExternalCategorySuggestion_RendersFolderAndBadge()
    {
        var suggestion = new WikiLinkAutocompleteItem
        {
            DisplayText = "Spells",
            IsExternal = true,
            IsCategory = true,
            Tooltip = "srd"
        };

        _service.IsVisible.Returns(true);
        _service.Suggestions.Returns([suggestion]);

        var cut = RenderComponent<WikiLinkAutocomplete>();

        Assert.Contains("wiki-link-suggestion-badge", cut.Markup);
        Assert.Contains("Spells", cut.Markup);
        Assert.Contains("srd", cut.Markup);
    }

    [Fact]
    public void ExternalNonCategorySuggestion_RendersLinkIcon()
    {
        var suggestion = new WikiLinkAutocompleteItem
        {
            DisplayText = "Acid Arrow",
            IsExternal = true,
            IsCategory = false,
            Tooltip = "srd"
        };

        _service.IsVisible.Returns(true);
        _service.Suggestions.Returns([suggestion]);

        var cut = RenderComponent<WikiLinkAutocomplete>();

        Assert.Contains("Acid Arrow", cut.Markup);
        Assert.Contains("wiki-link-suggestion-badge", cut.Markup);
    }

    [Fact]
    public void InternalSuggestionWithTooltip_RendersPathRow()
    {
        var suggestion = new WikiLinkAutocompleteItem
        {
            DisplayText = "Acid Arrow",
            IsExternal = false,
            Tooltip = "Spells/Level 2"
        };

        _service.IsVisible.Returns(true);
        _service.Suggestions.Returns([suggestion]);

        var cut = RenderComponent<WikiLinkAutocomplete>();

        Assert.Contains("wiki-link-suggestion-path", cut.Markup);
        Assert.Contains("Spells/Level 2", cut.Markup);
    }

    [Fact]
    public void InternalSuggestionWithoutTooltip_DoesNotRenderPathRow()
    {
        var suggestion = new WikiLinkAutocompleteItem
        {
            DisplayText = "Acid Arrow",
            IsExternal = false,
            Tooltip = null
        };

        _service.IsVisible.Returns(true);
        _service.Suggestions.Returns([suggestion]);

        var cut = RenderComponent<WikiLinkAutocomplete>();

        Assert.Empty(cut.FindAll(".wiki-link-suggestion-path"));
    }

    [Fact]
    public void Dispose_CanBeCalledTwice()
    {
        var cut = RenderComponent<WikiLinkAutocomplete>();

        cut.Instance.Dispose();
        cut.Instance.Dispose();

        Assert.True(true);
    }
}
