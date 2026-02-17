using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Shared;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the NotFoundAlert component.
/// This component displays a warning alert for "not found" states.
/// </summary>
[ExcludeFromCodeCoverage]
public class NotFoundAlertTests : MudBlazorTestContext
{
    [Fact]
    public void NotFoundAlert_WithDefaultParameters_ShowsDefaultMessage()
    {
        // Act
        var cut = RenderComponent<NotFoundAlert>();

        // Assert
        Assert.Contains("Not found", cut.Markup);
    }

    [Fact]
    public void NotFoundAlert_WithCustomMessage_ShowsCustomMessage()
    {
        // Arrange
        var customMessage = "The item you're looking for doesn't exist";

        // Act
        var cut = RenderComponent<NotFoundAlert>(parameters => parameters
            .Add(p => p.Message, customMessage));

        // Assert
        Assert.Contains(customMessage, cut.Markup);
    }

    [Fact]
    public void NotFoundAlert_WithEntityType_GeneratesMessage()
    {
        // Arrange
        var entityType = "World";

        // Act
        var cut = RenderComponent<NotFoundAlert>(parameters => parameters
            .Add(p => p.EntityType, entityType));

        // Assert
        Assert.Contains("World not found", cut.Markup);
    }

    [Fact]
    public void NotFoundAlert_WithEntityTypeAndCustomMessage_UsesCustomMessage()
    {
        // Arrange
        var entityType = "Campaign";
        var customMessage = "Custom error message";

        // Act
        var cut = RenderComponent<NotFoundAlert>(parameters => parameters
            .Add(p => p.EntityType, entityType)
            .Add(p => p.Message, customMessage));

        // Assert
        Assert.Contains(customMessage, cut.Markup);
        Assert.DoesNotContain("Campaign not found", cut.Markup);
    }

    [Fact]
    public void NotFoundAlert_RendersAsWarning()
    {
        // Act
        var cut = RenderComponent<NotFoundAlert>();

        // Assert
        var alert = cut.FindComponent<MudAlert>();
        Assert.Equal(Severity.Warning, alert.Instance.Severity);
    }

    [Theory]
    [InlineData("Article", "Article not found")]
    [InlineData("Character", "Character not found")]
    [InlineData("Quest", "Quest not found")]
    public void NotFoundAlert_WithVariousEntityTypes_GeneratesCorrectMessages(string entityType, string expectedMessage)
    {
        // Act
        var cut = RenderComponent<NotFoundAlert>(parameters => parameters
            .Add(p => p.EntityType, entityType));

        // Assert
        Assert.Contains(expectedMessage, cut.Markup);
    }
}
