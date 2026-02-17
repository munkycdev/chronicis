using Bunit;
using Chronicis.Client.Components;
using Chronicis.Client.ViewModels;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the REFACTORED SearchBox component using ViewModel pattern.
/// This component now accepts a ViewModel - trivially easy to test!
/// 
/// ViewModel pattern allows testing business logic separately from UI.
/// </summary>
public class SearchBoxTests : MudBlazorTestContext
{
    [Fact]
    public void SearchBox_RendersWithViewModel()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel();

        // Act
        var cut = RenderComponent<SearchBox_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, viewModel));

        // Assert
        var textField = cut.FindComponent<MudTextField<string>>();
        Assert.NotNull(textField);
    }

    [Fact]
    public void SearchBox_DisplaysSearchIcon_WhenNoText()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel();

        // Act
        var cut = RenderComponent<SearchBox_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, viewModel));

        // Assert
        var textField = cut.FindComponent<MudTextField<string>>();
        Assert.Equal(Icons.Material.Filled.Search, textField.Instance.AdornmentIcon);
    }

    [Fact]
    public void SearchBox_DisplaysClearIcon_WhenHasText()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel { SearchText = "test search" };

        // Act
        var cut = RenderComponent<SearchBox_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, viewModel));

        // Assert
        var textField = cut.FindComponent<MudTextField<string>>();
        Assert.Equal(Icons.Material.Filled.Close, textField.Instance.AdornmentIcon);
    }

    [Fact]
    public async Task SearchBox_CallsExecuteSearch_WhenSearchIconClicked()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel();
        var searchRequested = false;
        viewModel.SearchRequested += _ => searchRequested = true;

        // Act
        var cut = RenderComponent<SearchBox_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, viewModel));

        var textField = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(async () => await textField.Instance.OnAdornmentClick.InvokeAsync());

        // Assert
        Assert.True(searchRequested, "Search should have been requested");
    }

    [Fact]
    public async Task SearchBox_CallsClear_WhenClearIconClicked()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel { SearchText = "test" };
        var clearRequested = false;
        viewModel.ClearRequested += () => clearRequested = true;

        // Act
        var cut = RenderComponent<SearchBox_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, viewModel));

        var textField = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(async () => await textField.Instance.OnAdornmentClick.InvokeAsync());

        // Assert
        Assert.True(clearRequested, "Clear should have been requested");
    }

    [Fact]
    public void SearchBox_ExecutesSearch_OnEnterKey()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel { SearchText = "test" };
        var searchRequested = false;
        var searchText = string.Empty;
        viewModel.SearchRequested += text => { searchRequested = true; searchText = text; };

        var cut = RenderComponent<SearchBox_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, viewModel));

        // Act
        var textField = cut.Find("input");
        textField.KeyDown("Enter");

        // Assert
        Assert.True(searchRequested, "Search should have been requested");
        Assert.Equal("test", searchText);
    }

    [Fact]
    public void SearchBox_ClearsSearch_OnEscapeKey()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel { SearchText = "test" };
        var clearRequested = false;
        viewModel.ClearRequested += () => clearRequested = true;

        var cut = RenderComponent<SearchBox_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, viewModel));

        // Act
        var textField = cut.Find("input");
        textField.KeyDown("Escape");

        // Assert
        Assert.True(clearRequested, "Clear should have been requested");
        Assert.Empty(viewModel.SearchText);
    }

    [Fact]
    public void SearchBox_UpdatesViewModel_WhenTextChanges()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel();
        var textChanged = false;
        var changedText = string.Empty;
        viewModel.SearchTextChanged += text => { textChanged = true; changedText = text; };

        var cut = RenderComponent<SearchBox_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, viewModel));

        // Act
        viewModel.SearchText = "new search";

        // Assert
        Assert.True(textChanged, "SearchTextChanged event should have fired");
        Assert.Equal("new search", changedText);
    }
}

/// <summary>
/// Tests for SearchBoxViewModel itself (business logic).
/// These tests are completely independent of Blazor/UI.
/// </summary>
public class SearchBoxViewModelTests
{
    [Fact]
    public void ViewModel_HasText_ReturnsFalse_WhenEmpty()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel();

        // Assert
        Assert.False(viewModel.HasText);
    }

    [Fact]
    public void ViewModel_HasText_ReturnsTrue_WhenNotEmpty()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel { SearchText = "test" };

        // Assert
        Assert.True(viewModel.HasText);
    }

    [Fact]
    public void ViewModel_RaisesSearchTextChanged_WhenTextChanges()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel();
        var eventFired = false;
        var newText = string.Empty;
        viewModel.SearchTextChanged += text => { eventFired = true; newText = text; };

        // Act
        viewModel.SearchText = "search query";

        // Assert
        Assert.True(eventFired);
        Assert.Equal("search query", newText);
    }

    [Fact]
    public void ViewModel_ExecuteSearch_RaisesSearchRequested()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel { SearchText = "test" };
        var eventFired = false;
        var searchText = string.Empty;
        viewModel.SearchRequested += text => { eventFired = true; searchText = text; };

        // Act
        viewModel.ExecuteSearch();

        // Assert
        Assert.True(eventFired);
        Assert.Equal("test", searchText);
    }

    [Fact]
    public void ViewModel_Clear_RaisesClearRequested()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel { SearchText = "test" };
        var eventFired = false;
        viewModel.ClearRequested += () => eventFired = true;

        // Act
        viewModel.Clear();

        // Assert
        Assert.True(eventFired);
        Assert.Empty(viewModel.SearchText);
    }

    [Fact]
    public void ViewModel_Clear_ClearsSearchText()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel { SearchText = "test" };

        // Act
        viewModel.Clear();

        // Assert
        Assert.Empty(viewModel.SearchText);
    }

    [Fact]
    public void ViewModel_DoesNotRaiseEvent_WhenTextSetToSameValue()
    {
        // Arrange
        var viewModel = new SearchBoxViewModel { SearchText = "test" };
        var eventFired = false;
        viewModel.SearchTextChanged += _ => eventFired = true;

        // Act
        viewModel.SearchText = "test"; // Same value

        // Assert
        Assert.False(eventFired, "Event should not fire when setting same value");
    }
}
