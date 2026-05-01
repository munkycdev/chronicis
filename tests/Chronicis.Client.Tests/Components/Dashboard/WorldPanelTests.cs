using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.Dashboard;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Dashboard;

[ExcludeFromCodeCoverage]
public class WorldPanelTests : MudBlazorTestContext
{
    private readonly IArticleApiService _articleApi = Substitute.For<IArticleApiService>();
    private readonly IAppNavigator _navigator = Substitute.For<IAppNavigator>();

    public WorldPanelTests()
    {
        Services.AddSingleton(_articleApi);
        Services.AddSingleton(_navigator);
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
    public async Task ViewWorld_NavigatesToWorldPage()
    {
        var world = CreateWorld();
        _navigator.GoToWorldAsync(world.Slug).Returns(Task.CompletedTask);
        var instance = CreateInstance(world);

        await InvokePrivateAsync(instance, "ViewWorld");

        await _navigator.Received(1).GoToWorldAsync(world.Slug);
    }

    [Fact]
    public async Task AddSessionNote_WhenNoActiveCampaign_DoesNotNavigate()
    {
        var world = CreateWorld();
        world.Campaigns = new List<DashboardCampaignDto> { new() { Id = Guid.NewGuid(), Name = "Old", IsActive = false } };
        var instance = CreateInstance(world);

        await InvokePrivateAsync(instance, "AddSessionNote");

        await _navigator.DidNotReceive().GoToCampaignAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AddSessionNote_WhenActiveCampaignExists_NavigatesToCampaign()
    {
        var world = CreateWorld();
        world.Campaigns = new List<DashboardCampaignDto> { new() { Id = Guid.NewGuid(), Name = "Active", Slug = "active-campaign", IsActive = true } };
        _navigator.GoToCampaignAsync(world.Slug, "active-campaign").Returns(Task.CompletedTask);
        var instance = CreateInstance(world);

        await InvokePrivateAsync(instance, "AddSessionNote");

        await _navigator.Received(1).GoToCampaignAsync(world.Slug, "active-campaign");
    }

    [Fact]
    public async Task NavigateToCharacter_WhenArticleFound_CallsGoToArticleAsync()
    {
        var world = CreateWorld();
        var instance = CreateInstance(world);
        var characterId = Guid.NewGuid();
        var article = new ArticleDto
        {
            Id = characterId,
            WorldSlug = "world",
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Slug = "world", IsWorld = true },
                new() { Slug = "hero" }
            }
        };
        _articleApi.GetArticleDetailAsync(characterId).Returns(article);
        _navigator.GoToArticleAsync(article).Returns(Task.CompletedTask);

        await InvokePrivateAsync(instance, "NavigateToCharacter", characterId);

        await _navigator.Received(1).GoToArticleAsync(article);
    }

    [Fact]
    public async Task NavigateToCharacter_WhenArticleMissing_DoesNotNavigate()
    {
        var world = CreateWorld();
        var instance = CreateInstance(world);
        var characterId = Guid.NewGuid();
        _articleApi.GetArticleDetailAsync(characterId).Returns((ArticleDto?)null);

        await InvokePrivateAsync(instance, "NavigateToCharacter", characterId);

        await _navigator.DidNotReceive().GoToArticleAsync(Arg.Any<ArticleDto>());
    }

    [Fact]
    public async Task NavigateToCharacter_WhenBreadcrumbsEmpty_StillCallsGoToArticleAsync()
    {
        var world = CreateWorld();
        var instance = CreateInstance(world);
        var characterId = Guid.NewGuid();
        var article = new ArticleDto { Id = characterId, Breadcrumbs = new List<BreadcrumbDto>() };
        _articleApi.GetArticleDetailAsync(characterId).Returns(article);
        _navigator.GoToArticleAsync(article).Returns(Task.CompletedTask);

        await InvokePrivateAsync(instance, "NavigateToCharacter", characterId);

        await _navigator.Received(1).GoToArticleAsync(article);
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

    [Fact]
    public void Render_WhenHasCampaignsButNoneActive_HidesCampaignSections()
    {
        var world = CreateWorld();
        world.Campaigns = new List<DashboardCampaignDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Dormant", IsActive = false, SessionCount = 1, ArcCount = 1 }
        };

        var cut = RenderComponent<WorldPanel>(p => p.Add(x => x.World, world).Add(x => x.IsExpanded, true));

        Assert.DoesNotContain("Active Campaign", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("No campaigns yet", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenWorldHasCharacters_ShowsCharacterChip()
    {
        var world = CreateWorld();
        world.Campaigns = new List<DashboardCampaignDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Solo Campaign",
                IsActive = true,
                SessionCount = 1,
                ArcCount = 1,
                CurrentArc = new DashboardArcDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Solo Arc",
                    SessionCount = 1
                }
            }
        };
        world.MyCharacters = new List<DashboardCharacterDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Thorne", IconEmoji = "🛡️" }
        };

        var cut = RenderComponent<WorldPanel>(p => p.Add(x => x.World, world).Add(x => x.IsExpanded, true));

        Assert.Contains("My Characters", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Thorne", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("1 campaign", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("1 character", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_CharacterChipClick_CallsGoToArticleAsync()
    {
        var characterId = Guid.NewGuid();
        var article = new ArticleDto
        {
            Id = characterId,
            WorldSlug = "party",
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Slug = "party", IsWorld = true },
                new() { Slug = "thorne" }
            }
        };
        _articleApi.GetArticleDetailAsync(characterId).Returns(article);
        _navigator.GoToArticleAsync(article).Returns(Task.CompletedTask);

        var world = CreateWorld();
        world.MyCharacters = new List<DashboardCharacterDto>
        {
            new() { Id = characterId, Title = "Thorne", IconEmoji = "🛡️" }
        };

        var cut = RenderComponent<WorldPanel>(p => p.Add(x => x.World, world).Add(x => x.IsExpanded, true));

        var chip = cut.Find(".character-chip");
        chip.Click();

        cut.WaitForAssertion(async () =>
            await _navigator.Received(1).GoToArticleAsync(article));
    }

    private WorldPanel CreateInstance(DashboardWorldDto world)
    {
        var instance = new WorldPanel();
        SetProperty(instance, "World", world);
        SetProperty(instance, "ArticleApi", _articleApi);
        SetProperty(instance, "AppNavigator", _navigator);
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
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
