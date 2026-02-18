using Chronicis.Api.Controllers;
using Chronicis.Api.Services.Articles;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArticleExternalLinksControllerTests
{
    [Fact]
    public async Task GetExternalLinks_ReturnsOkPayload()
    {
        var service = Substitute.For<IArticleExternalLinkService>();
        var articleId = Guid.NewGuid();
        service.GetExternalLinksForArticleAsync(articleId)
            .Returns(
            [
                new ArticleExternalLinkDto
                {
                    Id = Guid.NewGuid(),
                    ArticleId = articleId,
                    Source = "srd",
                    ExternalId = "/api/spells/fireball",
                    DisplayTitle = "Fireball"
                }
            ]);

        var sut = new ArticleExternalLinksController(
            service,
            NullLogger<ArticleExternalLinksController>.Instance);

        var result = await sut.GetExternalLinks(articleId);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<List<ArticleExternalLinkDto>>(ok.Value);
        var first = Assert.Single(payload);
        Assert.Equal(articleId, first.ArticleId);
        Assert.Equal("srd", first.Source);
    }

    [Fact]
    public async Task GetExternalLinks_OnException_Returns500()
    {
        var service = Substitute.For<IArticleExternalLinkService>();
        var articleId = Guid.NewGuid();
        service.GetExternalLinksForArticleAsync(articleId).ThrowsAsync(new InvalidOperationException("boom"));

        var sut = new ArticleExternalLinksController(
            service,
            NullLogger<ArticleExternalLinksController>.Instance);

        var result = await sut.GetExternalLinks(articleId);
        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
        Assert.Equal("An error occurred while retrieving external links.", error.Value);
    }
}
