using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Articles;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;
using ArticleWikiLinkAutocompleteItem = Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem;

namespace Chronicis.Client.Tests.Components.Shared;

public class PrivateNotesTipTapEditorTests : MudBlazorTestContext
{
    private bool _providersRendered;

    private sealed record Deps(
        ILinkApiService LinkApi,
        IExternalLinkApiService ExternalLinkApi,
        IWikiLinkService WikiLinkService,
        IArticleCacheService ArticleCache,
        IAISummaryApiService SummaryApi,
        IWorldApiService WorldApi,
        IDrawerCoordinator DrawerCoordinator);

    private Deps CreateDeps()
    {
        var linkApi = Substitute.For<ILinkApiService>();
        var externalLinkApi = Substitute.For<IExternalLinkApiService>();
        var wikiLinkService = Substitute.For<IWikiLinkService>();
        var articleCache = Substitute.For<IArticleCacheService>();
        var summaryApi = Substitute.For<IAISummaryApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var drawerCoordinator = new DrawerCoordinator();

        Services.AddSingleton(linkApi);
        Services.AddSingleton(externalLinkApi);
        Services.AddSingleton(wikiLinkService);
        Services.AddSingleton(articleCache);
        Services.AddSingleton(summaryApi);
        Services.AddSingleton(worldApi);
        Services.AddSingleton<IDrawerCoordinator>(drawerCoordinator);
        Services.AddSingleton(Substitute.For<ILogger<PrivateNotesTipTapEditor>>());

        ComponentFactories.AddStub<ArticleDetailWikiLinkAutocomplete>();
        ComponentFactories.AddStub<ExternalLinkDetailPanel>();

        if (!_providersRendered)
        {
            RenderComponent<MudPopoverProvider>();
            RenderComponent<MudSnackbarProvider>();
            RenderComponent<MudDialogProvider>();
            _providersRendered = true;
        }

        return new Deps(linkApi, externalLinkApi, wikiLinkService, articleCache, summaryApi, worldApi, drawerCoordinator);
    }

    [Fact]
    public void Render_ReadOnlyWithValue_RendersMarkup()
    {
        CreateDeps();

        var cut = RenderComponent<PrivateNotesTipTapEditor>(p => p
            .Add(x => x.WorldId, Guid.NewGuid())
            .Add(x => x.ReadOnly, true)
            .Add(x => x.Value, "<p>secret</p>"));

        Assert.Contains("secret", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("No private notes yet", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_ReadOnlyEmpty_RendersEmptyState()
    {
        CreateDeps();

        var cut = RenderComponent<PrivateNotesTipTapEditor>(p => p
            .Add(x => x.WorldId, Guid.NewGuid())
            .Add(x => x.ReadOnly, true));

        Assert.Contains("No private notes yet", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PrivateNotesTipTapEditor_HandlersAndHelpers_AreCovered()
    {
        var d = CreateDeps();
        var worldId = Guid.NewGuid();
        var changedValue = string.Empty;

        d.LinkApi.GetSuggestionsAsync(worldId, "abcd").Returns(new List<LinkSuggestionDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "One", Slug = "one", DisplayPath = "One" }
        });

        d.ExternalLinkApi.GetSuggestionsAsync(worldId, "srd", "acid", Arg.Any<CancellationToken>())
            .Returns(new List<ExternalLinkSuggestionDto>
            {
                new() { Source = "srd", Id = "acid-arrow", Title = "Acid Arrow" }
            });

        var cut = RenderComponent<PrivateNotesTipTapEditor>(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.Value, "<p>initial</p>")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => changedValue = v)));

        cut.WaitForAssertion(() => Assert.Contains("chronicis-editor-container", cut.Markup, StringComparison.OrdinalIgnoreCase));

        var instance = cut.Instance;
        var fakeNav = Services.GetRequiredService<FakeNavigationManager>();

        Assert.StartsWith("private-notes-editor-", GetProperty(instance, "EditorElementId") as string, StringComparison.Ordinal);

        // Render autocomplete branch
        SetField(instance, "_showAutocomplete", true);
        cut.Render();
        Assert.NotNull(cut.FindComponent<Stub<ArticleDetailWikiLinkAutocomplete>>());

        // Parameter sync paths: editable update then readonly destroy path
        cut.SetParametersAndRender(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.Value, "<p>changed</p>")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => changedValue = v)));

        cut.SetParametersAndRender(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.ReadOnly, true)
            .Add(x => x.Value, "<p>readonly</p>")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => changedValue = v)));

        Assert.Contains("readonly", cut.Markup, StringComparison.OrdinalIgnoreCase);

        // Return to editable mode for handler coverage.
        cut.SetParametersAndRender(p => p
            .Add(x => x.WorldId, worldId)
            .Add(x => x.ReadOnly, false)
            .Add(x => x.Value, "<p>initial</p>")
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => changedValue = v)));

        // Toolbar image insert guard + success path
        await InvokePrivateTask(instance, "InsertImageFromToolbarAsync");
        SetField(instance, "_editorInitialized", true);
        await InvokePrivateTask(instance, "InsertImageFromToolbarAsync");

        // OnEditorUpdate guards and callback path
        SetField(instance, "_suppressEditorUpdates", true);
        await instance.OnEditorUpdate("<p>ignored</p>");
        SetField(instance, "_suppressEditorUpdates", false);
        SetField(instance, "_editorValue", "<p>same</p>");
        await instance.OnEditorUpdate("<p>same</p>");
        await instance.OnEditorUpdate("<p>new</p>");
        Assert.Equal("<p>new</p>", changedValue);

        // Autocomplete trigger: short internal query (clears suggestions)
        await instance.OnAutocompleteTriggered("ab", 1d, 2d);
        Assert.True((bool)GetField(instance, "_showAutocomplete")!);

        // Autocomplete trigger: external query + success
        await instance.OnAutocompleteTriggered("srd/acid", 3d, 4d);
        Assert.True((bool)GetField(instance, "_autocompleteIsExternalQuery")!);

        // Autocomplete trigger: internal query + success
        await instance.OnAutocompleteTriggered("abcd", 5d, 6d);
        Assert.False((bool)GetField(instance, "_autocompleteIsExternalQuery")!);

        // Autocomplete trigger: exception path
        d.LinkApi.GetSuggestionsAsync(worldId, "boom")
            .Returns(Task.FromException<List<LinkSuggestionDto>>(new Exception("autocomplete failed")));
        await instance.OnAutocompleteTriggered("boom", 0d, 0d);

        await instance.OnAutocompleteHidden();

        // Arrow navigation branches
        SetField(instance, "_autocompleteSuggestions", new List<ArticleWikiLinkAutocompleteItem>());
        await instance.OnAutocompleteArrowDown();
        await instance.OnAutocompleteArrowUp();

        SetField(instance, "_autocompleteSuggestions", new List<ArticleWikiLinkAutocompleteItem>
        {
            ArticleWikiLinkAutocompleteItem.FromInternal(new LinkSuggestionDto { ArticleId = Guid.NewGuid(), Title = "One", Slug = "one", DisplayPath = "One" }),
            ArticleWikiLinkAutocompleteItem.FromInternal(new LinkSuggestionDto { ArticleId = Guid.NewGuid(), Title = "Two", Slug = "two", DisplayPath = "Two" })
        });
        SetField(instance, "_autocompleteSelectedIndex", 0);
        await instance.OnAutocompleteArrowDown();
        await instance.OnAutocompleteArrowUp();
        await cut.InvokeAsync(() => InvokePrivateTask(instance, "OnAutocompleteIndexChanged", 1));
        Assert.Equal(1, (int)GetField(instance, "_autocompleteSelectedIndex")!);

        // Enter -> select current suggestion
        await instance.OnAutocompleteEnter();

        // Wiki link navigation branches
        await instance.OnWikiLinkClicked("not-a-guid");
        var articleId = Guid.NewGuid();
        d.ArticleCache.GetNavigationPathAsync(articleId).Returns("world/wiki");
        await instance.OnWikiLinkClicked(articleId.ToString());
        Assert.EndsWith("/article/world/wiki", fakeNav.Uri, StringComparison.OrdinalIgnoreCase);
        d.ArticleCache.GetNavigationPathAsync(articleId).Returns((string?)null);
        await instance.OnWikiLinkClicked(articleId.ToString());
        d.ArticleCache.GetNavigationPathAsync(articleId)
            .Returns(Task.FromException<string?>(new Exception("nav fail")));
        await instance.OnWikiLinkClicked(articleId.ToString());

        await instance.OnBrokenLinkClicked("missing");

        // External preview: guard, no-content, success, cache hit, exception
        await instance.OnExternalLinkClicked("", "", "");

        d.ExternalLinkApi.GetContentAsync("srd", "none", Arg.Any<CancellationToken>())
            .Returns((ExternalLinkContentDto?)null);
        await instance.OnExternalLinkClicked("srd", "none", "");

        d.ExternalLinkApi.GetContentAsync("srd", "acid-arrow", Arg.Any<CancellationToken>())
            .Returns(new ExternalLinkContentDto
            {
                Source = "srd",
                Id = "acid-arrow",
                Title = "Acid Arrow",
                Kind = "Spell",
                Markdown = "content"
            });
        d.DrawerCoordinator.Open(DrawerType.Metadata);
        await instance.OnExternalLinkClicked("srd", "acid-arrow", "Acid Arrow");
        Assert.Equal(DrawerType.None, d.DrawerCoordinator.Current);
        Assert.True((bool)GetField(instance, "_externalPreviewOpen")!);

        d.DrawerCoordinator.Open(DrawerType.Quests);
        cut.WaitForAssertion(() => Assert.False((bool)GetField(instance, "_externalPreviewOpen")!));

        await instance.OnExternalLinkClicked("srd", "acid-arrow", "Acid Arrow"); // cache hit

        d.ExternalLinkApi.GetContentAsync("srd", "boom", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExternalLinkContentDto?>(new Exception("content fail")));
        await instance.OnExternalLinkClicked("srd", "boom", "Boom");

        // Get article path / summary preview
        Assert.Null(await instance.GetArticlePath("nope"));
        d.ArticleCache.GetArticlePathAsync(articleId).Returns("world/path");
        Assert.Equal("world/path", await instance.GetArticlePath(articleId.ToString()));
        d.ArticleCache.GetArticlePathAsync(articleId)
            .Returns(Task.FromException<string?>(new Exception("path fail")));
        Assert.Null(await instance.GetArticlePath(articleId.ToString()));

        Assert.Null(await instance.GetArticleSummaryPreview("bad-guid"));
        d.SummaryApi.GetSummaryPreviewAsync(articleId).Returns((SummaryPreviewDto?)null);
        Assert.Null(await instance.GetArticleSummaryPreview(articleId.ToString()));
        d.SummaryApi.GetSummaryPreviewAsync(articleId).Returns(new SummaryPreviewDto { Title = "A", Summary = "" });
        Assert.Null(await instance.GetArticleSummaryPreview(articleId.ToString()));
        d.SummaryApi.GetSummaryPreviewAsync(articleId).Returns(new SummaryPreviewDto { Title = "A", Summary = "sum", TemplateName = "T" });
        Assert.NotNull(await instance.GetArticleSummaryPreview(articleId.ToString()));
        d.SummaryApi.GetSummaryPreviewAsync(articleId)
            .Returns(Task.FromException<SummaryPreviewDto?>(new Exception("summary fail")));
        Assert.Null(await instance.GetArticleSummaryPreview(articleId.ToString()));

        // Image upload lifecycle helpers
        var emptyWorldCut = RenderComponent<PrivateNotesTipTapEditor>(p => p
            .Add(x => x.WorldId, Guid.Empty)
            .Add(x => x.Value, ""));
        Assert.Null(await emptyWorldCut.Instance.OnImageUploadRequested("a.png", "image/png", 10));

        d.WorldApi.RequestDocumentUploadAsync(worldId, Arg.Any<WorldDocumentUploadRequestDto>())
            .Returns((WorldDocumentUploadResponseDto?)null);
        Assert.Null(await instance.OnImageUploadRequested("a.png", "image/png", 10));

        var docId = Guid.NewGuid();
        d.WorldApi.RequestDocumentUploadAsync(worldId, Arg.Any<WorldDocumentUploadRequestDto>())
            .Returns(new WorldDocumentUploadResponseDto { DocumentId = docId, UploadUrl = "https://upload", Title = "a.png" });
        Assert.NotNull(await instance.OnImageUploadRequested("a.png", "image/png", 10));

        d.WorldApi.RequestDocumentUploadAsync(worldId, Arg.Any<WorldDocumentUploadRequestDto>())
            .Returns(Task.FromException<WorldDocumentUploadResponseDto?>(new Exception("upload req fail")));
        Assert.Null(await instance.OnImageUploadRequested("a.png", "image/png", 10));

        await instance.OnImageUploadConfirmed("not-a-guid");
        d.WorldApi.ConfirmDocumentUploadAsync(worldId, docId).Returns(new WorldDocumentDto { Id = docId, WorldId = worldId });
        await instance.OnImageUploadConfirmed(docId.ToString());
        d.WorldApi.ConfirmDocumentUploadAsync(worldId, docId)
            .Returns(Task.FromException<WorldDocumentDto?>(new Exception("confirm fail")));
        await instance.OnImageUploadConfirmed(docId.ToString());

        Assert.Equal($"chronicis-image:{docId}", instance.GetImageProxyUrl(docId.ToString()));
        Assert.Null(await instance.ResolveImageUrl("bad"));
        d.WorldApi.DownloadDocumentAsync(docId).Returns(new DocumentDownloadResult("https://download", "a.png", "image/png", 10));
        Assert.Equal("https://download", await instance.ResolveImageUrl(docId.ToString()));
        d.WorldApi.DownloadDocumentAsync(docId)
            .Returns(Task.FromException<DocumentDownloadResult?>(new Exception("download fail")));
        Assert.Null(await instance.ResolveImageUrl(docId.ToString()));

        await instance.OnImageUploadStarted("map.png");
        await instance.OnImageUploadError("upload failed");

        // Private selection helpers via reflection
        var categorySuggestion = ArticleWikiLinkAutocompleteItem.FromExternal(new ExternalLinkSuggestionDto
        {
            Source = "srd",
            Id = "_category/spells",
            Title = "Spells",
            Category = "_category",
            Icon = "📚"
        });
        await InvokePrivateTask(instance, "OnAutocompleteSelect", categorySuggestion);

        var invalidExternalSuggestion = ArticleWikiLinkAutocompleteItem.FromExternal(new ExternalLinkSuggestionDto
        {
            Source = "srd",
            Id = "",
            Title = "Broken External"
        });
        await InvokePrivateTask(instance, "OnAutocompleteSelect", invalidExternalSuggestion);

        var validExternalSuggestion = ArticleWikiLinkAutocompleteItem.FromExternal(new ExternalLinkSuggestionDto
        {
            Source = "srd",
            Id = "fireball",
            Title = "Fireball"
        });
        await InvokePrivateTask(instance, "OnAutocompleteSelect", validExternalSuggestion);

        var invalidInternalSuggestion = new PrivateCtorAutocompleteItemBuilder()
            .WithTitle("Broken Internal")
            .Build();
        await InvokePrivateTask(instance, "OnAutocompleteSelect", invalidInternalSuggestion);

        var validInternalSuggestion = ArticleWikiLinkAutocompleteItem.FromInternal(new LinkSuggestionDto
        {
            ArticleId = Guid.NewGuid(),
            Title = "Valid Internal",
            DisplayPath = "Wiki/Valid",
            Slug = "valid-internal",
            MatchedAlias = "Alias"
        });
        await InvokePrivateTask(instance, "OnAutocompleteSelect", validInternalSuggestion);

        JSInterop.SetupVoid("insertWikiLink", _ => true).SetException(new InvalidOperationException("js fail"));
        await InvokePrivateTask(instance, "OnAutocompleteSelect", validInternalSuggestion);

        // OnAutocompleteCreate branches
        SetField(instance, "_autocompleteIsExternalQuery", true);
        await InvokePrivateTask(instance, "OnAutocompleteCreate", "Ignored");
        SetField(instance, "_autocompleteIsExternalQuery", false);

        d.WikiLinkService.CreateArticleFromAutocompleteAsync("Null Article", worldId).Returns((ArticleDto?)null);
        await InvokePrivateTask(instance, "OnAutocompleteCreate", "Null Article");

        d.WikiLinkService.CreateArticleFromAutocompleteAsync("Created Article", worldId)
            .Returns(new ArticleDto { Id = Guid.NewGuid(), Title = "Created Article" });
        await InvokePrivateTask(instance, "OnAutocompleteCreate", "Created Article");

        d.WikiLinkService.CreateArticleFromAutocompleteAsync("Boom Article", worldId)
            .Returns(Task.FromException<ArticleDto?>(new Exception("create fail")));
        await InvokePrivateTask(instance, "OnAutocompleteCreate", "Boom Article");

        // Close preview helper
        SetField(instance, "_externalPreviewOpen", true);
        await cut.InvokeAsync(() =>
        {
            InvokePrivate(instance, "CloseExternalPreview");
            return Task.CompletedTask;
        });
        Assert.False((bool)GetField(instance, "_externalPreviewOpen")!);

        // Parser helper branches
        Assert.False(CallTryParse(null, out _, out _));
        Assert.False(CallTryParse("noslash", out _, out _));
        Assert.False(CallTryParse("/leading", out _, out _));
        Assert.True(CallTryParse(" SRD/acid", out var source, out var remainder));
        Assert.Equal("srd", source);
        Assert.Equal("acid", remainder);

        await cut.InvokeAsync(() => instance.DisposeAsync().AsTask());
        cut.Dispose();
        emptyWorldCut.Dispose();
    }

    private static object? GetField(object instance, string name)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);

    private static void SetField(object instance, string name, object? value)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);

    private static object? GetProperty(object instance, string name)
        => instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);

    private static void InvokePrivate(object instance, string methodName, params object?[]? args)
        => instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, args);

    private static async Task InvokePrivateTask(object instance, string methodName, params object?[] args)
    {
        var result = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static object? InvokeStatic(string methodName, object?[]? args)
        => typeof(PrivateNotesTipTapEditor).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args);

    private static bool CallTryParse(string? query, out string source, out string remainder)
    {
        var args = new object?[] { query!, null, null };
        var result = (bool)InvokeStatic("TryParseExternalAutocompleteQuery", args)!;
        source = args[1] as string ?? string.Empty;
        remainder = args[2] as string ?? string.Empty;
        return result;
    }

    private sealed class PrivateCtorAutocompleteItemBuilder
    {
        private string _title = string.Empty;

        public PrivateCtorAutocompleteItemBuilder WithTitle(string title)
        {
            _title = title;
            return this;
        }

        public ArticleWikiLinkAutocompleteItem Build()
        {
            var ctor = typeof(ArticleWikiLinkAutocompleteItem)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null);
            Assert.NotNull(ctor);
            var item = (ArticleWikiLinkAutocompleteItem)ctor!.Invoke(Array.Empty<object>());

            SetInitProperty(item, nameof(ArticleWikiLinkAutocompleteItem.Title), _title);
            SetInitProperty(item, nameof(ArticleWikiLinkAutocompleteItem.IsExternal), false);
            SetInitProperty(item, nameof(ArticleWikiLinkAutocompleteItem.IsCategory), false);
            return item;
        }

        private static void SetInitProperty(object target, string propertyName, object? value)
            => target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .SetValue(target, value);
    }
}
