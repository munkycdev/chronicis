using System.Reflection;
using Blazored.LocalStorage;
using Bunit;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.Articles;
using Chronicis.Client.Components.Characters;
using Chronicis.Client.Components.Drawers;
using Chronicis.Client.Components.Maps;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Articles;

public class ArticleDetailMapFeatureReferenceTests : MudBlazorTestContext
{
    private sealed record Deps(
        ArticleDetailViewModel ViewModel,
        IArticleApiService ArticleApi,
        IMapApiService MapApi,
        ILinkApiService LinkApi,
        IExternalLinkApiService ExternalLinkApi,
        IAppContextService AppContext);

    private Deps CreateDeps()
    {
        var articleApi = Substitute.For<IArticleApiService>();
        var linkApi = Substitute.For<ILinkApiService>();
        var mapApi = Substitute.For<IMapApiService>();
        var wikiLinkService = Substitute.For<IWikiLinkService>();
        var summaryApi = Substitute.For<IAISummaryApiService>();
        var markdown = Substitute.For<IMarkdownService>();
        var treeState = Substitute.For<ITreeStateService>();
        var appContext = Substitute.For<IAppContextService>();
        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        var articleCache = Substitute.For<IArticleCacheService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var titleService = Substitute.For<IPageTitleService>();
        var externalLinkApi = Substitute.For<IExternalLinkApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var drawerCoordinator = new DrawerCoordinator();

        var viewModel = new ArticleDetailViewModel(
            articleApi,
            linkApi,
            treeState,
            breadcrumbs,
            appContext,
            articleCache,
            navigator,
            notifier,
            titleService,
            Substitute.For<ILogger<ArticleDetailViewModel>>());

        Services.AddSingleton(articleApi);
        Services.AddSingleton(linkApi);
        Services.AddSingleton(externalLinkApi);
        Services.AddSingleton(mapApi);
        Services.AddSingleton(wikiLinkService);
        Services.AddSingleton(Substitute.For<IWikiLinkCommitService>());
        Services.AddSingleton(summaryApi);
        Services.AddSingleton(markdown);
        Services.AddSingleton(treeState);
        Services.AddSingleton(appContext);
        Services.AddSingleton(breadcrumbs);
        Services.AddSingleton(Substitute.For<ILocalStorageService>());
        Services.AddSingleton(Substitute.For<IWorldApiService>());
        Services.AddSingleton(articleCache);
        Services.AddSingleton<IDrawerCoordinator>(drawerCoordinator);
        Services.AddSingleton(Substitute.For<IKeyboardShortcutService>());
        Services.AddSingleton(Substitute.For<ILogger<ArticleDetail>>());
        Services.AddSingleton(viewModel);

        ComponentFactories.AddStub<LoadingSkeleton>();
        ComponentFactories.AddStub<EmptyState>();
        ComponentFactories.AddStub<ArticleHeader>();
        ComponentFactories.AddStub<CharacterClaimButton>();
        ComponentFactories.AddStub<AISummarySection>();
        ComponentFactories.AddStub<ArticleActionBar>();
        ComponentFactories.AddStub<DrawerHost>();
        ComponentFactories.AddStub<ExternalLinkDetailPanel>();
        ComponentFactories.AddStub<ArticleDetailWikiLinkAutocomplete>();
        ComponentFactories.AddStub<SessionMapViewerModal>();

        return new Deps(viewModel, articleApi, mapApi, linkApi, externalLinkApi, appContext);
    }

    [Fact]
    public async Task SessionNoteAutocomplete_MapPathFeatureSuggestions_AndInsertsFeatureChip()
    {
        var deps = CreateDeps();
        var worldId = Guid.NewGuid();
        var mapId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        deps.AppContext.CurrentWorldId.Returns((Guid?)worldId);
        deps.MapApi.GetMapAutocompleteAsync(worldId, "Roshar").Returns([
            new MapAutocompleteDto
            {
                MapId = mapId,
                Name = "Roshar"
            }
        ]);
        deps.MapApi.GetMapFeatureAutocompleteAsync(worldId, mapId, "rav").Returns([
            new MapFeatureAutocompleteDto
            {
                MapFeatureId = featureId,
                MapId = mapId,
                MapName = "Roshar",
                DisplayText = "Ravinia"
            }
        ]);

        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Title = "Session 3",
            Body = "<p></p>",
            Type = ArticleType.SessionNote
        };
        await deps.ViewModel.HydrateArticleAsync(article);

        var cut = RenderComponent<ArticleDetail>();
        SetField(cut.Instance, "_article", article);
        SetField(cut.Instance, "_editTitle", article.Title);
        SetField(cut.Instance, "_editBody", article.Body);

        await cut.InvokeAsync(() => cut.Instance.OnAutocompleteTriggered("maps/Roshar/rav", 10d, 20d));

        var suggestions = Assert.IsType<List<Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem>>(GetField(cut.Instance, "_autocompleteSuggestions"));
        var suggestion = Assert.Single(suggestions);
        Assert.True(suggestion.IsMapFeature);
        Assert.Equal(featureId, suggestion.MapFeatureId);
        Assert.Equal(mapId, suggestion.MapId);
        Assert.Equal("Roshar", suggestion.SecondaryText);

        JSInterop.SetupVoid("insertMapFeatureLinkToken", _ => true).SetVoidResult();
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "OnAutocompleteSelect", suggestion));

        var invocation = Assert.Single(JSInterop.Invocations.Where(call => call.Identifier == "insertMapFeatureLinkToken"));
        Assert.Equal($"tiptap-editor-{article.Id}", invocation.Arguments[0]?.ToString());
        Assert.Equal(featureId.ToString(), invocation.Arguments[1]?.ToString());
        Assert.Equal(mapId.ToString(), invocation.Arguments[2]?.ToString());
        Assert.Equal("Ravinia", invocation.Arguments[3]?.ToString());
        Assert.Equal("Roshar", invocation.Arguments[4]?.ToString());
    }

    [Fact]
    public async Task SessionNoteSave_PreservesMapFeatureChipMarkup()
    {
        var deps = CreateDeps();
        var worldId = Guid.NewGuid();
        deps.AppContext.CurrentWorldId.Returns((Guid?)worldId);
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Title = "Session 8",
            Body = "<p></p>",
            Type = ArticleType.SessionNote
        };
        await deps.ViewModel.HydrateArticleAsync(article);

        ArticleUpdateDto? savedDto = null;
        deps.ArticleApi.UpdateArticleAsync(article.Id, Arg.Any<ArticleUpdateDto>())
            .Returns(callInfo =>
            {
                savedDto = callInfo.ArgAt<ArticleUpdateDto>(1);
                return Task.FromResult<ArticleDto?>(article);
            });

        var cut = RenderComponent<ArticleDetail>();
        SetField(cut.Instance, "_article", article);
        SetField(cut.Instance, "_editTitle", article.Title);
        SetField(cut.Instance, "_editBody", article.Body);

        var chipMarkup =
            $"<p><span data-type=\"map-feature-link\" data-feature-id=\"{Guid.NewGuid()}\" data-map-id=\"{Guid.NewGuid()}\" data-display=\"Blackroot Ford\" data-map-name=\"Ambria\"></span></p>";
        cut.Instance.OnEditorUpdate(chipMarkup);

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "SaveArticle"));

        Assert.NotNull(savedDto);
        Assert.Equal(chipMarkup, savedDto!.Body);
    }

    [Fact]
    public async Task MapFeatureChipClick_OpensModal_WithFeatureTarget()
    {
        var deps = CreateDeps();
        var worldId = Guid.NewGuid();
        deps.AppContext.CurrentWorldId.Returns((Guid?)worldId);
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            Title = "Session 11",
            Body = "<p></p>",
            Type = ArticleType.SessionNote
        };
        await deps.ViewModel.HydrateArticleAsync(article);

        var cut = RenderComponent<ArticleDetail>();
        SetField(cut.Instance, "_article", article);

        var mapId = Guid.NewGuid();
        var featureId = Guid.NewGuid();
        await cut.InvokeAsync(() => cut.Instance.OnMapFeatureChipClicked(mapId.ToString(), featureId.ToString(), "  Ambria  "));

        Assert.True((bool)GetField(cut.Instance, "_isMapModalOpen")!);
        Assert.Equal(mapId, (Guid)GetField(cut.Instance, "_selectedMapId")!);
        Assert.Equal(featureId, (Guid?)GetField(cut.Instance, "_selectedMapFeatureId"));
        Assert.Equal("Ambria", GetField(cut.Instance, "_selectedMapName"));
    }

    private static object? GetField(object instance, string name)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);

    private static void SetField(object instance, string name, object? value)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);

    private static async Task InvokePrivateTask(object instance, string methodName, params object?[] args)
    {
        var result = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, args);
        if (result is Task task)
        {
            await task;
        }
    }
}
