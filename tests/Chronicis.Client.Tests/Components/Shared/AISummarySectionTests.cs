using Bunit;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class AISummarySectionTests : MudBlazorTestContext
{
    private readonly IAISummaryApiService _summaryApi = Substitute.For<IAISummaryApiService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();

    public AISummarySectionTests()
    {
        Services.AddSingleton(_summaryApi);
        Services.AddSingleton(_snackbar);

        _summaryApi.GetTemplatesAsync().Returns([
            new SummaryTemplateDto { Id = Guid.NewGuid(), Name = "Default" },
            new SummaryTemplateDto { Id = Guid.NewGuid(), Name = "Detailed" }
        ]);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _snackbar.Dispose();
        }

        base.Dispose(disposing);
    }

    private IRenderedComponent<AISummarySection> RenderSut(Action<ComponentParameterCollectionBuilder<AISummarySection>>? configure = null)
    {
        RenderComponent<MudPopoverProvider>();
        return RenderComponent<AISummarySection>(parameters =>
        {
            configure?.Invoke(parameters);
        });
    }

    [Fact]
    public void ExistingSummary_RendersSummaryContent()
    {
        var entityId = Guid.NewGuid();
        _summaryApi.GetSummaryAsync(entityId).Returns(new ArticleSummaryDto
        {
            ArticleId = entityId,
            Summary = "Existing summary",
            GeneratedAt = DateTime.UtcNow,
            TemplateName = "Default"
        });

        var cut = RenderSut(p => p
            .Add(x => x.EntityId, entityId)
            .Add(x => x.IsExpanded, true));

        cut.WaitForAssertion(() => Assert.Contains("Existing summary", cut.Markup));
    }

    [Fact]
    public void ExpandedWithoutSummary_ShowsNoSourcesMessage()
    {
        var entityId = Guid.NewGuid();
        _summaryApi.GetSummaryAsync(entityId).Returns(new ArticleSummaryDto { ArticleId = entityId, Summary = null });
        _summaryApi.GetEstimateAsync(entityId).Returns(new SummaryEstimateDto
        {
            EntityId = entityId,
            EntityType = "Article",
            SourceCount = 0,
            EstimatedCostUSD = 0.0001m
        });

        var cut = RenderSut(p => p
            .Add(x => x.EntityId, entityId)
            .Add(x => x.IsExpanded, false));

        cut.Find(".ai-summary-header").Click();

        cut.WaitForAssertion(() => Assert.Contains("has no content", cut.Markup));
    }

    [Fact]
    public void ExpandedWithEstimateSources_ShowsGenerateControls()
    {
        var entityId = Guid.NewGuid();
        _summaryApi.GetSummaryAsync(entityId).Returns(new ArticleSummaryDto { ArticleId = entityId, Summary = null });
        _summaryApi.GetEstimateAsync(entityId).Returns(new SummaryEstimateDto
        {
            EntityId = entityId,
            EntityType = "Article",
            SourceCount = 2,
            EstimatedInputTokens = 100,
            EstimatedOutputTokens = 50,
            EstimatedCostUSD = 0.001m
        });

        var cut = RenderSut(p => p
            .Add(x => x.EntityId, entityId)
            .Add(x => x.IsExpanded, false));

        cut.Find(".ai-summary-header").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Generate", cut.Markup);
            Assert.Contains("will be analyzed", cut.Markup);
        });
    }

    [Fact]
    public async Task GenerateSuccess_ShowsSuccessMessage()
    {
        var entityId = Guid.NewGuid();
        _summaryApi.GetSummaryAsync(entityId).Returns(new ArticleSummaryDto { ArticleId = entityId, Summary = null });
        _summaryApi.GetEstimateAsync(entityId).Returns(new SummaryEstimateDto
        {
            EntityId = entityId,
            EntityType = "Article",
            SourceCount = 1,
            EstimatedCostUSD = 0.001m
        });
        _summaryApi.GenerateSummaryAsync(entityId, Arg.Any<GenerateSummaryRequestDto>()).Returns(new SummaryGenerationDto
        {
            Success = true,
            Summary = "Generated summary",
            GeneratedDate = DateTime.UtcNow
        });

        var cut = RenderSut(p => p
            .Add(x => x.EntityId, entityId)
            .Add(x => x.IsExpanded, false));

        cut.Find(".ai-summary-header").Click();
        cut.WaitForAssertion(() => Assert.Contains("Generate", cut.Markup));

        await cut.Find("button.generate-button").ClickAsync(new());

        _snackbar.Received().Add("Summary generated!", Severity.Success, Arg.Any<Action<SnackbarOptions>?>());
        cut.WaitForAssertion(() => Assert.Contains("Generated summary", cut.Markup));
    }

    [Fact]
    public async Task GenerateFailure_ShowsErrorMessage()
    {
        var entityId = Guid.NewGuid();
        _summaryApi.GetSummaryAsync(entityId).Returns(new ArticleSummaryDto { ArticleId = entityId, Summary = null });
        _summaryApi.GetEstimateAsync(entityId).Returns(new SummaryEstimateDto
        {
            EntityId = entityId,
            EntityType = "Article",
            SourceCount = 1,
            EstimatedCostUSD = 0.001m
        });
        _summaryApi.GenerateSummaryAsync(entityId, Arg.Any<GenerateSummaryRequestDto>()).Returns(new SummaryGenerationDto
        {
            Success = false,
            ErrorMessage = "boom"
        });

        var cut = RenderSut(p => p
            .Add(x => x.EntityId, entityId)
            .Add(x => x.IsExpanded, false));

        cut.Find(".ai-summary-header").Click();
        cut.WaitForAssertion(() => Assert.Contains("Generate", cut.Markup));

        await cut.Find("button.generate-button").ClickAsync(new());

        _snackbar.Received().Add("boom", Severity.Error, Arg.Any<Action<SnackbarOptions>?>());
    }
}
