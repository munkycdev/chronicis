using Chronicis.Client.Services;

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
    public static IServiceCollection AddChronicisApplicationServices(this IServiceCollection services)
    {
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

        // Concrete API service (no interface)
        services.AddChronicisApiServiceConcrete<ResourceProviderApiService>();

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
