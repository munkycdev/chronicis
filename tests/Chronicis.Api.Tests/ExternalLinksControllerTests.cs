using Chronicis.Api.Controllers;
using Chronicis.Api.Services.ExternalLinks;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ExternalLinksControllerTests
{
    [Fact]
    public async Task GetSuggestions_EmptySource_ReturnsOkWithEmptyList()
    {
        var service = Substitute.For<IExternalLinkService>();
        var sut = new ExternalLinksController(service, NullLogger<ExternalLinksController>.Instance);

        var result = await sut.GetSuggestions(Guid.NewGuid(), "", "fire", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<List<ExternalLinkSuggestionDto>>(ok.Value);
        Assert.Empty(payload);
        await service.DidNotReceiveWithAnyArgs().GetSuggestionsAsync(default, default!, default!, default);
    }

    [Fact]
    public async Task GetSuggestions_MapsServiceResult()
    {
        var service = Substitute.For<IExternalLinkService>();
        service.GetSuggestionsAsync(Arg.Any<Guid?>(), "srd", "acid", Arg.Any<CancellationToken>())
            .Returns(
            [
                new ExternalLinkSuggestion
                {
                    Source = "srd",
                    Id = "/api/spells/acid-arrow",
                    Title = "Acid Arrow",
                    Subtitle = "Spell",
                    Category = "spells",
                    Icon = "spark",
                    Href = "https://example.test"
                }
            ]);

        var sut = new ExternalLinksController(service, NullLogger<ExternalLinksController>.Instance);

        var result = await sut.GetSuggestions(null, "srd", "acid", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<List<ExternalLinkSuggestionDto>>(ok.Value);
        var dto = Assert.Single(payload);
        Assert.Equal("srd", dto.Source);
        Assert.Equal("/api/spells/acid-arrow", dto.Id);
        Assert.Equal("Acid Arrow", dto.Title);
        Assert.Equal("Spell", dto.Subtitle);
        Assert.Equal("spells", dto.Category);
        Assert.Equal("spark", dto.Icon);
        Assert.Equal("https://example.test", dto.Href);
    }

    [Fact]
    public async Task GetSuggestions_NullQuery_PassesEmptyQueryToService()
    {
        var service = Substitute.For<IExternalLinkService>();
        service.GetSuggestionsAsync(Arg.Any<Guid?>(), "srd", "", Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = new ExternalLinksController(service, NullLogger<ExternalLinksController>.Instance);

        var result = await sut.GetSuggestions(Guid.NewGuid(), "srd", null, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<List<ExternalLinkSuggestionDto>>(ok.Value);
        Assert.Empty(payload);
        await service.Received(1).GetSuggestionsAsync(Arg.Any<Guid?>(), "srd", "", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetContent_MissingSourceOrId_ReturnsBadRequest()
    {
        var service = Substitute.For<IExternalLinkService>();
        var sut = new ExternalLinksController(service, NullLogger<ExternalLinksController>.Instance);

        var result = await sut.GetContent("srd", "", CancellationToken.None);
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ExternalLinkErrorDto>(bad.Value);
        Assert.Equal("Source and id are required", error.Message);
    }

    [Fact]
    public async Task GetContent_MissingSource_ReturnsBadRequest_WithoutCallingService()
    {
        var service = Substitute.For<IExternalLinkService>();
        var sut = new ExternalLinksController(service, NullLogger<ExternalLinksController>.Instance);

        var result = await sut.GetContent("", "/api/spells/acid-arrow", CancellationToken.None);
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ExternalLinkErrorDto>(bad.Value);
        Assert.Equal("Source and id are required", error.Message);
        await service.DidNotReceiveWithAnyArgs().GetContentAsync(default!, default!, default);
    }

    [Fact]
    public async Task GetContent_NotFound_Returns404()
    {
        var service = Substitute.For<IExternalLinkService>();
        service.GetContentAsync("srd", "/api/spells/acid-arrow", Arg.Any<CancellationToken>())
            .Returns((ExternalLinkContent?)null);
        var sut = new ExternalLinksController(service, NullLogger<ExternalLinksController>.Instance);

        var result = await sut.GetContent("srd", "/api/spells/acid-arrow", CancellationToken.None);
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ExternalLinkErrorDto>(notFound.Value);
        Assert.Equal("Content not found", error.Message);
    }

    [Fact]
    public async Task GetContent_MapsServiceResult()
    {
        var service = Substitute.For<IExternalLinkService>();
        service.GetContentAsync("srd", "/api/spells/acid-arrow", Arg.Any<CancellationToken>())
            .Returns(new ExternalLinkContent
            {
                Source = "srd",
                Id = "/api/spells/acid-arrow",
                Title = "Acid Arrow",
                Kind = "Spell",
                Markdown = "content",
                Attribution = "attr",
                ExternalUrl = "https://example.test",
                JsonData = "{}"
            });

        var sut = new ExternalLinksController(service, NullLogger<ExternalLinksController>.Instance);

        var result = await sut.GetContent("srd", "/api/spells/acid-arrow", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ExternalLinkContentDto>(ok.Value);
        Assert.Equal("srd", payload.Source);
        Assert.Equal("/api/spells/acid-arrow", payload.Id);
        Assert.Equal("Acid Arrow", payload.Title);
        Assert.Equal("Spell", payload.Kind);
        Assert.Equal("content", payload.Markdown);
        Assert.Equal("attr", payload.Attribution);
        Assert.Equal("https://example.test", payload.ExternalUrl);
        Assert.Equal("{}", payload.JsonData);
    }
}
