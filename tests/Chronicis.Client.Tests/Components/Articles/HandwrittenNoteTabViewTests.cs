using System.Diagnostics.CodeAnalysis;
using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.Articles;
using Chronicis.Client.Components.Maps;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Articles;

/// <summary>
/// Unit tests for HandwrittenNoteTabView component.
/// Tests tab rendering, active tab logic, empty state display, and transcribe button callback.
/// </summary>
[ExcludeFromCodeCoverage]
public class HandwrittenNoteTabViewTests : MudBlazorTestContext
{
    private bool _providersRendered;

    private void RegisterDependencies()
    {
        Services.AddSingleton(Substitute.For<ILinkApiService>());
        Services.AddSingleton(Substitute.For<IExternalLinkApiService>());
        Services.AddSingleton(Substitute.For<IMapApiService>());
        Services.AddSingleton(Substitute.For<IWikiLinkService>());
        Services.AddSingleton(Substitute.For<IWikiLinkCommitService>());
        Services.AddSingleton(Substitute.For<IArticleCacheService>());
        Services.AddSingleton(Substitute.For<IAISummaryApiService>());
        Services.AddSingleton(Substitute.For<IWorldApiService>());
        Services.AddSingleton<IDrawerCoordinator>(new DrawerCoordinator());
        Services.AddSingleton(Substitute.For<ILogger<PrivateNotesTipTapEditor>>());
        Services.AddSingleton(Substitute.For<IAppNavigator>());

        ComponentFactories.AddStub<ArticleDetailWikiLinkAutocomplete>();
        ComponentFactories.AddStub<ExternalLinkDetailPanel>();
        ComponentFactories.AddStub<SessionMapViewerModal>();

        if (!_providersRendered)
        {
            RenderComponent<MudPopoverProvider>();
            RenderComponent<MudSnackbarProvider>();
            RenderComponent<MudDialogProvider>();
            _providersRendered = true;
        }
    }

    #region Image Rendering (Requirement 5.2)

    [Fact]
    public void HandwrittenTab_WhenImageDownloadUrlIsNonNull_RendersImage()
    {
        RegisterDependencies();
        var url = "https://example.com/note.png";

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.ImageDownloadUrl, url)
            .Add(x => x.WorldId, Guid.NewGuid()));

        var img = cut.Find("img");
        Assert.Equal(url, img.GetAttribute("src"));
        Assert.Equal("Handwritten note", img.GetAttribute("alt"));
    }

    [Fact]
    public void HandwrittenTab_WhenImageDownloadUrlIsNull_ShowsNoImageAlert()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.ImageDownloadUrl, (string?)null)
            .Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("No handwritten note image available", cut.Markup);
    }

    [Fact]
    public void HandwrittenTab_WhenImageDownloadUrlIsEmpty_ShowsNoImageAlert()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.ImageDownloadUrl, "")
            .Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("No handwritten note image available", cut.Markup);
    }

    [Fact]
    public void HandwrittenTab_WhenImageDownloadUrlIsWhitespace_ShowsNoImageAlert()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.ImageDownloadUrl, "   ")
            .Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("No handwritten note image available", cut.Markup);
    }

    #endregion

    #region Transcribed Tab Empty State (Requirement 5.4)

    [Fact]
    public void TranscribedTab_WhenBodyIsNull_ShowsNoTranscriptionMessage()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.Body, (string?)null)
            .Add(x => x.ShowTranscribedTabActive, true)
            .Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("No transcription available", cut.Markup);
    }

    [Fact]
    public void TranscribedTab_WhenBodyIsEmpty_ShowsNoTranscriptionMessage()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.Body, "")
            .Add(x => x.ShowTranscribedTabActive, true)
            .Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("No transcription available", cut.Markup);
    }

    [Fact]
    public void TranscribedTab_WhenBodyIsWhitespace_ShowsNoTranscriptionMessage()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.Body, "   ")
            .Add(x => x.ShowTranscribedTabActive, true)
            .Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("No transcription available", cut.Markup);
    }

    [Fact]
    public void TranscribedTab_WhenBodyIsNull_ShowsTranscribeButton()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.Body, (string?)null)
            .Add(x => x.ShowTranscribedTabActive, true)
            .Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("Transcribe", cut.Markup);
        var buttons = cut.FindComponents<MudButton>();
        Assert.Contains(buttons, b => b.Markup.Contains("Transcribe", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Transcribed Tab With Content (Requirement 5.3)

    [Fact]
    public void TranscribedTab_WhenBodyHasContent_ShowsTipTapEditor()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.Body, "<p>Some transcribed content</p>")
            .Add(x => x.ShowTranscribedTabActive, true)
            .Add(x => x.WorldId, Guid.NewGuid()));

        // Should NOT show the empty state message
        Assert.DoesNotContain("No transcription available", cut.Markup);
        // PrivateNotesTipTapEditor should be rendered (its container div)
        var editor = cut.FindComponent<PrivateNotesTipTapEditor>();
        Assert.NotNull(editor);
    }

    #endregion

    #region Active Tab Logic (Requirement 5.1)

    [Fact]
    public void WhenShowTranscribedTabActiveIsTrue_TranscribedTabIsActive()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.Body, (string?)null)
            .Add(x => x.ShowTranscribedTabActive, true)
            .Add(x => x.WorldId, Guid.NewGuid()));

        // The Transcribed tab content should be visible (shows empty state)
        Assert.Contains("No transcription available", cut.Markup);
    }

    [Fact]
    public void WhenShowTranscribedTabActiveIsFalse_HandwrittenTabIsActive()
    {
        RegisterDependencies();
        var url = "https://example.com/note.png";

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.ImageDownloadUrl, url)
            .Add(x => x.ShowTranscribedTabActive, false)
            .Add(x => x.WorldId, Guid.NewGuid()));

        // The Handwritten tab content should be visible (shows image)
        var img = cut.Find("img");
        Assert.Equal(url, img.GetAttribute("src"));
    }

    #endregion

    #region Transcribe Button Callback (Requirement 5.4)

    [Fact]
    public async Task TranscribeButton_WhenClicked_InvokesOnTranscribeRequested()
    {
        RegisterDependencies();
        var callbackInvoked = false;

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.Body, (string?)null)
            .Add(x => x.ShowTranscribedTabActive, true)
            .Add(x => x.OnTranscribeRequested, EventCallback.Factory.Create(this, () => callbackInvoked = true))
            .Add(x => x.WorldId, Guid.NewGuid()));

        var transcribeButton = cut.FindComponents<MudButton>()
            .First(b => b.Markup.Contains("Transcribe", StringComparison.OrdinalIgnoreCase));
        await cut.InvokeAsync(() => transcribeButton.Find("button").Click());

        Assert.True(callbackInvoked);
    }

    #endregion

    #region Tab Labels Rendered (Requirement 5.1)

    [Fact]
    public void Component_RendersBothTabLabels()
    {
        RegisterDependencies();

        var cut = RenderComponent<HandwrittenNoteTabView>(p => p
            .Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("Handwritten", cut.Markup);
        Assert.Contains("Transcribed", cut.Markup);
    }

    #endregion
}
