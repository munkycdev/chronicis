using Chronicis.Api.Controllers;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Api.Services.Articles;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ControllersCoverageSmokeTests
{
    [Fact]
    public async Task ArcsController_GetArc_ReturnsOk()
    {
        var user = CreateCurrentUserService();
        var service = Substitute.For<IArcService>();
        var arcId = Guid.NewGuid();
        service.GetArcAsync(arcId, Arg.Any<Guid>()).Returns(new ArcDto { Id = arcId, Name = "Arc" });
        var sut = new ArcsController(service, user, NullLogger<ArcsController>.Instance);

        var result = await sut.GetArc(arcId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CampaignArcsController_GetArcsByCampaign_ReturnsOk()
    {
        var user = CreateCurrentUserService();
        var service = Substitute.For<IArcService>();
        service.GetArcsByCampaignAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns([new ArcDto { Id = Guid.NewGuid(), Name = "Arc" }]);
        var sut = new CampaignArcsController(service, user, NullLogger<CampaignArcsController>.Instance);

        var result = await sut.GetArcsByCampaign(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CampaignsController_GetCampaign_ReturnsOk()
    {
        var user = CreateCurrentUserService();
        var service = Substitute.For<ICampaignService>();
        var campaignId = Guid.NewGuid();
        service.GetCampaignAsync(campaignId, Arg.Any<Guid>())
            .Returns(new CampaignDetailDto { Id = campaignId, Name = "Campaign" });
        var sut = new CampaignsController(service, user, NullLogger<CampaignsController>.Instance);

        var result = await sut.GetCampaign(campaignId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task WorldActiveContextController_GetActiveContext_ReturnsOk()
    {
        var user = CreateCurrentUserService();
        var service = Substitute.For<ICampaignService>();
        service.GetActiveContextAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new ActiveContextDto());
        var sut = new WorldActiveContextController(service, user, NullLogger<WorldActiveContextController>.Instance);

        var result = await sut.GetActiveContext(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CharactersController_GetClaimStatus_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateDbContext();
        var sut = new CharactersController(db, CreateCurrentUserService(), NullLogger<CharactersController>.Instance);

        var result = await sut.GetClaimStatus(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task DashboardController_GetDashboard_ReturnsOk()
    {
        using var db = CreateDbContext();
        var promptService = Substitute.For<IPromptService>();
        promptService.GeneratePrompts(Arg.Any<DashboardDto>()).Returns([]);
        var sut = new DashboardController(
            db,
            promptService,
            CreateCurrentUserService(),
            NullLogger<DashboardController>.Instance);

        var result = await sut.GetDashboard();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task ArticleSummaryController_GetSummary_ReturnsNotFound_WhenNoAccess()
    {
        using var db = CreateDbContext();
        var summaryService = Substitute.For<ISummaryService>();
        var sut = new ArticleSummaryController(
            summaryService,
            db,
            CreateCurrentUserService(),
            NullLogger<ArticleSummaryController>.Instance);

        var result = await sut.GetSummary(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CampaignSummaryController_GetSummary_ReturnsNotFound_WhenNoAccess()
    {
        using var db = CreateDbContext();
        var summaryService = Substitute.For<ISummaryService>();
        var sut = new CampaignSummaryController(
            summaryService,
            db,
            CreateCurrentUserService(),
            NullLogger<CampaignSummaryController>.Instance);

        var result = await sut.GetSummary(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ArcSummaryController_GetSummary_ReturnsNotFound_WhenNoAccess()
    {
        using var db = CreateDbContext();
        var summaryService = Substitute.For<ISummaryService>();
        var sut = new ArcSummaryController(
            summaryService,
            db,
            CreateCurrentUserService(),
            NullLogger<ArcSummaryController>.Instance);

        var result = await sut.GetSummary(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void HealthController_GetHealth_ReturnsOk()
    {
        using var db = CreateDbContext();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var healthService = Substitute.For<ISystemHealthService>();
        var sut = new HealthController(db, NullLogger<HealthController>.Instance, config, healthService);

        var result = sut.GetHealth();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ImagesController_GetImage_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateDbContext();
        var blob = Substitute.For<IBlobStorageService>();
        var sut = new ImagesController(
            db,
            blob,
            CreateCurrentUserService(),
            NullLogger<ImagesController>.Instance);

        var result = await sut.GetImage(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PublicController_GetPublicWorld_EmptySlug_ReturnsBadRequest()
    {
        var service = Substitute.For<IPublicWorldService>();
        var sut = new PublicController(service, NullLogger<PublicController>.Instance);

        var result = await sut.GetPublicWorld("");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task QuestsController_GetQuest_ReturnsOk_OnSuccess()
    {
        var user = CreateCurrentUserService();
        var questService = Substitute.For<IQuestService>();
        var questId = Guid.NewGuid();
        questService.GetQuestAsync(questId, Arg.Any<Guid>())
            .Returns(ServiceResult<QuestDto>.Success(new QuestDto { Id = questId, Title = "Quest" }));
        var sut = new QuestsController(questService, user, NullLogger<QuestsController>.Instance);

        var result = await sut.GetQuest(questId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task QuestUpdatesController_GetQuestUpdates_ReturnsOk_OnSuccess()
    {
        var user = CreateCurrentUserService();
        var service = Substitute.For<IQuestUpdateService>();
        service.GetQuestUpdatesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), 0, 20)
            .Returns(ServiceResult<PagedResult<QuestUpdateEntryDto>>.Success(new PagedResult<QuestUpdateEntryDto>
            {
                Items = [new QuestUpdateEntryDto { Id = Guid.NewGuid(), Body = "Update" }],
                TotalCount = 1,
                Skip = 0,
                Take = 20
            }));
        var sut = new QuestUpdatesController(service, user, NullLogger<QuestUpdatesController>.Instance);

        var result = await sut.GetQuestUpdates(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task ResourceProvidersController_GetWorldProviders_ReturnsOk()
    {
        var user = CreateCurrentUserService();
        var service = Substitute.For<IResourceProviderService>();
        service.GetWorldProvidersAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(
            [
                (new ResourceProvider
                {
                    Code = "srd",
                    Name = "SRD",
                    Description = "desc",
                    DocumentationLink = "https://example.test/docs",
                    License = "OGL"
                }, true)
            ]);

        var sut = new ResourceProvidersController(service, user, NullLogger<ResourceProvidersController>.Instance);

        var result = await sut.GetWorldProviders(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SearchController_ShortQuery_ReturnsEmptyPayload()
    {
        using var db = CreateDbContext();
        var user = CreateCurrentUserService();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        var sut = new SearchController(db, user, NullLogger<SearchController>.Instance, hierarchy);

        var result = await sut.Search("a");
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<GlobalSearchResultsDto>(ok.Value);
        Assert.Equal(0, payload.TotalResults);
    }

    [Fact]
    public async Task SummaryController_GetTemplates_ReturnsOk()
    {
        var user = CreateCurrentUserService();
        var service = Substitute.For<ISummaryService>();
        service.GetTemplatesAsync().Returns([]);
        var sut = new SummaryController(service, user, NullLogger<SummaryController>.Instance);

        var result = await sut.GetTemplates();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UsersController_GetCurrentUserProfile_ReturnsOk()
    {
        var userService = Substitute.For<IUserService>();
        var currentUser = CreateCurrentUserService();
        var knownUser = await currentUser.GetRequiredUserAsync();
        userService.GetUserProfileAsync(knownUser.Id).Returns(new UserProfileDto
        {
            Id = knownUser.Id,
            DisplayName = knownUser.DisplayName,
            Email = knownUser.Email
        });
        var sut = new UsersController(userService, currentUser, NullLogger<UsersController>.Instance);

        var result = await sut.GetCurrentUserProfile();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task WorldDocumentsController_RequestDocumentUpload_NullRequest_ReturnsBadRequest()
    {
        var service = Substitute.For<IWorldDocumentService>();
        var sut = new WorldDocumentsController(
            service,
            CreateCurrentUserService(),
            NullLogger<WorldDocumentsController>.Instance);

        var result = await sut.RequestDocumentUpload(Guid.NewGuid(), null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task WorldLinksController_GetWorldLinks_ReturnsNotFound_WhenNoWorldAccess()
    {
        using var db = CreateDbContext();
        var sut = new WorldLinksController(db, CreateCurrentUserService(), NullLogger<WorldLinksController>.Instance);

        var result = await sut.GetWorldLinks(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task WorldsController_GetWorlds_ReturnsOk()
    {
        using var db = CreateDbContext();
        var user = CreateCurrentUserService();
        var worldService = Substitute.For<IWorldService>();
        worldService.GetUserWorldsAsync(Arg.Any<Guid>()).Returns([]);
        var sut = new WorldsController(
            worldService,
            Substitute.For<IWorldMembershipService>(),
            Substitute.For<IWorldInvitationService>(),
            Substitute.For<IWorldPublicSharingService>(),
            Substitute.For<IExportService>(),
            Substitute.For<IArticleHierarchyService>(),
            db,
            user,
            NullLogger<WorldsController>.Instance);

        var result = await sut.GetWorlds();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task ArticlesController_GetRootArticles_ReturnsOk()
    {
        using var db = CreateDbContext();
        var user = CreateCurrentUserService();
        var articleService = Substitute.For<IArticleService>();
        articleService.GetRootArticlesAsync(Arg.Any<Guid>(), Arg.Any<Guid?>()).Returns([]);

        var sut = new ArticlesController(
            articleService,
            Substitute.For<IArticleValidationService>(),
            Substitute.For<ILinkSyncService>(),
            Substitute.For<IAutoLinkService>(),
            Substitute.For<IArticleExternalLinkService>(),
            Substitute.For<IArticleHierarchyService>(),
            db,
            user,
            Substitute.For<IWorldDocumentService>(),
            NullLogger<ArticlesController>.Instance);

        var result = await sut.GetRootArticles(null);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    private static ChronicisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase($"controllers-coverage-{Guid.NewGuid()}")
            .Options;
        return new ChronicisDbContext(options);
    }

    private static ICurrentUserService CreateCurrentUserService()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test User",
            Email = "test@example.com",
            Auth0UserId = "auth0|test-user"
        };

        var service = Substitute.For<ICurrentUserService>();
        service.GetRequiredUserAsync().Returns(user);
        service.GetCurrentUserAsync().Returns(user);
        return service;
    }
}
