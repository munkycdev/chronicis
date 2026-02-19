using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Dashboard;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Dashboard;

[ExcludeFromCodeCoverage]
public class WorldPanelTests : MudBlazorTestContext
{
    private readonly IArticleApiService _articleApi = Substitute.For<IArticleApiService>();

    public WorldPanelTests()
    {
        Services.AddSingleton(_articleApi);
    }

    [Fact]
    public void ComponentType_IsAvailable()
    {
        Assert.Equal("WorldPanel", typeof(WorldPanel).Name);
    }

    [Fact]
    public void ToggleExpanded_FlipsState()
    {
        var instance = CreateInstance(CreateWorld());
        SetProperty(instance, "IsExpanded", true);

        InvokePrivate(instance, "ToggleExpanded");

        Assert.False((bool)GetProperty(instance, "IsExpanded")!);
    }

    [Fact]
    public void ViewWorld_NavigatesToWorldPage()
    {
        var world = CreateWorld();
        var instance = CreateInstance(world);
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);

        InvokePrivate(instance, "ViewWorld");

        Assert.Contains($"/world/{world.Slug}", nav!.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSessionNote_WhenNoActiveCampaign_DoesNotNavigate()
    {
        var world = CreateWorld();
        world.Campaigns = new List<DashboardCampaignDto> { new() { Id = Guid.NewGuid(), Name = "Old", IsActive = false } };
        var instance = CreateInstance(world);
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);
        var before = nav!.Uri;

        InvokePrivate(instance, "AddSessionNote");

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public void AddSessionNote_WhenActiveCampaignExists_NavigatesToCampaign()
    {
        var activeId = Guid.NewGuid();
        var world = CreateWorld();
        world.Campaigns = new List<DashboardCampaignDto> { new() { Id = activeId, Name = "Active", IsActive = true } };
        var instance = CreateInstance(world);
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);

        InvokePrivate(instance, "AddSessionNote");

        Assert.Contains($"/world/{world.Slug}/campaign/{activeId}", nav!.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NavigateToCharacter_WhenArticleHasBreadcrumbs_NavigatesToArticlePath()
    {
        var world = CreateWorld();
        var instance = CreateInstance(world);
        var characterId = Guid.NewGuid();
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);
        _articleApi.GetArticleDetailAsync(characterId).Returns(new ArticleDto
        {
            Id = characterId,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Slug = "world" },
                new() { Slug = "hero" }
            }
        });

        await InvokePrivateAsync(instance, "NavigateToCharacter", characterId);

        Assert.Contains("/article/world/hero", nav!.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NavigateToCharacter_WhenArticleMissing_DoesNotNavigate()
    {
        var world = CreateWorld();
        var instance = CreateInstance(world);
        var characterId = Guid.NewGuid();
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);
        var before = nav!.Uri;
        _articleApi.GetArticleDetailAsync(characterId).Returns((ArticleDto?)null);

        await InvokePrivateAsync(instance, "NavigateToCharacter", characterId);

        Assert.Equal(before, nav.Uri);
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(30, "30m ago")]
    [InlineData(120, "2h ago")]
    [InlineData(2880, "2d ago")]
    [InlineData(10080, "1w ago")]
    public void FormatRelativeTime_RecentRanges_ReturnExpectedText(int minutesAgo, string expected)
    {
        var instance = CreateInstance(CreateWorld());
        var when = DateTime.UtcNow.AddMinutes(-minutesAgo);

        var value = (string)InvokePrivate(instance, "FormatRelativeTime", when)!;

        Assert.Equal(expected, value);
    }

    [Fact]
    public void FormatRelativeTime_OlderThanMonth_ReturnsDate()
    {
        var instance = CreateInstance(CreateWorld());
        var when = DateTime.UtcNow.AddDays(-45);

        var value = (string)InvokePrivate(instance, "FormatRelativeTime", when)!;

        Assert.Contains(",", value, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ShowsEmptyCampaignMessage_WhenNoCampaigns()
    {
        var world = CreateWorld();
        world.Campaigns = new List<DashboardCampaignDto>();

        var cut = RenderComponent<WorldPanel>(p => p.Add(x => x.World, world).Add(x => x.IsExpanded, true));

        Assert.Contains("No campaigns yet", cut.Markup);
    }

    [Fact]
    public void Render_ShowsActiveCampaignDetails_WhenActiveCampaignPresent()
    {
        var world = CreateWorld();
        world.Campaigns = new List<DashboardCampaignDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Main Campaign",
                IsActive = true,
                SessionCount = 5,
                ArcCount = 2,
                CurrentArc = new DashboardArcDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Arc One",
                    SessionCount = 3,
                    LatestSessionDate = DateTime.UtcNow.AddDays(-1)
                }
            }
        };

        var cut = RenderComponent<WorldPanel>(p => p.Add(x => x.World, world).Add(x => x.IsExpanded, true));

        Assert.Contains("Active Campaign", cut.Markup);
        Assert.Contains("Main Campaign", cut.Markup);
        Assert.Contains("Current Arc:", cut.Markup);
    }

    [Fact]
    public void Render_HidesDescription_WhenEmpty()
    {
        var world = CreateWorld();
        world.Description = null;

        var cut = RenderComponent<WorldPanel>(p => p.Add(x => x.World, world).Add(x => x.IsExpanded, false));

        Assert.DoesNotContain("A world of adventure", cut.Markup, StringComparison.Ordinal);
    }

    private WorldPanel CreateInstance(DashboardWorldDto world)
    {
        var instance = new WorldPanel();
        SetProperty(instance, "World", world);
        SetProperty(instance, "Navigation", Services.GetRequiredService<NavigationManager>());
        SetProperty(instance, "ArticleApi", _articleApi);
        return instance;
    }

    private static DashboardWorldDto CreateWorld()
    {
        return new DashboardWorldDto
        {
            Id = Guid.NewGuid(),
            Name = "Eberron",
            Slug = "eberron",
            Description = "A world of adventure",
            ArticleCount = 12,
            Campaigns = new List<DashboardCampaignDto>(),
            MyCharacters = new List<DashboardCharacterDto>()
        };
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }

    private static object? GetProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(property);
        return property!.GetValue(instance);
    }

    private static object? InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.Invoke(instance, args);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var result = InvokePrivate(instance, methodName, args);
        if (result is Task task)
        {
            await task;
        }
    }
}
