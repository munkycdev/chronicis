using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Shared;
using MudBlazor;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the EmptyState component.
/// This component displays an empty state with optional action button.
/// </summary>
[ExcludeFromCodeCoverage]
public class EmptyStateTests : TestContext
{
    [Fact]
    public void EmptyState_RendersWithDefaultValues()
    {
        // Act
        var cut = RenderComponent<EmptyState>();

        // Assert - Check for presence of elements rather than exact markup
        var iconElement = cut.Find(".chronicis-empty-state-icon");
        Assert.Contains("📄", iconElement.TextContent);

        var titleElement = cut.Find(".chronicis-empty-state-title");
        Assert.Contains("Nothing Here Yet", titleElement.TextContent);

        var messageElement = cut.Find(".chronicis-empty-state-message");
        Assert.Contains("Get started by creating your first item.", messageElement.TextContent);
    }

    [Fact]
    public void EmptyState_RendersCustomIcon()
    {
        // Arrange
        var icon = "🎮";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, icon));

        // Assert
        var iconElement = cut.Find(".chronicis-empty-state-icon");
        Assert.Contains(icon, iconElement.TextContent);
    }

    [Fact]
    public void EmptyState_RendersCustomTitle()
    {
        // Arrange
        var title = "No Games Found";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Title, title));

        // Assert
        var titleElement = cut.Find(".chronicis-empty-state-title");
        Assert.Contains(title, titleElement.TextContent);
    }

    [Fact]
    public void EmptyState_RendersCustomMessage()
    {
        // Arrange
        var message = "Create your first campaign to get started.";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var messageElement = cut.Find(".chronicis-empty-state-message");
        Assert.Contains(message, messageElement.TextContent);
    }

    [Fact]
    public void EmptyState_WithNoActionText_DoesNotRenderButton()
    {
        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionText, null));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Empty(buttons);
    }

    [Fact]
    public void EmptyState_WithActionText_RendersButton()
    {
        // Arrange
        var actionText = "Create New";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionText, actionText));

        // Assert
        var button = cut.Find("button");
        Assert.NotNull(button);
        Assert.Contains(actionText, button.TextContent);
    }

    [Fact]
    public void EmptyState_ActionButton_TriggersCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var actionText = "Create New";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionText, actionText)
            .Add(p => p.OnActionClick, () => callbackInvoked = true));

        var button = cut.Find("button");
        button.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void EmptyState_WithCustomActionIcon_RendersIcon()
    {
        // Arrange
        var actionText = "Create New";
        var actionIcon = Icons.Material.Filled.Create;

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.ActionText, actionText)
            .Add(p => p.ActionIcon, actionIcon));

        // Assert
        var button = cut.Find("button");
        Assert.NotNull(button);
    }

    [Fact]
    public void EmptyState_UsesProvidedParameters()
    {
        // Arrange
        var icon = "🏰";
        var title = "No Worlds";
        var message = "Start your adventure by creating a world.";
        var actionText = "Create World";

        // Act
        var cut = RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, icon)
            .Add(p => p.Title, title)
            .Add(p => p.Message, message)
            .Add(p => p.ActionText, actionText));

        // Assert
        Assert.Contains(icon, cut.Markup);
        Assert.Contains(title, cut.Markup);
        Assert.Contains(message, cut.Markup);
        Assert.Contains(actionText, cut.Markup);
    }
}
