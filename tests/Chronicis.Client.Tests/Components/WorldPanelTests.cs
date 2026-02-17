using Bunit;
using Chronicis.Client.Components.Dashboard;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for WorldPanel component.
/// This component displays world information on the dashboard.
/// Already well-designed - takes data as parameter, minimal service dependencies for navigation only.
/// </summary>
public class WorldPanelTests : MudBlazorTestContext
{
    private readonly IArticleApiService _mockArticleApi;

    public WorldPanelTests()
    {
        _mockArticleApi = Substitute.For<IArticleApiService>();
        Services.AddScoped(_ => _mockArticleApi);
    }
    [Fact]
    public void WorldPanel_RendersWorldName()
    {
        // Arrange
        var world = CreateTestWorld("Test World");

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world));

        // Assert
        var worldName = cut.Find(".world-name");
        Assert.Equal("Test World", worldName.TextContent);
    }

    [Fact]
    public void WorldPanel_RendersDescription_WhenProvided()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.Description = "A fantastical realm";

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world));

        // Assert
        var description = cut.Find(".world-description");
        Assert.Equal("A fantastical realm", description.TextContent);
    }

    [Fact]
    public void WorldPanel_DoesNotRenderDescription_WhenEmpty()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.Description = null;

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world));

        // Assert
        var descriptions = cut.FindAll(".world-description");
        Assert.Empty(descriptions);
    }

    [Fact]
    public void WorldPanel_DisplaysArticleCount()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.ArticleCount = 42;

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, true));

        // Assert
        var stats = cut.Markup;
        Assert.Contains("42 articles", stats);
    }

    [Fact]
    public void WorldPanel_DisplaysCampaignCount()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.Campaigns = new List<DashboardCampaignDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Campaign 1" },
            new() { Id = Guid.NewGuid(), Name = "Campaign 2" }
        };

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, true));

        // Assert
        var stats = cut.Markup;
        Assert.Contains("2 campaigns", stats);
    }

    [Fact]
    public void WorldPanel_DisplaysCharacterCount()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.MyCharacters = new List<DashboardCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Character 1" },
            new() { Id = Guid.NewGuid(), Title = "Character 2" },
            new() { Id = Guid.NewGuid(), Title = "Character 3" }
        };

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, true));

        // Assert
        var stats = cut.Markup;
        Assert.Contains("3 characters", stats);
    }

    [Fact]
    public void WorldPanel_IsExpandedByDefault()
    {
        // Arrange
        var world = CreateTestWorld("Test World");

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world));

        // Assert - Content should be visible
        var content = cut.FindAll(".world-content");
        Assert.NotEmpty(content);
    }

    [Fact]
    public void WorldPanel_CanBeCollapsed()
    {
        // Arrange
        var world = CreateTestWorld("Test World");

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, false));

        // Assert - Content should NOT be visible
        var content = cut.FindAll(".world-content");
        Assert.Empty(content);
    }

    [Fact]
    public void WorldPanel_TogglesExpansion_WhenHeaderClicked()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        var isExpanded = true;
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, isExpanded)
            .Add(p => p.IsExpandedChanged, newValue => isExpanded = newValue));

        // Verify initially expanded
        Assert.NotEmpty(cut.FindAll(".world-content"));

        // Act - Click header to collapse
        var header = cut.Find(".world-header");
        header.Click();

        // Assert
        Assert.False(isExpanded);
    }

    [Fact]
    public void WorldPanel_ShowsActiveCampaign_WhenPresent()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.Campaigns = new List<DashboardCampaignDto>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                Name = "Active Campaign",
                IsActive = true,
                SessionCount = 5,
                ArcCount = 2
            }
        };

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, true));

        // Assert
        var content = cut.Markup;
        Assert.Contains("Active Campaign", content);
        Assert.Contains("5 sessions", content);
        Assert.Contains("2 arcs", content);
    }

    [Fact]
    public void WorldPanel_ShowsCurrentArc_WhenPresent()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.Campaigns = new List<DashboardCampaignDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Active Campaign",
                IsActive = true,
                CurrentArc = new DashboardArcDto
                {
                    Id = Guid.NewGuid(),
                    Name = "The Dragon's Lair",
                    SessionCount = 3
                }
            }
        };

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, true));

        // Assert
        var content = cut.Markup;
        Assert.Contains("The Dragon's Lair", content);
        Assert.Contains("3 session", content);
    }

    [Fact]
    public void WorldPanel_ShowsEmptyState_WhenNoCampaigns()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.Campaigns = new List<DashboardCampaignDto>();

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, true));

        // Assert
        var emptyState = cut.Find(".empty-section");
        Assert.Contains("No campaigns yet", emptyState.TextContent);
    }

    [Fact]
    public void WorldPanel_RendersCharacterList_WhenCharactersExist()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.MyCharacters = new List<DashboardCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Gandalf", IconEmoji = "🧙" },
            new() { Id = Guid.NewGuid(), Title = "Aragorn", IconEmoji = "🗡️" }
        };

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, true));

        // Assert
        var content = cut.Markup;
        Assert.Contains("Gandalf", content);
        Assert.Contains("Aragorn", content);
        Assert.Contains("My Characters", content);
    }

    [Fact]
    public void WorldPanel_DoesNotShowCharacterSection_WhenNoCharacters()
    {
        // Arrange
        var world = CreateTestWorld("Test World");
        world.MyCharacters = new List<DashboardCharacterDto>();

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, true));

        // Assert
        var content = cut.Markup;
        Assert.DoesNotContain("My Characters", content);
    }

    [Fact]
    public void WorldPanel_AppliesCustomClass_WhenProvided()
    {
        // Arrange
        var world = CreateTestWorld("Test World");

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.Class, "custom-class"));

        // Assert
        var panel = cut.Find(".world-panel");
        Assert.Contains("custom-class", panel.ClassName);
    }

    [Fact]
    public void WorldPanel_ShowsExpandIcon_WhenExpanded()
    {
        // Arrange
        var world = CreateTestWorld("Test World");

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, true));

        // Assert - Find icon with expand-icon class in the header
        var expandIcon = cut.FindComponents<MudIcon>()
            .FirstOrDefault(icon => icon.Instance.Class?.Contains("expand-icon") == true);
        
        Assert.NotNull(expandIcon);
        Assert.Equal(Icons.Material.Filled.ExpandLess, expandIcon.Instance.Icon);
    }

    [Fact]
    public void WorldPanel_ShowsCollapseIcon_WhenCollapsed()
    {
        // Arrange
        var world = CreateTestWorld("Test World");

        // Act
        var cut = RenderComponent<WorldPanel>(parameters => parameters
            .Add(p => p.World, world)
            .Add(p => p.IsExpanded, false));

        // Assert - Find icon with expand-icon class in the header
        var expandIcon = cut.FindComponents<MudIcon>()
            .FirstOrDefault(icon => icon.Instance.Class?.Contains("expand-icon") == true);
        
        Assert.NotNull(expandIcon);
        Assert.Equal(Icons.Material.Filled.ExpandMore, expandIcon.Instance.Icon);
    }

    // Helper method to create test world
    private DashboardWorldDto CreateTestWorld(string name)
    {
        return new DashboardWorldDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLowerInvariant().Replace(" ", "-"),
            ArticleCount = 0,
            Campaigns = new List<DashboardCampaignDto>(),
            MyCharacters = new List<DashboardCharacterDto>()
        };
    }
}
