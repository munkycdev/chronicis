using Bunit;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for AISummarySection_REFACTORED component.
/// Tests rendering with mocked ViewModel - business logic is tested in ViewModelTests.
/// </summary>
public class AISummarySectionTests : MudBlazorTestContext
{
    private readonly IAISummaryFacade _mockFacade;

    public AISummarySectionTests()
    {
        _mockFacade = Substitute.For<IAISummaryFacade>();
    }

    private AISummarySectionViewModel CreateViewModel()
    {
        return new AISummarySectionViewModel(_mockFacade);
    }

    #region Rendering Tests

    [Fact]
    public void Component_RendersHeader()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        var cut = RenderComponent<AISummarySection_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, vm));

        // Assert
        Assert.Contains("AI Summary", cut.Markup);
        var header = cut.Find(".ai-summary-header");
        Assert.NotNull(header);
    }

    [Fact]
    public void Component_RendersLoadingState_WhenViewModelIsLoading()
    {
        // Arrange
        var vm = CreateViewModel();
        typeof(AISummarySectionViewModel)
            .GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, true);
        typeof(AISummarySectionViewModel)
            .GetField("_loadingMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, "Generating summary...");
        typeof(AISummarySectionViewModel)
            .GetField("_isExpanded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, true);

        // Act
        var cut = RenderComponent<AISummarySection_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, vm));

        // Assert
        Assert.Contains("Generating summary...", cut.Markup);
        var progressBar = cut.FindComponent<MudProgressLinear>();
        Assert.NotNull(progressBar);
    }

    [Fact]
    public void Component_RendersNoSourcesMessage_WhenEstimateHasZeroSources()
    {
        // Arrange
        var vm = CreateViewModel();
        var estimate = new SummaryEstimateDto
        {
            SourceCount = 0
        };
        
        typeof(AISummarySectionViewModel)
            .GetField("_estimate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, estimate);
        typeof(AISummarySectionViewModel)
            .GetField("_isExpanded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, true);
        typeof(AISummarySectionViewModel)
            .GetField("_entityType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, "Article");

        // Act
        var cut = RenderComponent<AISummarySection_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, vm));

        // Assert
        Assert.Contains("wiki links", cut.Markup);
    }

    [Fact]
    public void Component_RendersErrorMessage_WhenViewModelHasError()
    {
        // Arrange
        var vm = CreateViewModel();
        typeof(AISummarySectionViewModel)
            .GetField("_errorMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, "Something went wrong");
        typeof(AISummarySectionViewModel)
            .GetField("_isExpanded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, true);

        // Act
        var cut = RenderComponent<AISummarySection_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, vm));

        // Assert
        Assert.Contains("Something went wrong", cut.Markup);
        var alert = cut.FindComponent<MudAlert>();
        Assert.NotNull(alert);
        Assert.Equal(Severity.Error, alert.Instance.Severity);
    }

    [Fact]
    public void Component_ShowsExpandIcon_WhenCollapsed()
    {
        // Arrange
        var vm = CreateViewModel();
        typeof(AISummarySectionViewModel)
            .GetField("_isExpanded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, false);

        // Act
        var cut = RenderComponent<AISummarySection_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, vm));

        // Assert
        var expandIcon = cut.FindComponents<MudIcon>()
            .FirstOrDefault(icon => icon.Instance.Class?.Contains("expand-icon") == true);
        Assert.NotNull(expandIcon);
        Assert.Equal(Icons.Material.Filled.ExpandMore, expandIcon.Instance.Icon);
    }

    [Fact]
    public void Component_ShowsCollapseIcon_WhenExpanded()
    {
        // Arrange
        var vm = CreateViewModel();
        typeof(AISummarySectionViewModel)
            .GetField("_isExpanded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(vm, true);

        // Act
        var cut = RenderComponent<AISummarySection_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, vm));

        // Assert
        var expandIcon = cut.FindComponents<MudIcon>()
            .FirstOrDefault(icon => icon.Instance.Class?.Contains("expand-icon") == true);
        Assert.NotNull(expandIcon);
        Assert.Equal(Icons.Material.Filled.ExpandLess, expandIcon.Instance.Icon);
    }

    #endregion

    #region User Action Tests

    [Fact]
    public async Task Component_CallsViewModelToggle_WhenHeaderClicked()
    {
        // Arrange
        var vm = Substitute.ForPartsOf<AISummarySectionViewModel>(_mockFacade);
        
        var cut = RenderComponent<AISummarySection_REFACTORED>(parameters => parameters
            .Add(p => p.ViewModel, vm));

        // Act
        var header = cut.Find(".ai-summary-header");
        await header.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await vm.Received(1).ToggleExpandedAsync();
    }

    #endregion
}
