using Chronicis.Client.Components.Shared;
using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class ExternalLinkDetailPanelTests : MudBlazorTestContext
{
    private readonly IRenderDefinitionService _renderDefinitionService = Substitute.For<IRenderDefinitionService>();
    private readonly IMarkdownService _markdownService = Substitute.For<IMarkdownService>();

    public ExternalLinkDetailPanelTests()
    {
        Services.AddSingleton(_renderDefinitionService);
        Services.AddSingleton(_markdownService);
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<ExternalLinkDetailPanel>>(NullLogger<ExternalLinkDetailPanel>.Instance);
    }

    [Fact]
    public void NullContent_RendersNothing()
    {
        var cut = RenderComponent<ExternalLinkDetailPanel>(p => p.Add(x => x.Content, (ExternalLinkContentDto?)null));

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void InvalidJson_FallsBackToMarkdown()
    {
        _markdownService.ToHtml("**md**").Returns("<p>md</p>");

        var cut = RenderComponent<ExternalLinkDetailPanel>(p => p.Add(x => x.Content, new ExternalLinkContentDto
        {
            Source = "srd",
            Id = "/api/spells/acid-arrow",
            JsonData = "{invalid-json",
            Markdown = "**md**"
        }));

        Assert.Contains("external-link-preview-body", cut.Markup);
        Assert.Contains("<p>md</p>", cut.Markup);
    }

    [Fact]
    public void ValidJsonAndDefinitionOverride_RendersStructuredOutput()
    {
        var definition = new RenderDefinition
        {
            CatchAll = true,
            Sections =
            [
                new RenderSection
                {
                    Label = "Basics",
                    Fields =
                    [
                        new RenderField { Path = "name", Label = "Name" },
                        new RenderField { Path = "level", Label = "Level" }
                    ]
                }
            ]
        };

        var json = "{\"fields\":{\"name\":\"Acid Arrow\",\"level\":2,\"school\":\"Evocation\"}}";
        var cut = RenderComponent<ExternalLinkDetailPanel>(p => p
            .Add(x => x.Content, new ExternalLinkContentDto
            {
                Source = "srd",
                Id = "/api/2014/spells/acid-arrow",
                JsonData = json
            })
            .Add(x => x.DefinitionOverride, definition));

        Assert.Contains("elp-structured", cut.Markup);
        Assert.Contains("Acid Arrow", cut.Markup);
        Assert.Contains("Level", cut.Markup);
        Assert.Contains("Properties", cut.Markup);
    }

    [Fact]
    public void ValidJsonWithoutOverride_UsesResolveService()
    {
        _renderDefinitionService.ResolveAsync("srd", "/api/2014/spells")
            .Returns(new RenderDefinition { CatchAll = true, Sections = [] });

        var cut = RenderComponent<ExternalLinkDetailPanel>(p => p.Add(x => x.Content, new ExternalLinkContentDto
        {
            Source = "srd",
            Id = "/api/2014/spells/acid-arrow",
            JsonData = "{\"fields\":{\"name\":\"Acid Arrow\"}}"
        }));

        _renderDefinitionService.Received(1).ResolveAsync("srd", "/api/2014/spells");
        Assert.Contains("elp-structured", cut.Markup);
    }
}
