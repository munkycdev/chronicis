using Chronicis.Client.Abstractions;
using Chronicis.Client.Infrastructure;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.Admin;
using Microsoft.Extensions.Options;

namespace Chronicis.Client.Extensions;

/// <summary>
/// Extension methods for registering application services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Adds all Chronicis application services including API services,
    /// state services, and domain services.
    /// </summary>
    public static IServiceCollection AddChronicisApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SysAdmin checker — reads from the "SysAdmin" config section in wwwroot/appsettings.json
        services.Configure<SysAdminOptions>(configuration.GetSection("SysAdmin"));
        services.AddSingleton<ISysAdminChecker>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SysAdminOptions>>().Value;
            return new SysAdminChecker(options);
        });

        // API Services - use the authenticated "ChronicisApi" client
        services.AddChronicisApiService<IArticleApiService, ArticleApiService>();
        services.AddChronicisApiService<ISearchApiService, SearchApiService>();
        services.AddChronicisApiService<IAISummaryApiService, AISummaryApiService>();
        services.AddChronicisApiService<IWorldApiService, WorldApiService>();
        services.AddChronicisApiService<ICampaignApiService, CampaignApiService>();
        services.AddChronicisApiService<IArcApiService, ArcApiService>();
        services.AddChronicisApiService<ILinkApiService, LinkApiService>();
        services.AddChronicisApiService<IArticleExternalLinkApiService, ArticleExternalLinkApiService>();
        services.AddChronicisApiService<IExternalLinkApiService, ExternalLinkApiService>();
        services.AddChronicisApiService<IUserApiService, UserApiService>();
        services.AddChronicisApiService<ICharacterApiService, CharacterApiService>();
        services.AddChronicisApiService<IDashboardApiService, DashboardApiService>();
        services.AddChronicisApiService<IResourceProviderApiService, ResourceProviderApiService>();
        services.AddChronicisApiService<IAdminApiService, AdminApiService>();

        // API services with special dependencies
        services.AddChronicisApiServiceWithSnackbar<IQuestApiService, QuestApiService>();
        services.AddChronicisApiServiceWithJSRuntime<IExportApiService, ExportApiService>();

        // Public API service (uses unauthenticated client)
        services.AddScoped<IPublicApiService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<PublicApiService>>();
            return new PublicApiService(factory.CreateClient("ChronicisPublicApi"), logger);
        });

        // Health Status API service (uses unauthenticated client since health endpoints are public)
        services.AddScoped<IHealthStatusApiService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<HealthStatusApiService>>();
            return new HealthStatusApiService(factory.CreateClient("ChronicisPublicApi"), logger);
        });

        // UI Infrastructure abstractions — decouple ViewModels from MudBlazor/JS/NavigationManager
        services.AddScoped<IAppNavigator, AppNavigator>();
        services.AddScoped<IUserNotifier, UserNotifier>();
        services.AddScoped<IConfirmationService, ConfirmationService>();
        services.AddScoped<IPageTitleService, PageTitleService>();

        // ViewModels
        services.AddTransient<SearchViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CampaignDetailViewModel>();
        services.AddTransient<ArcDetailViewModel>();

        // State & coordination services
        services.AddScoped<ITreeStateService, TreeStateService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAppContextService, AppContextService>();
        services.AddScoped<IMetadataDrawerService, MetadataDrawerService>();
        services.AddScoped<IQuestDrawerService, QuestDrawerService>();
        services.AddScoped<IKeyboardShortcutService, KeyboardShortcutService>();

        // Domain services
        services.AddScoped<IArticleCacheService, ArticleCacheService>();
        services.AddScoped<IWikiLinkService, WikiLinkService>();
        services.AddScoped<IWikiLinkAutocompleteService, WikiLinkAutocompleteService>();
        services.AddScoped<IBreadcrumbService, BreadcrumbService>();
        services.AddScoped<IMarkdownService, MarkdownService>();

        // Render definition service (loads from wwwroot static assets via base URI)
        services.AddScoped<IRenderDefinitionService>(sp =>
        {
            var navigationManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
            var http = new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
            var logger = sp.GetRequiredService<ILogger<RenderDefinitionService>>();
            return new RenderDefinitionService(http, logger);
        });

        return services;
    }
}
