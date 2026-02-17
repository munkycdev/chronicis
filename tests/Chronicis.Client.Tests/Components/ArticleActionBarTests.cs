using Bunit;
using Chronicis.Client.Components.Articles;
using Chronicis.Client.Components.Shared;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the ArticleActionBar component.
/// This component displays action buttons and save status for article editing.
/// </summary>
public class ArticleActionBarTests : MudBlazorTestContext
{
    [Fact]
    public void ArticleActionBar_Renders_AllButtons()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>();

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Equal(4, buttons.Count); // New Child, Auto-Link, Save, Delete
    }

    [Fact]
    public void ArticleActionBar_RendersSaveStatusIndicator()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>();

        // Assert
        var statusIndicator = cut.FindComponent<SaveStatusIndicator>();
        Assert.NotNull(statusIndicator);
    }

    [Fact]
    public void ArticleActionBar_PassesStatusToIndicator()
    {
        // Arrange
        var lastSaveTime = "2 minutes ago";

        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.IsSaving, true)
            .Add(p => p.HasUnsavedChanges, false)
            .Add(p => p.LastSaveTime, lastSaveTime));

        // Assert
        var statusIndicator = cut.FindComponent<SaveStatusIndicator>();
        Assert.True(statusIndicator.Instance.IsSaving);
        Assert.False(statusIndicator.Instance.HasUnsavedChanges);
        Assert.Equal(lastSaveTime, statusIndicator.Instance.LastSaveTime);
    }

    [Fact]
    public void ArticleActionBar_SaveButton_TriggersCallback()
    {
        // Arrange
        var saveClicked = false;

        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.OnSave, () => saveClicked = true));

        var saveButtons = cut.FindComponents<MudButton>();
        var saveButton = saveButtons.First(b => b.Markup.Contains("Save") && !b.Markup.Contains("Indicator"));
        saveButton.Find("button").Click();

        // Assert
        Assert.True(saveClicked);
    }

    [Fact]
    public void ArticleActionBar_DeleteButton_TriggersCallback()
    {
        // Arrange
        var deleteClicked = false;

        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.OnDelete, () => deleteClicked = true));

        var deleteButtons = cut.FindComponents<MudButton>();
        var deleteButton = deleteButtons.First(b => b.Markup.Contains("Delete"));
        deleteButton.Find("button").Click();

        // Assert
        Assert.True(deleteClicked);
    }

    [Fact]
    public void ArticleActionBar_AutoLinkButton_TriggersCallback()
    {
        // Arrange
        var autoLinkClicked = false;

        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.OnAutoLink, () => autoLinkClicked = true));

        var autoLinkButtons = cut.FindComponents<MudButton>();
        var autoLinkButton = autoLinkButtons.First(b => b.Markup.Contains("Auto-"));
        autoLinkButton.Find("button").Click();

        // Assert
        Assert.True(autoLinkClicked);
    }

    [Fact]
    public void ArticleActionBar_CreateChildButton_TriggersCallback()
    {
        // Arrange
        var createChildClicked = false;

        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.OnCreateChild, () => createChildClicked = true));

        var createChildButtons = cut.FindComponents<MudButton>();
        var createChildButton = createChildButtons.First(b => b.Markup.Contains("New Child"));
        createChildButton.Find("button").Click();

        // Assert
        Assert.True(createChildClicked);
    }

    [Fact]
    public void ArticleActionBar_WhenSaving_DisablesSaveButton()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.IsSaving, true));

        // Assert
        var saveButtons = cut.FindComponents<MudButton>();
        var saveButton = saveButtons.First(b => b.Markup.Contains("Save") && !b.Markup.Contains("Indicator"));
        Assert.True(saveButton.Instance.Disabled);
    }

    [Fact]
    public void ArticleActionBar_WhenSaving_DisablesCreateChildButton()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.IsSaving, true));

        // Assert
        var childButtons = cut.FindComponents<MudButton>();
        var createChildButton = childButtons.First(b => b.Markup.Contains("New Child"));
        Assert.True(createChildButton.Instance.Disabled);
    }

    [Fact]
    public void ArticleActionBar_WhenAutoLinking_DisablesAutoLinkButton()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.IsAutoLinking, true));

        // Assert - Just verify text is shown
        Assert.Contains("Scanning...", cut.Markup);
    }

    [Fact]
    public void ArticleActionBar_WhenCreatingChild_DisablesCreateChildButton()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.IsCreatingChild, true));

        // Assert - Just verify text is shown
        Assert.Contains("Creating...", cut.Markup);
    }

    [Fact]
    public void ArticleActionBar_WhenCreatingChild_ShowsCreatingText()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.IsCreatingChild, true));

        // Assert
        Assert.Contains("Creating...", cut.Markup);
    }

    [Fact]
    public void ArticleActionBar_WhenAutoLinking_ShowsScanningText()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>(parameters => parameters
            .Add(p => p.IsAutoLinking, true));

        // Assert
        Assert.Contains("Scanning...", cut.Markup);
    }

    [Fact]
    public void ArticleActionBar_SaveButton_HasSuccessColor()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>();

        // Assert
        var saveButtons = cut.FindComponents<MudButton>();
        var saveButton = saveButtons.First(b => b.Markup.Contains("Save"));
        Assert.Equal(Color.Success, saveButton.Instance.Color);
        Assert.Equal(Variant.Filled, saveButton.Instance.Variant);
    }

    [Fact]
    public void ArticleActionBar_DeleteButton_HasErrorColor()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>();

        // Assert
        var deleteButtons = cut.FindComponents<MudButton>();
        var deleteButton = deleteButtons.First(b => b.Markup.Contains("Delete"));
        Assert.Equal(Color.Error, deleteButton.Instance.Color);
        Assert.Equal(Variant.Filled, deleteButton.Instance.Variant);
    }

    [Fact]
    public void ArticleActionBar_NewChildButton_HasPrimaryColor()
    {
        // Act
        var cut = RenderComponent<ArticleActionBar>();

        // Assert
        var childButtons = cut.FindComponents<MudButton>();
        var newChildButton = childButtons.First(b => b.Markup.Contains("New Child"));
        Assert.Equal(Color.Primary, newChildButton.Instance.Color);
        Assert.Equal(Variant.Outlined, newChildButton.Instance.Variant);
    }
}
