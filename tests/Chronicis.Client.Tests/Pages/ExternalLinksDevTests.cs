using Bunit;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class ExternalLinksDevTests : MudBlazorTestContext
{
    [Fact]
    public void ExternalLinksDev_InitialState_ShowsEmptyMessages()
    {
        RegisterApi();

        var cut = RenderComponent<ExternalLinksDev>();

        Assert.Contains("No suggestions loaded", cut.Markup);
        Assert.Contains("No content loaded", cut.Markup);
    }

    [Fact]
    public void ExternalLinksDev_FetchSuggestions_RendersSuggestions()
    {
        var api = RegisterApi();
        api.GetSuggestionsAsync(null, "srd", "fireball", Arg.Any<CancellationToken>()).Returns(
        [
            new ExternalLinkSuggestionDto { Title = "Fireball", Id = "/api/2014/spells/fireball" }
        ]);

        var cut = RenderComponent<ExternalLinksDev>();
        cut.FindAll("button")[0].Click();

        cut.WaitForAssertion(() => Assert.Contains("Fireball", cut.Markup));
    }

    [Fact]
    public void ExternalLinksDev_FetchContent_RendersContent()
    {
        var api = RegisterApi();
        api.GetContentAsync("srd", "spells/srd_fireball", Arg.Any<CancellationToken>()).Returns(
            new ExternalLinkContentDto { Title = "Fireball", Kind = "spell", Markdown = "A bright streak" });

        var cut = RenderComponent<ExternalLinksDev>();
        cut.FindAll("button")[1].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Fireball", cut.Markup);
            Assert.Contains("spell", cut.Markup);
            Assert.Contains("A bright streak", cut.Markup);
        });
    }

    private IExternalLinkApiService RegisterApi()
    {
        var api = Substitute.For<IExternalLinkApiService>();
        api.GetSuggestionsAsync(Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<ExternalLinkSuggestionDto>());
        Services.AddSingleton(api);
        return api;
    }
}
