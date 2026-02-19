using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Shared;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the EntityListItem component.
/// This component displays a clickable list item for entity lists.
/// </summary>
[ExcludeFromCodeCoverage]
public class EntityListItemTests : MudBlazorTestContext
{
    [Fact]
    public void EntityListItem_WithTitle_RendersTitle()
    {
        // Arrange
        var title = "My Campaign Arc";

        // Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, title));

        // Assert
        Assert.Contains(title, cut.Markup);
    }

    [Fact]
    public void EntityListItem_WithDefaultIcon_ShowsDescriptionIcon()
    {
        // Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var icon = cut.FindComponent<MudIcon>();
        Assert.Equal(Icons.Material.Filled.Description, icon.Instance.Icon);
    }

    [Fact]
    public void EntityListItem_WithCustomIcon_ShowsCustomIcon()
    {
        // Arrange
        var customIcon = Icons.Material.Filled.Star;

        // Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.Icon, customIcon));

        // Assert
        var icon = cut.FindComponent<MudIcon>();
        Assert.Equal(customIcon, icon.Instance.Icon);
    }

    [Fact]
    public void EntityListItem_IconHasSmallSize()
    {
        // Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var icon = cut.FindComponent<MudIcon>();
        Assert.Equal(Size.Small, icon.Instance.Size);
    }

    [Fact]
    public void EntityListItem_OnClick_TriggersCallback()
    {
        // Arrange
        var clicked = false;

        // Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, "Test")
            .Add(p => p.OnClick, () => clicked = true));

        var listItem = cut.FindComponent<MudListItem<string>>();
        listItem.Find(".chronicis-list-item").Click();

        // Assert
        Assert.True(clicked);
    }

    [Fact]
    public void EntityListItem_WithChildContent_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, "Test")
            .AddChildContent("<span class='test-child'>Child Content</span>"));

        // Assert
        Assert.Contains("Child Content", cut.Markup);
        var child = cut.Find(".test-child");
        Assert.NotNull(child);
    }

    [Fact]
    public void EntityListItem_WithoutChildContent_RendersWithoutError()
    {
        // Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        Assert.Contains("Test", cut.Markup);
    }

    [Fact]
    public void EntityListItem_HasCorrectCssClass()
    {
        // Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var listItem = cut.Find(".chronicis-list-item");
        Assert.NotNull(listItem);
    }

    [Fact]
    public void EntityListItem_DisplaysIconAndTitleTogether()
    {
        // Arrange
        var title = "Session 1";
        var icon = Icons.Material.Filled.Event;

        // Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, title)
            .Add(p => p.Icon, icon));

        // Assert
        Assert.Contains(title, cut.Markup);
        var mudIcon = cut.FindComponent<MudIcon>();
        Assert.Equal(icon, mudIcon.Instance.Icon);
    }

    [Theory]
    [InlineData("Arc 1", Icons.Material.Filled.AccountTree)]
    [InlineData("Session 42", Icons.Material.Filled.Event)]
    [InlineData("Chapter 3", Icons.Material.Filled.MenuBook)]
    public void EntityListItem_RendersVariousIconTitleCombinations(string title, string icon)
    {
        // Act
        var cut = RenderComponent<EntityListItem>(parameters => parameters
            .Add(p => p.Title, title)
            .Add(p => p.Icon, icon));

        // Assert
        Assert.Contains(title, cut.Markup);
        var mudIcon = cut.FindComponent<MudIcon>();
        Assert.Equal(icon, mudIcon.Instance.Icon);
    }
}
