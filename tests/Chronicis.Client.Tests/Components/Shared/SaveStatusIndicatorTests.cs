using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Shared;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the SaveStatusIndicator component.
/// This component displays the current save status (saving, unsaved, saved).
/// </summary>
[ExcludeFromCodeCoverage]
public class SaveStatusIndicatorTests : MudBlazorTestContext
{
    [Fact]
    public void SaveStatusIndicator_WhenSaving_ShowsSavingMessage()
    {
        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.IsSaving, true));

        // Assert
        Assert.Contains("Saving...", cut.Markup);
    }

    [Fact]
    public void SaveStatusIndicator_WhenSaving_ShowsProgressCircular()
    {
        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.IsSaving, true));

        // Assert
        var progress = cut.FindComponent<MudProgressCircular>();
        Assert.NotNull(progress);
        Assert.Equal(Size.Small, progress.Instance.Size);
        Assert.True(progress.Instance.Indeterminate);
    }

    [Fact]
    public void SaveStatusIndicator_WithUnsavedChanges_ShowsUnsavedMessage()
    {
        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.HasUnsavedChanges, true));

        // Assert
        Assert.Contains("⚠️ Unsaved changes", cut.Markup);
    }

    [Fact]
    public void SaveStatusIndicator_WhenSaved_ShowsSavedMessage()
    {
        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.IsSaving, false)
            .Add(p => p.HasUnsavedChanges, false));

        // Assert
        Assert.Contains("✓ Saved", cut.Markup);
    }

    [Fact]
    public void SaveStatusIndicator_WithLastSaveTime_IncludesTimeInMessage()
    {
        // Arrange
        var lastSaveTime = "2 minutes ago";

        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.IsSaving, false)
            .Add(p => p.HasUnsavedChanges, false)
            .Add(p => p.LastSaveTime, lastSaveTime));

        // Assert
        Assert.Contains($"✓ Saved {lastSaveTime}", cut.Markup);
    }

    [Fact]
    public void SaveStatusIndicator_WithoutLastSaveTime_ShowsJustSaved()
    {
        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.IsSaving, false)
            .Add(p => p.HasUnsavedChanges, false)
            .Add(p => p.LastSaveTime, (string?)null));

        // Assert
        Assert.Contains("✓ Saved", cut.Markup);
        Assert.DoesNotContain("✓ Saved ", cut.Markup); // No trailing space when no time
    }

    [Fact]
    public void SaveStatusIndicator_AppliesSavingClass_WhenSaving()
    {
        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.IsSaving, true));

        // Assert
        var statusDiv = cut.Find(".chronicis-save-status");
        Assert.Contains("saving", statusDiv.ClassName);
    }

    [Fact]
    public void SaveStatusIndicator_AppliesUnsavedClass_WhenUnsaved()
    {
        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.HasUnsavedChanges, true));

        // Assert
        var statusDiv = cut.Find(".chronicis-save-status");
        Assert.Contains("unsaved", statusDiv.ClassName);
    }

    [Fact]
    public void SaveStatusIndicator_AppliesSavedClass_WhenSaved()
    {
        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.IsSaving, false)
            .Add(p => p.HasUnsavedChanges, false));

        // Assert
        var statusDiv = cut.Find(".chronicis-save-status");
        Assert.Contains("saved", statusDiv.ClassName);
    }

    [Fact]
    public void SaveStatusIndicator_SavingTakesPrecedence_OverUnsavedChanges()
    {
        // Act - Both IsSaving and HasUnsavedChanges are true
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.IsSaving, true)
            .Add(p => p.HasUnsavedChanges, true));

        // Assert - Should show saving state
        Assert.Contains("Saving...", cut.Markup);
        Assert.DoesNotContain("Unsaved changes", cut.Markup);
    }

    [Theory]
    [InlineData(true, false, false, "Saving...")]
    [InlineData(false, true, false, "⚠️ Unsaved changes")]
    [InlineData(false, false, false, "✓ Saved")]
    public void SaveStatusIndicator_DisplaysCorrectMessage_ForStateCombo(
        bool isSaving,
        bool hasUnsaved,
        bool includeTime,
        string expectedText)
    {
        // Arrange
        var lastSaveTime = includeTime ? "just now" : null;

        // Act
        var cut = RenderComponent<SaveStatusIndicator>(parameters => parameters
            .Add(p => p.IsSaving, isSaving)
            .Add(p => p.HasUnsavedChanges, hasUnsaved)
            .Add(p => p.LastSaveTime, lastSaveTime));

        // Assert
        Assert.Contains(expectedText, cut.Markup);
    }
}
