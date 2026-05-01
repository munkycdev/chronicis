using Chronicis.Client.Infrastructure;
using Chronicis.Client.Services.Routing;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Infrastructure;

public class AppNavigatorTests
{
    private sealed class TestNavigationManager : NavigationManager
    {
        private readonly List<(string Uri, bool Replace)> _navigations = new();
        public IReadOnlyList<(string Uri, bool Replace)> Navigations => _navigations;

        public TestNavigationManager(string baseUri = "https://app.test/", string currentUri = "https://app.test/")
        {
            Initialize(baseUri, currentUri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad) =>
            _navigations.Add((uri, forceLoad));
    }

    private static (TestNavigationManager Nav, IAppUrlBuilder UrlBuilder, AppNavigator Sut) Create()
    {
        var nav = new TestNavigationManager();
        var urlBuilder = new AppUrlBuilder();
        return (nav, urlBuilder, new AppNavigator(nav, urlBuilder, NullLogger<AppNavigator>.Instance));
    }

    [Fact]
    public void BaseUri_ReturnsNavigationManagerBaseUri()
    {
        var nav = new TestNavigationManager(baseUri: "https://chronicis.app/", currentUri: "https://chronicis.app/");
        var sut = new AppNavigator(nav, new AppUrlBuilder(), NullLogger<AppNavigator>.Instance);

        Assert.Equal("https://chronicis.app/", sut.BaseUri);
    }

    [Fact]
    public void Uri_ReturnsNavigationManagerUri()
    {
        var nav = new TestNavigationManager(
            baseUri: "https://chronicis.app/",
            currentUri: "https://chronicis.app/world/123");
        var sut = new AppNavigator(nav, new AppUrlBuilder(), NullLogger<AppNavigator>.Instance);

        Assert.Equal("https://chronicis.app/world/123", sut.Uri);
    }

    [Fact]
    public void NavigateTo_WithDefaultReplace_NavigatesWithReplacefalse()
    {
        var (nav, _, sut) = Create();

        sut.NavigateTo("/dashboard");

        Assert.Single(nav.Navigations);
        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }

    [Fact]
    public void NavigateTo_WithReplaceTrue_NavigatesWithReplaceTrue()
    {
        var (nav, _, sut) = Create();

        sut.NavigateTo("/dashboard", replace: true);

        Assert.Single(nav.Navigations);
        Assert.Equal(("/dashboard", true), nav.Navigations[0]);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Slug navigation methods
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GoToWorldAsync_NavigatesToWorldSlugUrl()
    {
        var (nav, _, sut) = Create();

        await sut.GoToWorldAsync("middle-earth");

        Assert.Equal(("/middle-earth", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToWorldAsync_WithReplace_PassesReplaceFlag()
    {
        var (nav, _, sut) = Create();

        await sut.GoToWorldAsync("middle-earth", replace: true);

        Assert.Equal(("/middle-earth", true), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToCampaignAsync_NavigatesToCampaignSlugUrl()
    {
        var (nav, _, sut) = Create();

        await sut.GoToCampaignAsync("middle-earth", "fellowship");

        Assert.Equal(("/middle-earth/fellowship", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToCampaignAsync_DtoOverload_UsesSlugFields()
    {
        var (nav, _, sut) = Create();
        var campaign = new CampaignDto { Slug = "the-campaign", WorldSlug = "my-world" };

        await sut.GoToCampaignAsync(campaign);

        Assert.Equal(("/my-world/the-campaign", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArcAsync_NavigatesToArcSlugUrl()
    {
        var (nav, _, sut) = Create();

        await sut.GoToArcAsync("me", "wotr", "fellowship");

        Assert.Equal(("/me/wotr/fellowship", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArcAsync_DtoOverload_UsesSlugFields()
    {
        var (nav, _, sut) = Create();
        var arc = new ArcDto { Slug = "the-arc", CampaignSlug = "the-campaign", WorldSlug = "my-world" };

        await sut.GoToArcAsync(arc);

        Assert.Equal(("/my-world/the-campaign/the-arc", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArcAsync_WithReplace_PassesReplaceFlag()
    {
        var (nav, _, sut) = Create();

        await sut.GoToArcAsync("me", "wotr", "fellowship", replace: true);

        Assert.Equal(("/me/wotr/fellowship", true), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToSessionAsync_NavigatesToSessionSlugUrl()
    {
        var (nav, _, sut) = Create();

        await sut.GoToSessionAsync("me", "wotr", "fellowship", "session-1");

        Assert.Equal(("/me/wotr/fellowship/session-1", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToSessionAsync_DtoOverload_UsesSlugFields()
    {
        var (nav, _, sut) = Create();
        var session = new SessionTreeDto
        {
            Slug = "session-1",
            ArcSlug = "fellowship",
            CampaignSlug = "wotr",
            WorldSlug = "me"
        };

        await sut.GoToSessionAsync(session);

        Assert.Equal(("/me/wotr/fellowship/session-1", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToSessionNoteAsync_NavigatesToFiveSegmentUrl()
    {
        var (nav, _, sut) = Create();

        await sut.GoToSessionNoteAsync("me", "wotr", "fellowship", "session-1", "my-note");

        Assert.Equal(("/me/wotr/fellowship/session-1/my-note", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToMapListingAsync_NavigatesToMapsUrl()
    {
        var (nav, _, sut) = Create();

        await sut.GoToMapListingAsync("middle-earth");

        Assert.Equal(("/middle-earth/maps", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToMapAsync_NavigatesToMapSlugUrl()
    {
        var (nav, _, sut) = Create();

        await sut.GoToMapAsync("middle-earth", "eriador");

        Assert.Equal(("/middle-earth/maps/eriador", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToWikiArticleAsync_NavigatesToArticleSlugUrl()
    {
        var (nav, _, sut) = Create();

        await sut.GoToWikiArticleAsync("middle-earth", ["locations", "rivendell"]);

        Assert.Equal(("/middle-earth/wiki/locations/rivendell", false), nav.Navigations[0]);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GoToTutorialAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GoToTutorialAsync_NavigatesToTutorialsUrl()
    {
        var (nav, _, sut) = Create();

        await sut.GoToTutorialAsync("getting-started");

        Assert.Equal(("/tutorials/getting-started", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToTutorialAsync_WithReplace_PassesReplaceFlag()
    {
        var (nav, _, sut) = Create();

        await sut.GoToTutorialAsync("getting-started", replace: true);

        Assert.Equal(("/tutorials/getting-started", true), nav.Navigations[0]);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GoToArticleAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GoToArticleAsync_Tutorial_NavigatesToTutorialsRoute()
    {
        var (nav, _, sut) = Create();
        var article = new ArticleDto { Slug = "intro", Type = ArticleType.Tutorial, Breadcrumbs = [] };

        await sut.GoToArticleAsync(article);

        Assert.Equal(("/tutorials/intro", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArticleAsync_SessionNote_WithFullBreadcrumbs_NavigatesToFiveSegmentUrl()
    {
        var (nav, _, sut) = Create();
        var article = new ArticleDto
        {
            Slug = "note-1",
            Type = ArticleType.SessionNote,
            Breadcrumbs =
            [
                new BreadcrumbDto { Slug = "me", IsWorld = true },
                new BreadcrumbDto { Slug = "wotr" },
                new BreadcrumbDto { Slug = "fellowship" },
                new BreadcrumbDto { Slug = "session-1" },
                new BreadcrumbDto { Slug = "note-1" }
            ]
        };

        await sut.GoToArticleAsync(article);

        Assert.Equal(("/me/wotr/fellowship/session-1/note-1", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArticleAsync_SessionNote_MissingBreadcrumbs_NavigatesToDashboard()
    {
        var (nav, _, sut) = Create();
        var article = new ArticleDto { Slug = "orphan", Type = ArticleType.SessionNote, Breadcrumbs = [] };

        await sut.GoToArticleAsync(article);

        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArticleAsync_SessionNote_NullBreadcrumbs_NavigatesToDashboard()
    {
        var (nav, _, sut) = Create();
        var article = new ArticleDto { Slug = "orphan", Type = ArticleType.SessionNote, Breadcrumbs = null };

        await sut.GoToArticleAsync(article);

        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArticleAsync_WikiArticle_NavigatesToWikiUrl()
    {
        var (nav, _, sut) = Create();
        var article = new ArticleDto
        {
            Slug = "rivendell",
            Type = ArticleType.WikiArticle,
            WorldSlug = "middle-earth",
            Breadcrumbs =
            [
                new BreadcrumbDto { Slug = "middle-earth", IsWorld = true },
                new BreadcrumbDto { Slug = "rivendell" }
            ]
        };

        await sut.GoToArticleAsync(article);

        Assert.Equal(("/middle-earth/wiki/rivendell", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArticleAsync_WikiArticle_MissingWorldSlug_FallsBackToBreadcrumb()
    {
        var (nav, _, sut) = Create();
        var article = new ArticleDto
        {
            Slug = "rivendell",
            Type = ArticleType.WikiArticle,
            WorldSlug = string.Empty,
            Breadcrumbs =
            [
                new BreadcrumbDto { Slug = "middle-earth", IsWorld = true },
                new BreadcrumbDto { Slug = "rivendell" }
            ]
        };

        await sut.GoToArticleAsync(article);

        Assert.Equal(("/middle-earth/wiki/rivendell", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArticleAsync_WikiArticle_MissingWorldSlugAndNoBreadcrumbs_NavigatesToDashboard()
    {
        var (nav, _, sut) = Create();
        var article = new ArticleDto
        {
            Slug = "lost-article",
            Type = ArticleType.WikiArticle,
            WorldSlug = string.Empty,
            Breadcrumbs = []
        };

        await sut.GoToArticleAsync(article);

        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArticleAsync_WikiArticle_NullBreadcrumbs_NavigatesToDashboard()
    {
        var (nav, _, sut) = Create();
        var article = new ArticleDto
        {
            Slug = "lost-article",
            Type = ArticleType.WikiArticle,
            WorldSlug = "some-world",
            Breadcrumbs = null
        };

        await sut.GoToArticleAsync(article);

        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToArticleAsync_DeprecatedSession_NavigatesToDashboard()
    {
        var (nav, _, sut) = Create();
        var article = new ArticleDto { Id = Guid.NewGuid(), Slug = "old-session", Type = ArticleType.Session, Breadcrumbs = [] };

        await sut.GoToArticleAsync(article);

        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GoToSearchResultAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GoToSearchResultAsync_Tutorial_NavigatesToTutorialsRoute()
    {
        var (nav, _, sut) = Create();
        var result = new ArticleSearchResultDto
        {
            Type = ArticleType.Tutorial,
            ArticleSlugChain = ["getting-started"]
        };

        await sut.GoToSearchResultAsync(result);

        Assert.Equal(("/tutorials/getting-started", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToSearchResultAsync_Tutorial_EmptySlugChain_NavigatesToDashboard()
    {
        var (nav, _, sut) = Create();
        var result = new ArticleSearchResultDto { Type = ArticleType.Tutorial, ArticleSlugChain = [] };

        await sut.GoToSearchResultAsync(result);

        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToSearchResultAsync_SessionNote_NavigatesToFiveSegmentUrl()
    {
        var (nav, _, sut) = Create();
        var result = new ArticleSearchResultDto
        {
            Type = ArticleType.SessionNote,
            WorldSlug = "me",
            CampaignSlug = "wotr",
            ArcSlug = "fellowship",
            SessionSlug = "session-1",
            ArticleSlugChain = ["note-1"]
        };

        await sut.GoToSearchResultAsync(result);

        Assert.Equal(("/me/wotr/fellowship/session-1/note-1", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToSearchResultAsync_SessionNote_MissingContext_NavigatesToDashboard()
    {
        var (nav, _, sut) = Create();
        var result = new ArticleSearchResultDto
        {
            Type = ArticleType.SessionNote,
            WorldSlug = "me",
            CampaignSlug = null,
            ArcSlug = null,
            SessionSlug = null,
            ArticleSlugChain = []
        };

        await sut.GoToSearchResultAsync(result);

        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToSearchResultAsync_WikiArticle_NavigatesToWikiUrl()
    {
        var (nav, _, sut) = Create();
        var result = new ArticleSearchResultDto
        {
            Type = ArticleType.WikiArticle,
            WorldSlug = "middle-earth",
            ArticleSlugChain = ["locations", "rivendell"]
        };

        await sut.GoToSearchResultAsync(result);

        Assert.Equal(("/middle-earth/wiki/locations/rivendell", false), nav.Navigations[0]);
    }

    [Fact]
    public async Task GoToSearchResultAsync_WikiArticle_MissingWorldSlug_NavigatesToDashboard()
    {
        var (nav, _, sut) = Create();
        var result = new ArticleSearchResultDto
        {
            Type = ArticleType.WikiArticle,
            WorldSlug = string.Empty,
            ArticleSlugChain = ["rivendell"]
        };

        await sut.GoToSearchResultAsync(result);

        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }
}
