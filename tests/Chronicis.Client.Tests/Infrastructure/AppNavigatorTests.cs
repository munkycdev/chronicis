using Chronicis.Client.Infrastructure;
using Microsoft.AspNetCore.Components;
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

    [Fact]
    public void BaseUri_ReturnsNavigationManagerBaseUri()
    {
        var nav = new TestNavigationManager(baseUri: "https://chronicis.app/", currentUri: "https://chronicis.app/");
        var sut = new AppNavigator(nav);

        Assert.Equal("https://chronicis.app/", sut.BaseUri);
    }

    [Fact]
    public void Uri_ReturnsNavigationManagerUri()
    {
        var nav = new TestNavigationManager(
            baseUri: "https://chronicis.app/",
            currentUri: "https://chronicis.app/world/123");
        var sut = new AppNavigator(nav);

        Assert.Equal("https://chronicis.app/world/123", sut.Uri);
    }

    [Fact]
    public void NavigateTo_WithDefaultReplace_NavigatesWithReplacefalse()
    {
        var nav = new TestNavigationManager();
        var sut = new AppNavigator(nav);

        sut.NavigateTo("/dashboard");

        Assert.Single(nav.Navigations);
        Assert.Equal(("/dashboard", false), nav.Navigations[0]);
    }

    [Fact]
    public void NavigateTo_WithReplaceTrue_NavigatesWithReplaceTrue()
    {
        var nav = new TestNavigationManager();
        var sut = new AppNavigator(nav);

        sut.NavigateTo("/dashboard", replace: true);

        Assert.Single(nav.Navigations);
        Assert.Equal(("/dashboard", true), nav.Navigations[0]);
    }
}
