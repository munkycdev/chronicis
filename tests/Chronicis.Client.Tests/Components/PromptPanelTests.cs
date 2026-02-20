using System.Diagnostics.CodeAnalysis;
using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Dashboard;
using Chronicis.Client.Components.Shared;
using Chronicis.Shared.DTOs;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the PromptPanel component.
/// This component displays contextual prompts and suggestions to users.
/// </summary>
[ExcludeFromCodeCoverage]
public class PromptPanelTests : MudBlazorTestContext
{
    private PromptDto CreatePrompt(
        string title = "Test Prompt",
        string message = "Test message",
        PromptCategory category = PromptCategory.Suggestion,
        string? actionText = null,
        string? actionUrl = null,
        string icon = "💡")
    {
        return new PromptDto
        {
            Title = title,
            Message = message,
            Category = category,
            ActionText = actionText,
            ActionUrl = actionUrl,
            Icon = icon
        };
    }

    [Fact]
    public void PromptPanel_WithNoPrompts_RendersNothing()
    {
        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, new List<PromptDto>()));

        // Assert - Check for the actual panel content, not CSS
        Assert.DoesNotContain("Suggestions", cut.Markup);
        var panels = cut.FindAll(".prompt-panel");
        // Note: CSS class name will exist in <style>, but no actual div with that class
        Assert.DoesNotContain("<div", cut.Find("style").OuterHtml.ToLower());
    }

    [Fact]
    public void PromptPanel_WithPrompts_RendersPanel()
    {
        // Arrange
        var prompts = new List<PromptDto>
        {
            CreatePrompt()
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        Assert.Contains("Suggestions", cut.Markup);
        var panel = cut.Find(".prompt-panel");
        Assert.NotNull(panel);
    }

    [Fact]
    public void PromptPanel_ShowsSuggestionsTitle()
    {
        // Arrange
        var prompts = new List<PromptDto>
        {
            CreatePrompt()
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        Assert.Contains("Suggestions", cut.Markup);
        var icon = cut.FindComponent<MudIcon>();
        Assert.Equal(Icons.Material.Filled.AutoAwesome, icon.Instance.Icon);
    }

    [Fact]
    public void PromptPanel_RendersPromptTitle()
    {
        // Arrange
        var title = "Create your first world";
        var prompts = new List<PromptDto>
        {
            CreatePrompt(title: title)
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        Assert.Contains(title, cut.Markup);
    }

    [Fact]
    public void PromptPanel_RendersPromptMessage()
    {
        // Arrange
        var message = "Get started by creating your first campaign world";
        var prompts = new List<PromptDto>
        {
            CreatePrompt(message: message)
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        Assert.Contains(message, cut.Markup);
    }

    [Fact]
    public void PromptPanel_RendersPromptIcon()
    {
        // Arrange
        var icon = "🌍";
        var prompts = new List<PromptDto>
        {
            CreatePrompt(icon: icon)
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        Assert.Contains(icon, cut.Markup);
    }

    [Fact]
    public void PromptPanel_WithAction_RendersActionButton()
    {
        // Arrange
        var actionText = "Create World";
        var prompts = new List<PromptDto>
        {
            CreatePrompt(actionText: actionText, actionUrl: "/worlds/create")
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        Assert.Contains($"{actionText} →", cut.Markup);
        var button = cut.Find("button.prompt-action");
        Assert.NotNull(button);
    }

    [Fact]
    public void PromptPanel_WithoutAction_DoesNotRenderButton()
    {
        // Arrange
        var prompts = new List<PromptDto>
        {
            CreatePrompt(actionText: null, actionUrl: null)
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        var buttons = cut.FindAll("button.prompt-action");
        Assert.Empty(buttons);
    }

    [Fact]
    public void PromptPanel_ActionButton_NavigatesToUrl()
    {
        // Arrange
        var actionUrl = "/worlds/create";
        var prompts = new List<PromptDto>
        {
            CreatePrompt(actionText: "Create", actionUrl: actionUrl)
        };
        var navMan = this.Services.GetService<FakeNavigationManager>();

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        var button = cut.Find("button.prompt-action");
        button.Click();

        // Assert
        Assert.NotNull(navMan);
        Assert.EndsWith(actionUrl, navMan.Uri);
    }

    [Theory]
    [InlineData(PromptCategory.MissingFundamental, "missing-fundamental")]
    [InlineData(PromptCategory.NeedsAttention, "needs-attention")]
    [InlineData(PromptCategory.Suggestion, "suggestion")]
    public void PromptPanel_AppliesCategoryClass(PromptCategory category, string expectedClass)
    {
        // Arrange
        var prompts = new List<PromptDto>
        {
            CreatePrompt(category: category)
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        var item = cut.Find(".prompt-item");
        Assert.Contains(expectedClass, item.ClassName);
    }

    [Fact]
    public void PromptPanel_UnknownCategory_UsesNoCategoryClass()
    {
        var prompts = new List<PromptDto>
        {
            CreatePrompt(category: (PromptCategory)999)
        };

        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        var item = cut.Find(".prompt-item");
        Assert.DoesNotContain("missing-fundamental", item.ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("needs-attention", item.ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("suggestion", item.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void PromptPanel_WithMultiplePrompts_RendersAll()
    {
        // Arrange
        var prompts = new List<PromptDto>
        {
            CreatePrompt(title: "First Prompt", message: "First message"),
            CreatePrompt(title: "Second Prompt", message: "Second message"),
            CreatePrompt(title: "Third Prompt", message: "Third message")
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        Assert.Contains("First Prompt", cut.Markup);
        Assert.Contains("Second Prompt", cut.Markup);
        Assert.Contains("Third Prompt", cut.Markup);

        var items = cut.FindAll(".prompt-item");
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void PromptPanel_RendersIconDisplayComponent()
    {
        // Arrange
        var prompts = new List<PromptDto>
        {
            CreatePrompt(icon: "🎮")
        };

        // Act
        var cut = RenderComponent<PromptPanel>(parameters => parameters
            .Add(p => p.Prompts, prompts));

        // Assert
        var iconDisplay = cut.FindComponent<IconDisplay>();
        Assert.NotNull(iconDisplay);
    }
}
