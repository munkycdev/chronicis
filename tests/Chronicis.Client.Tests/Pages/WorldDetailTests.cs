using System.Reflection;
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Components.Settings;
using Chronicis.Client.Components.World;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class WorldDetailTests : MudBlazorTestContext
{
    [Fact]
    public async Task WorldDetail_GenerateSlugFromName_NormalizesAndPads()
    {
        var slug = await InvokePrivateWithResultAsync<string>(new WorldDetail(), "GenerateSlugFromName", "  My World!  ");
        var shortSlug = await InvokePrivateWithResultAsync<string>(new WorldDetail(), "GenerateSlugFromName", "a");

        Assert.Equal("my-world", slug);
        Assert.Equal("a00", shortSlug);
    }

    [Fact]
    public async Task WorldDetail_GenerateSlugFromName_Whitespace_ReturnsEmpty()
    {
        var slug = await InvokePrivateWithResultAsync<string>(new WorldDetail(), "GenerateSlugFromName", "   ");

        Assert.Equal(string.Empty, slug);
    }

    [Fact]
    public async Task WorldDetail_GetFaviconUrl_ValidAndInvalidUrls()
    {
        var valid = await InvokePrivateWithResultAsync<string>(new WorldDetail(), "GetFaviconUrl", "https://example.com/path");
        var invalid = await InvokePrivateWithResultAsync<string>(new WorldDetail(), "GetFaviconUrl", "not-a-url");

        Assert.Equal("https://www.google.com/s2/favicons?domain=example.com&sz=32", valid);
        Assert.Equal(string.Empty, invalid);
    }

    [Theory]
    [InlineData("application/pdf", Icons.Material.Filled.PictureAsPdf)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", Icons.Material.Filled.Description)]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Icons.Material.Filled.TableChart)]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation", Icons.Material.Filled.Slideshow)]
    [InlineData("text/plain", Icons.Material.Filled.TextSnippet)]
    [InlineData("text/markdown", Icons.Material.Filled.Article)]
    [InlineData("image/png", Icons.Material.Filled.Image)]
    [InlineData("application/octet-stream", Icons.Material.Filled.InsertDriveFile)]
    public async Task WorldDetail_GetDocumentIcon_ReturnsExpectedIcon(string contentType, string expected)
    {
        var sut = new WorldDetail();

        var icon = await InvokePrivateWithResultAsync<string>(sut, "GetDocumentIcon", contentType);

        Assert.Equal(expected, icon);
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1048576, "1 MB")]
    public async Task WorldDetail_FormatFileSize_FormatsUnits(long bytes, string expected)
    {
        var sut = new WorldDetail();

        var text = await InvokePrivateWithResultAsync<string>(sut, "FormatFileSize", bytes);

        Assert.Equal(expected, text);
    }

    [Fact]
    public async Task WorldDetail_NavigateToArticle_WithBreadcrumbs_UsesBreadcrumbUrl()
    {
        var sut = new WorldDetail();
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        breadcrumbService.BuildArticleUrl(Arg.Any<List<BreadcrumbDto>>()).Returns("/article/world/path");

        SetProperty(sut, "Navigation", nav);
        SetProperty(sut, "BreadcrumbService", breadcrumbService);

        var article = new ArticleDto
        {
            Slug = "fallback",
            Breadcrumbs = [new BreadcrumbDto { Slug = "world" }]
        };

        await InvokePrivateAsync(sut, "NavigateToArticle", article);

        Assert.EndsWith("/article/world/path", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_NavigateToArticle_WithoutBreadcrumbs_UsesSlugFallback()
    {
        var sut = new WorldDetail();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        SetProperty(sut, "Navigation", nav);
        SetProperty(sut, "BreadcrumbService", Substitute.For<IBreadcrumbService>());

        var article = new ArticleDto { Slug = "fallback" };

        await InvokePrivateAsync(sut, "NavigateToArticle", article);

        Assert.EndsWith("/article/fallback", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_NavigateToArticle_NullBreadcrumbs_UsesSlugFallback()
    {
        var sut = new WorldDetail();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        SetProperty(sut, "Navigation", nav);
        SetProperty(sut, "BreadcrumbService", Substitute.For<IBreadcrumbService>());

        var article = new ArticleDto { Slug = "fallback", Breadcrumbs = null! };

        await InvokePrivateAsync(sut, "NavigateToArticle", article);

        Assert.EndsWith("/article/fallback", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_CheckSlugAvailability_WithEmptySlug_SetsResetStateAndReturns()
    {
        var sut = new WorldDetail();
        SetPrivateField(sut, "_publicSlug", "   ");
        SetPrivateField(sut, "_slugIsAvailable", true);
        SetPrivateField(sut, "_slugError", "old");
        SetPrivateField(sut, "_slugHelperText", "old");

        await InvokePrivateAsync(sut, "CheckSlugAvailability");

        Assert.False(GetPrivateField<bool>(sut, "_slugIsAvailable"));
        Assert.Null(GetPrivateField<string?>(sut, "_slugError"));
        Assert.Null(GetPrivateField<string?>(sut, "_slugHelperText"));
    }

    [Fact]
    public async Task WorldDetail_SaveNewLink_MissingTitleOrUrl_ReturnsWithoutCreate()
    {
        var sut = new WorldDetail();
        var worldApi = Substitute.For<IWorldApiService>();

        SetProperty(sut, "WorldApi", worldApi);
        SetProperty(sut, "Snackbar", Substitute.For<ISnackbar>());
        SetPrivateField(sut, "_newLinkTitle", "");
        SetPrivateField(sut, "_newLinkUrl", "https://example.com");

        await InvokePrivateAsync(sut, "SaveNewLink");

        await worldApi.DidNotReceive().CreateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<WorldLinkCreateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveEditLink_InvalidUrl_ReturnsWithoutUpdate()
    {
        var sut = new WorldDetail();
        var worldApi = Substitute.For<IWorldApiService>();

        SetProperty(sut, "WorldApi", worldApi);
        SetProperty(sut, "Snackbar", Substitute.For<ISnackbar>());
        SetPrivateField(sut, "_editingLinkId", Guid.NewGuid());
        SetPrivateField(sut, "_editLinkTitle", "Link");
        SetPrivateField(sut, "_editLinkUrl", "bad-url");

        await InvokePrivateAsync(sut, "SaveEditLink");

        await worldApi.DidNotReceive().UpdateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<WorldLinkUpdateDto>());
    }

    [Fact]
    public async Task WorldDetail_CopyPublicUrl_WhenNoUrl_DoesNothing()
    {
        var sut = new WorldDetail();
        var js = Substitute.For<IJSRuntime>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        SetProperty(sut, "JSRuntime", js);
        SetProperty(sut, "Snackbar", Substitute.For<ISnackbar>());
        SetProperty(sut, "Navigation", nav);
        SetPrivateField(sut, "_world", new WorldDetailDto { PublicSlug = string.Empty });

        await InvokePrivateAsync(sut, "CopyPublicUrl");

        Assert.Empty(js.ReceivedCalls());
    }

    [Fact]
    public async Task WorldDetail_GetFullPublicUrl_WithSlug_BuildsExpectedUrl()
    {
        var sut = new WorldDetail();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        SetProperty(sut, "Navigation", nav);
        SetPrivateField(sut, "_world", new WorldDetailDto { PublicSlug = "my-world" });

        var url = await InvokePrivateWithResultAsync<string>(sut, "GetFullPublicUrl");

        Assert.EndsWith("/w/my-world", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_GetFullPublicUrl_WhenWorldNull_ReturnsEmpty()
    {
        var sut = new WorldDetail();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        SetProperty(sut, "Navigation", nav);
        SetPrivateField(sut, "_world", null);

        var url = await InvokePrivateWithResultAsync<string>(sut, "GetFullPublicUrl");

        Assert.Equal(string.Empty, url);
    }

    [Fact]
    public async Task WorldDetail_ShouldShowPublicPreview_WhenWorldNull_ReturnsFalse()
    {
        var sut = new WorldDetail();
        SetPrivateField(sut, "_world", null);

        var value = await InvokePrivateWithResultAsync<bool>(sut, "ShouldShowPublicPreview");

        Assert.False(value);
    }

    [Fact]
    public async Task WorldDetail_ShouldShowPublicPreview_WhenWorldNotPublic_ReturnsFalse()
    {
        var sut = new WorldDetail();
        SetPrivateField(sut, "_world", new WorldDetailDto { IsPublic = false, PublicSlug = "slug" });

        var value = await InvokePrivateWithResultAsync<bool>(sut, "ShouldShowPublicPreview");

        Assert.False(value);
    }

    [Fact]
    public async Task WorldDetail_ShouldShowPublicPreview_WhenPublicSlugMissing_ReturnsFalse()
    {
        var sut = new WorldDetail();
        SetPrivateField(sut, "_world", new WorldDetailDto { IsPublic = true, PublicSlug = string.Empty });

        var value = await InvokePrivateWithResultAsync<bool>(sut, "ShouldShowPublicPreview");

        Assert.False(value);
    }

    [Fact]
    public async Task WorldDetail_ShouldShowPublicPreview_WhenPublicAndSlugPresent_ReturnsTrue()
    {
        var sut = new WorldDetail();
        SetPrivateField(sut, "_world", new WorldDetailDto { IsPublic = true, PublicSlug = "slug" });

        var value = await InvokePrivateWithResultAsync<bool>(sut, "ShouldShowPublicPreview");

        Assert.True(value);
    }

    [Fact]
    public async Task WorldDetail_CheckSlugAvailability_WhenResultNull_SetsError()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_publicSlug", "taken-slug");
        rendered.WorldApi.CheckPublicSlugAsync(rendered.WorldId, "taken-slug").Returns((PublicSlugCheckResultDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "CheckSlugAvailability");

        Assert.Equal("Failed to check availability", GetPrivateField<string?>(rendered.Instance, "_slugError"));
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_slugIsAvailable"));
    }

    [Fact]
    public async Task WorldDetail_CheckSlugAvailability_WhenValidationError_UsesSuggestedSlug()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_publicSlug", "bad");
        rendered.WorldApi.CheckPublicSlugAsync(rendered.WorldId, "bad").Returns(new PublicSlugCheckResultDto
        {
            ValidationError = "Invalid format",
            SuggestedSlug = "good-slug"
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "CheckSlugAvailability");

        Assert.Equal("Invalid format", GetPrivateField<string?>(rendered.Instance, "_slugError"));
        Assert.Equal("Try: good-slug", GetPrivateField<string?>(rendered.Instance, "_slugHelperText"));
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_slugIsAvailable"));
    }

    [Fact]
    public async Task WorldDetail_CheckSlugAvailability_WhenTaken_SetsTakenMessage()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_publicSlug", "existing");
        rendered.WorldApi.CheckPublicSlugAsync(rendered.WorldId, "existing").Returns(new PublicSlugCheckResultDto
        {
            IsAvailable = false,
            SuggestedSlug = "existing-2"
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "CheckSlugAvailability");

        Assert.Equal("This slug is already taken", GetPrivateField<string?>(rendered.Instance, "_slugError"));
        Assert.Equal("Try: existing-2", GetPrivateField<string?>(rendered.Instance, "_slugHelperText"));
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_slugIsAvailable"));
    }

    [Fact]
    public async Task WorldDetail_CheckSlugAvailability_WhenAvailable_SetsAvailable()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_publicSlug", "available");
        rendered.WorldApi.CheckPublicSlugAsync(rendered.WorldId, "available").Returns(new PublicSlugCheckResultDto
        {
            IsAvailable = true
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "CheckSlugAvailability");

        Assert.True(GetPrivateField<bool>(rendered.Instance, "_slugIsAvailable"));
        Assert.Equal("Available!", GetPrivateField<string?>(rendered.Instance, "_slugHelperText"));
    }

    [Fact]
    public async Task WorldDetail_CheckSlugAvailability_WhenApiThrows_SetsErrorAndUnavailable()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_publicSlug", "boom-slug");
        rendered.WorldApi.CheckPublicSlugAsync(rendered.WorldId, "boom-slug")
            .Returns(Task.FromException<PublicSlugCheckResultDto?>(new Exception("lookup failed")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "CheckSlugAvailability");

        Assert.Equal("Error: lookup failed", GetPrivateField<string?>(rendered.Instance, "_slugError"));
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_slugIsAvailable"));
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_isCheckingSlug"));
    }

    [Fact]
    public async Task WorldDetail_SaveNewLink_WithValidValues_CreatesAndRefreshes()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isAddingLink", true);
        SetPrivateField(rendered.Instance, "_newLinkTitle", "Docs");
        SetPrivateField(rendered.Instance, "_newLinkUrl", "https://example.com");
        SetPrivateField(rendered.Instance, "_newLinkDescription", "desc");

        rendered.WorldApi.CreateWorldLinkAsync(rendered.WorldId, Arg.Any<WorldLinkCreateDto>()).Returns(new WorldLinkDto
        {
            Id = Guid.NewGuid(),
            Title = "Docs",
            Url = "https://example.com"
        });
        rendered.WorldApi.GetWorldLinksAsync(rendered.WorldId).Returns(new List<WorldLinkDto>());

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveNewLink");

        await rendered.WorldApi.Received(1).CreateWorldLinkAsync(rendered.WorldId, Arg.Is<WorldLinkCreateDto>(x =>
            x.Title == "Docs" && x.Url == "https://example.com" && x.Description == "desc"));
        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_isAddingLink"));
    }

    [Fact]
    public async Task WorldDetail_SaveNewLink_WithHttpUrl_CallsCreate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_newLinkTitle", "Docs");
        SetPrivateField(rendered.Instance, "_newLinkUrl", "http://example.com");
        rendered.WorldApi.CreateWorldLinkAsync(rendered.WorldId, Arg.Any<WorldLinkCreateDto>()).Returns((WorldLinkDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveNewLink");

        await rendered.WorldApi.Received(1).CreateWorldLinkAsync(rendered.WorldId, Arg.Any<WorldLinkCreateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveEditLink_WithValidValues_UpdatesAndRefreshes()
    {
        var rendered = CreateRenderedSut();
        var linkId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_editingLinkId", linkId);
        SetPrivateField(rendered.Instance, "_editLinkTitle", "Updated");
        SetPrivateField(rendered.Instance, "_editLinkUrl", "https://example.com/new");
        SetPrivateField(rendered.Instance, "_editLinkDescription", "new");

        rendered.WorldApi.UpdateWorldLinkAsync(rendered.WorldId, linkId, Arg.Any<WorldLinkUpdateDto>()).Returns(new WorldLinkDto
        {
            Id = linkId,
            Title = "Updated",
            Url = "https://example.com/new"
        });
        rendered.WorldApi.GetWorldLinksAsync(rendered.WorldId).Returns(new List<WorldLinkDto>());

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveEditLink");

        await rendered.WorldApi.Received(1).UpdateWorldLinkAsync(rendered.WorldId, linkId, Arg.Is<WorldLinkUpdateDto>(x =>
            x.Title == "Updated" && x.Url == "https://example.com/new" && x.Description == "new"));
        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.Null(GetPrivateField<Guid?>(rendered.Instance, "_editingLinkId"));
    }

    [Fact]
    public async Task WorldDetail_SaveEditLink_WithHttpUrl_CallsUpdate()
    {
        var rendered = CreateRenderedSut();
        var id = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_editingLinkId", id);
        SetPrivateField(rendered.Instance, "_editLinkTitle", "Link");
        SetPrivateField(rendered.Instance, "_editLinkUrl", "http://example.com");
        rendered.WorldApi.UpdateWorldLinkAsync(rendered.WorldId, id, Arg.Any<WorldLinkUpdateDto>()).Returns((WorldLinkDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveEditLink");

        await rendered.WorldApi.Received(1).UpdateWorldLinkAsync(rendered.WorldId, id, Arg.Any<WorldLinkUpdateDto>());
    }

    [Fact]
    public async Task WorldDetail_CopyPublicUrl_WhenClipboardWriteSucceeds_ShowsSuccess()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { PublicSlug = "my-world" });

        await InvokePrivateAsync(rendered.Instance, "CopyPublicUrl");

        await rendered.JsRuntime.Received(1).InvokeVoidAsync("navigator.clipboard.writeText", Arg.Any<object?[]>());
    }

    [Fact]
    public async Task WorldDetail_CopyPublicUrl_WhenClipboardThrows_ShowsFailure()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { PublicSlug = "my-world" });
        rendered.JsRuntime
            .When(x => x.InvokeVoidAsync("navigator.clipboard.writeText", Arg.Any<object?[]>()))
            .Do(_ => throw new Exception("clipboard blocked"));

        await InvokePrivateAsync(rendered.Instance, "CopyPublicUrl");

        await rendered.JsRuntime.Received(1).InvokeVoidAsync("navigator.clipboard.writeText", Arg.Any<object?[]>());
    }

    [Fact]
    public async Task WorldDetail_StartEditLink_WhenDescriptionNull_UsesEmptyDescription()
    {
        var sut = new WorldDetail();
        var link = new WorldLinkDto
        {
            Id = Guid.NewGuid(),
            Title = "Link",
            Url = "https://example.com",
            Description = null
        };

        await InvokePrivateAsync(sut, "StartEditLink", link);

        Assert.Equal(string.Empty, GetPrivateField<string>(sut, "_editLinkDescription"));
    }

    [Fact]
    public async Task WorldDetail_DeleteLink_WhenCanceled_DoesNotCallDelete()
    {
        var rendered = CreateRenderedSut();
        var link = new WorldLinkDto { Id = Guid.NewGuid(), Title = "L", Url = "https://example.com" };
        rendered.DialogService.ShowMessageBox(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns((bool?)false);

        await InvokePrivateOnRendererAsync(rendered.Cut, "DeleteLink", link);

        await rendered.WorldApi.DidNotReceive().DeleteWorldLinkAsync(rendered.WorldId, link.Id);
    }

    [Fact]
    public async Task WorldDetail_DeleteLink_WhenDeleteFails_ShowsError()
    {
        var rendered = CreateRenderedSut();
        var link = new WorldLinkDto { Id = Guid.NewGuid(), Title = "L", Url = "https://example.com" };
        SetPrivateField(rendered.Instance, "_links", new List<WorldLinkDto> { link });
        rendered.DialogService.ShowMessageBox(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns((bool?)true);
        rendered.WorldApi.DeleteWorldLinkAsync(rendered.WorldId, link.Id).Returns(false);

        await InvokePrivateOnRendererAsync(rendered.Cut, "DeleteLink", link);

        await rendered.WorldApi.Received(1).DeleteWorldLinkAsync(rendered.WorldId, link.Id);
        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_DeleteLink_WhenDeleteThrows_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();
        var link = new WorldLinkDto { Id = Guid.NewGuid(), Title = "L", Url = "https://example.com" };
        rendered.DialogService.ShowMessageBox(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns((bool?)true);
        rendered.WorldApi.DeleteWorldLinkAsync(rendered.WorldId, link.Id).Returns(Task.FromException<bool>(new Exception("boom")));

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "DeleteLink", link));

        Assert.Null(ex);
    }

    [Fact]
    public async Task WorldDetail_OpenUploadDialog_WhenCanceled_DoesNotRefresh()
    {
        var rendered = CreateRenderedSut();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        rendered.DialogService.ShowAsync<WorldDocumentUploadDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(dialog));

        await InvokePrivateOnRendererAsync(rendered.Cut, "OpenUploadDialog");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_OpenUploadDialog_WhenSuccess_Refreshes()
    {
        var rendered = CreateRenderedSut();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(new object())));
        rendered.DialogService.ShowAsync<WorldDocumentUploadDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(dialog));
        rendered.WorldApi.GetWorldDocumentsAsync(rendered.WorldId).Returns(new List<WorldDocumentDto>());

        await InvokePrivateOnRendererAsync(rendered.Cut, "OpenUploadDialog");

        await rendered.TreeState.Received(1).RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_OpenUploadDialog_WhenDialogResultIsNull_DoesNotRefresh()
    {
        var rendered = CreateRenderedSut();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(null));
        rendered.DialogService.ShowAsync<WorldDocumentUploadDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult(dialog));

        await InvokePrivateOnRendererAsync(rendered.Cut, "OpenUploadDialog");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_StartEditDocument_WhenDescriptionNull_UsesEmptyDescription()
    {
        var sut = new WorldDetail();
        var doc = new WorldDocumentDto
        {
            Id = Guid.NewGuid(),
            Title = "Doc",
            Description = null
        };

        await InvokePrivateAsync(sut, "StartEditDocument", doc);

        Assert.Equal(string.Empty, GetPrivateField<string>(sut, "_editDocumentDescription"));
    }

    [Fact]
    public async Task WorldDetail_SaveDocumentEdit_WhenNoEditingId_Returns()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_editingDocumentId", null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveDocumentEdit");

        await rendered.WorldApi.DidNotReceive().UpdateDocumentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<WorldDocumentUpdateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveDocumentEdit_WhenUpdatedNull_DoesNotRefresh()
    {
        var rendered = CreateRenderedSut();
        var docId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_editingDocumentId", docId);
        SetPrivateField(rendered.Instance, "_editDocumentTitle", "Doc");
        SetPrivateField(rendered.Instance, "_editDocumentDescription", "Desc");
        rendered.WorldApi.UpdateDocumentAsync(rendered.WorldId, docId, Arg.Any<WorldDocumentUpdateDto>()).Returns((WorldDocumentDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveDocumentEdit");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_SaveDocumentEdit_WhenDocNotFound_StillClearsEditing()
    {
        var rendered = CreateRenderedSut();
        var docId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_documents", new List<WorldDocumentDto>());
        SetPrivateField(rendered.Instance, "_editingDocumentId", docId);
        SetPrivateField(rendered.Instance, "_editDocumentTitle", "Doc");
        SetPrivateField(rendered.Instance, "_editDocumentDescription", "Desc");
        rendered.WorldApi.UpdateDocumentAsync(rendered.WorldId, docId, Arg.Any<WorldDocumentUpdateDto>()).Returns(new WorldDocumentDto
        {
            Id = docId,
            Title = "Doc",
            Description = "Desc"
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveDocumentEdit");

        Assert.Null(GetPrivateField<Guid?>(rendered.Instance, "_editingDocumentId"));
        await rendered.TreeState.Received(1).RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_DeleteDocument_WhenDocMissing_Returns()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_documents", new List<WorldDocumentDto>());

        await InvokePrivateOnRendererAsync(rendered.Cut, "DeleteDocument", Guid.NewGuid());

        await rendered.WorldApi.DidNotReceive().DeleteDocumentAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task WorldDetail_DeleteDocument_WhenConfirmedAndDeleteFails_DoesNotRefresh()
    {
        var rendered = CreateRenderedSut();
        var docId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_documents", new List<WorldDocumentDto>
        {
            new() { Id = docId, Title = "Doc" }
        });
        rendered.DialogService.ShowMessageBox(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns((bool?)true);
        rendered.WorldApi.DeleteDocumentAsync(rendered.WorldId, docId).Returns(false);

        await InvokePrivateOnRendererAsync(rendered.Cut, "DeleteDocument", docId);

        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_DownloadDocument_WhenDownloadNull_DoesNotOpenWindow()
    {
        var rendered = CreateRenderedSut();
        var docId = Guid.NewGuid();
        rendered.WorldApi.DownloadDocumentAsync(docId).Returns((DocumentDownloadResult?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "DownloadDocument", docId);

        await rendered.JsRuntime.DidNotReceive().InvokeVoidAsync("open", Arg.Any<object?[]>());
    }

    [Fact]
    public async Task WorldDetail_DownloadDocument_WhenApiThrows_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();
        var docId = Guid.NewGuid();
        rendered.WorldApi.DownloadDocumentAsync(docId).Returns(Task.FromException<DocumentDownloadResult?>(new Exception("boom")));

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "DownloadDocument", docId));

        Assert.Null(ex);
    }

    [Fact]
    public async Task WorldDetail_CreateCampaign_WhenDialogCanceled_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        rendered.DialogService.ShowAsync<CreateCampaignDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));
        var before = rendered.Navigation.Uri;

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateCampaign");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
        Assert.Equal(before, rendered.Navigation.Uri);
    }

    [Fact]
    public async Task WorldDetail_CreateCharacter_WhenDialogResultIsNull_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(null));
        rendered.DialogService.ShowAsync<CreateArticleDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));
        var before = rendered.Navigation.Uri;

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateCharacter");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
        Assert.Equal(before, rendered.Navigation.Uri);
    }

    [Fact]
    public async Task WorldDetail_CreateCampaign_WhenDialogReturnsCampaign_Navigates()
    {
        var rendered = CreateRenderedSut();
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "C" };
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(campaign)));
        rendered.DialogService.ShowAsync<CreateCampaignDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateCampaign");

        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.EndsWith($"/campaign/{campaign.Id}", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_CreateCharacter_WhenDialogReturnsArticle_NavigatesViaBreadcrumb()
    {
        var rendered = CreateRenderedSut();
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Slug = "hero",
            Breadcrumbs = [new BreadcrumbDto { Slug = "world" }, new BreadcrumbDto { Slug = "hero" }]
        };
        rendered.BreadcrumbService.BuildArticleUrl(article.Breadcrumbs).Returns("/article/world/hero");
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(article)));
        rendered.DialogService.ShowAsync<CreateArticleDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateCharacter");

        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.EndsWith("/article/world/hero", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_CreateWikiArticle_WhenDialogReturnsArticle_NavigatesViaSlugFallback()
    {
        var rendered = CreateRenderedSut();
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Slug = "wiki-slug",
            Breadcrumbs = new List<BreadcrumbDto>()
        };
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(article)));
        rendered.DialogService.ShowAsync<CreateArticleDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateWikiArticle");

        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.EndsWith("/article/wiki-slug", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_CreateWikiArticle_WhenDialogResultIsWrongType_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(new object())));
        rendered.DialogService.ShowAsync<CreateArticleDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));
        var before = rendered.Navigation.Uri;

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateWikiArticle");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
        Assert.Equal(before, rendered.Navigation.Uri);
    }

    [Fact]
    public async Task WorldDetail_CreateWikiArticle_WhenDialogCanceled_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        rendered.DialogService.ShowAsync<CreateArticleDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));
        var before = rendered.Navigation.Uri;

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateWikiArticle");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
        Assert.Equal(before, rendered.Navigation.Uri);
    }

    [Fact]
    public async Task WorldDetail_OnPublicToggleChanged_WhenPublicAndSlugEmpty_GeneratesSlug()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Name = "My Cool World" });
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_publicSlug", string.Empty);
        rendered.WorldApi.CheckPublicSlugAsync(rendered.WorldId, Arg.Any<string>()).Returns(new PublicSlugCheckResultDto { IsAvailable = true });

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnPublicToggleChanged");

        var slug = GetPrivateField<string>(rendered.Instance, "_publicSlug");
        Assert.Equal("my-cool-world", slug);
        Assert.True(GetPrivateField<bool>(rendered.Instance, "_hasUnsavedChanges"));
    }

    [Fact]
    public async Task WorldDetail_OnPublicToggleChanged_WhenNotPublic_DoesNotGenerateSlug()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Name = "My Cool World" });
        SetPrivateField(rendered.Instance, "_isPublic", false);
        SetPrivateField(rendered.Instance, "_publicSlug", string.Empty);

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnPublicToggleChanged");

        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_publicSlug"));
        Assert.True(GetPrivateField<bool>(rendered.Instance, "_hasUnsavedChanges"));
    }

    [Fact]
    public async Task WorldDetail_OnPublicToggleChanged_WhenWorldNullAndSlugEmpty_LeavesSlugEmpty()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", null);
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_publicSlug", string.Empty);
        rendered.WorldApi.CheckPublicSlugAsync(rendered.WorldId, Arg.Any<string>())
            .Returns(new PublicSlugCheckResultDto { ValidationError = "Slug must be at least 3 characters" });

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnPublicToggleChanged");

        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_publicSlug"));
        Assert.True(GetPrivateField<bool>(rendered.Instance, "_hasUnsavedChanges"));
    }

    [Fact]
    public async Task WorldDetail_GetPublicUrlBase_UsesNavigationBaseUri()
    {
        var rendered = CreateRenderedSut();

        var urlBase = await InvokePrivateWithResultAsync<string>(rendered.Instance, "GetPublicUrlBase");

        Assert.EndsWith("/w/", urlBase, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_OnNameChanged_SetsUnsavedChanges()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_hasUnsavedChanges", false);

        await InvokePrivateAsync(rendered.Instance, "OnNameChanged");

        Assert.True(GetPrivateField<bool>(rendered.Instance, "_hasUnsavedChanges"));
    }

    [Fact]
    public async Task WorldDetail_OnDescriptionChanged_SetsUnsavedChanges()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_hasUnsavedChanges", false);

        await InvokePrivateAsync(rendered.Instance, "OnDescriptionChanged");

        Assert.True(GetPrivateField<bool>(rendered.Instance, "_hasUnsavedChanges"));
    }

    [Fact]
    public async Task WorldDetail_StartAddLink_InitializesAddLinkState()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isAddingLink", false);
        SetPrivateField(rendered.Instance, "_newLinkTitle", "x");
        SetPrivateField(rendered.Instance, "_newLinkUrl", "x");
        SetPrivateField(rendered.Instance, "_newLinkDescription", "x");

        await InvokePrivateAsync(rendered.Instance, "StartAddLink");

        Assert.True(GetPrivateField<bool>(rendered.Instance, "_isAddingLink"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_newLinkTitle"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_newLinkUrl"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_newLinkDescription"));
    }

    [Fact]
    public async Task WorldDetail_CancelAddLink_ResetsAddLinkState()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isAddingLink", true);
        SetPrivateField(rendered.Instance, "_newLinkTitle", "x");
        SetPrivateField(rendered.Instance, "_newLinkUrl", "x");
        SetPrivateField(rendered.Instance, "_newLinkDescription", "x");

        await InvokePrivateAsync(rendered.Instance, "CancelAddLink");

        Assert.False(GetPrivateField<bool>(rendered.Instance, "_isAddingLink"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_newLinkTitle"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_newLinkUrl"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_newLinkDescription"));
    }

    [Fact]
    public async Task WorldDetail_CancelEditLink_ResetsEditLinkState()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_editingLinkId", Guid.NewGuid());
        SetPrivateField(rendered.Instance, "_editLinkTitle", "x");
        SetPrivateField(rendered.Instance, "_editLinkUrl", "x");
        SetPrivateField(rendered.Instance, "_editLinkDescription", "x");

        await InvokePrivateAsync(rendered.Instance, "CancelEditLink");

        Assert.Null(GetPrivateField<Guid?>(rendered.Instance, "_editingLinkId"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_editLinkTitle"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_editLinkUrl"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_editLinkDescription"));
    }

    [Fact]
    public async Task WorldDetail_CancelDocumentEdit_ResetsDocumentEditState()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_editingDocumentId", Guid.NewGuid());
        SetPrivateField(rendered.Instance, "_editDocumentTitle", "x");
        SetPrivateField(rendered.Instance, "_editDocumentDescription", "x");

        await InvokePrivateAsync(rendered.Instance, "CancelDocumentEdit");

        Assert.Null(GetPrivateField<Guid?>(rendered.Instance, "_editingDocumentId"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_editDocumentTitle"));
        Assert.Equal(string.Empty, GetPrivateField<string>(rendered.Instance, "_editDocumentDescription"));
    }

    [Fact]
    public async Task WorldDetail_HandleFaviconError_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();

        var ex = await Record.ExceptionAsync(() => InvokePrivateAsync(rendered.Instance, "HandleFaviconError"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task WorldDetail_OnMembersChanged_WhenWorldNull_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", null);
        rendered.WorldApi.ClearReceivedCalls();

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnMembersChanged");

        await rendered.WorldApi.DidNotReceive().GetWorldAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task WorldDetail_OnMembersChanged_WhenWorldExists_UpdatesMemberData()
    {
        var rendered = CreateRenderedSut();
        var existing = new WorldDetailDto { Id = rendered.WorldId, Name = "World", MemberCount = 1, Members = new List<WorldMemberDto>() };
        SetPrivateField(rendered.Instance, "_world", existing);
        rendered.WorldApi.GetWorldAsync(rendered.WorldId).Returns(new WorldDetailDto
        {
            Id = rendered.WorldId,
            Name = "World",
            MemberCount = 3,
            Members = new List<WorldMemberDto> { new() { Id = Guid.NewGuid(), DisplayName = "A", Email = "a@x.com" } }
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnMembersChanged");

        Assert.Equal(3, existing.MemberCount);
        Assert.Single(existing.Members);
    }

    [Fact]
    public async Task WorldDetail_SaveWorld_WhenSlugInvalid_DoesNotCallUpdate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World" });
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", false);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveWorld");

        await rendered.WorldApi.DidNotReceive().UpdateWorldAsync(Arg.Any<Guid>(), Arg.Any<WorldUpdateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveWorld_WhenUpdateSucceeds_RefreshesAndClearsUnsaved()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World" });
        SetPrivateField(rendered.Instance, "_editName", " Updated ");
        SetPrivateField(rendered.Instance, "_editDescription", " Desc ");
        SetPrivateField(rendered.Instance, "_hasUnsavedChanges", true);
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", true);
        SetPrivateField(rendered.Instance, "_publicSlug", "my-public");
        rendered.WorldApi.UpdateWorldAsync(rendered.WorldId, Arg.Any<WorldUpdateDto>()).Returns(new WorldDetailDto
        {
            Id = rendered.WorldId,
            Name = "Updated",
            Description = "Desc",
            IsPublic = true,
            PublicSlug = "my-public"
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveWorld");

        await rendered.WorldApi.Received(1).UpdateWorldAsync(rendered.WorldId, Arg.Is<WorldUpdateDto>(d =>
            d.Name == "Updated" && d.Description == "Desc" && d.IsPublic == true && d.PublicSlug == "my-public"));
        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_hasUnsavedChanges"));
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_isSaving"));
    }

    [Fact]
    public async Task WorldDetail_SaveWorld_WhenUpdateThrows_ResetsSavingFlag()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World" });
        SetPrivateField(rendered.Instance, "_isPublic", false);
        rendered.WorldApi.UpdateWorldAsync(rendered.WorldId, Arg.Any<WorldUpdateDto>())
            .Returns(Task.FromException<WorldDto?>(new Exception("boom")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveWorld");

        Assert.False(GetPrivateField<bool>(rendered.Instance, "_isSaving"));
    }

    [Fact]
    public async Task WorldDetail_SaveWorld_WhenAlreadySaving_ReturnsWithoutUpdate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World" });
        SetPrivateField(rendered.Instance, "_isSaving", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveWorld");

        await rendered.WorldApi.DidNotReceive().UpdateWorldAsync(Arg.Any<Guid>(), Arg.Any<WorldUpdateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveWorld_WhenWorldNullAndAlreadySaving_ReturnsWithoutUpdate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_world", null);
        SetPrivateField(rendered.Instance, "_isSaving", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveWorld");

        await rendered.WorldApi.DidNotReceive().UpdateWorldAsync(Arg.Any<Guid>(), Arg.Any<WorldUpdateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveNewLink_InvalidUrl_ReturnsWithoutCreate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_newLinkTitle", "Docs");
        SetPrivateField(rendered.Instance, "_newLinkUrl", "bad-url");

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveNewLink");

        await rendered.WorldApi.DidNotReceive().CreateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<WorldLinkCreateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveNewLink_NonHttpUrl_ReturnsWithoutCreate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_newLinkTitle", "Docs");
        SetPrivateField(rendered.Instance, "_newLinkUrl", "ftp://example.com");

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveNewLink");

        await rendered.WorldApi.DidNotReceive().CreateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<WorldLinkCreateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveNewLink_WhenCreateReturnsNull_DoesNotRefresh()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_newLinkTitle", "Docs");
        SetPrivateField(rendered.Instance, "_newLinkUrl", "https://example.com");
        rendered.WorldApi.CreateWorldLinkAsync(rendered.WorldId, Arg.Any<WorldLinkCreateDto>()).Returns((WorldLinkDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveNewLink");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_SaveNewLink_WhenCreateThrows_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_newLinkTitle", "Docs");
        SetPrivateField(rendered.Instance, "_newLinkUrl", "https://example.com");
        rendered.WorldApi.CreateWorldLinkAsync(rendered.WorldId, Arg.Any<WorldLinkCreateDto>())
            .Returns(Task.FromException<WorldLinkDto?>(new Exception("boom")));

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "SaveNewLink"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task WorldDetail_SaveEditLink_MissingTitleOrUrl_ReturnsWithoutUpdate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_editingLinkId", Guid.NewGuid());
        SetPrivateField(rendered.Instance, "_editLinkTitle", "");
        SetPrivateField(rendered.Instance, "_editLinkUrl", "https://example.com");

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveEditLink");

        await rendered.WorldApi.DidNotReceive().UpdateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<WorldLinkUpdateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveEditLink_WhenEditingLinkIdMissing_ReturnsWithoutUpdate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_editingLinkId", null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveEditLink");

        await rendered.WorldApi.DidNotReceive().UpdateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<WorldLinkUpdateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveEditLink_NonHttpUrl_ReturnsWithoutUpdate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_editingLinkId", Guid.NewGuid());
        SetPrivateField(rendered.Instance, "_editLinkTitle", "Link");
        SetPrivateField(rendered.Instance, "_editLinkUrl", "ftp://example.com");

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveEditLink");

        await rendered.WorldApi.DidNotReceive().UpdateWorldLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<WorldLinkUpdateDto>());
    }

    [Fact]
    public async Task WorldDetail_SaveEditLink_WhenUpdateReturnsNull_DoesNotRefresh()
    {
        var rendered = CreateRenderedSut();
        var id = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_editingLinkId", id);
        SetPrivateField(rendered.Instance, "_editLinkTitle", "Link");
        SetPrivateField(rendered.Instance, "_editLinkUrl", "https://example.com");
        rendered.WorldApi.UpdateWorldLinkAsync(rendered.WorldId, id, Arg.Any<WorldLinkUpdateDto>()).Returns((WorldLinkDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveEditLink");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_SaveEditLink_WhenUpdateThrows_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();
        var id = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_editingLinkId", id);
        SetPrivateField(rendered.Instance, "_editLinkTitle", "Link");
        SetPrivateField(rendered.Instance, "_editLinkUrl", "https://example.com");
        rendered.WorldApi.UpdateWorldLinkAsync(rendered.WorldId, id, Arg.Any<WorldLinkUpdateDto>())
            .Returns(Task.FromException<WorldLinkDto?>(new Exception("boom")));

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "SaveEditLink"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task WorldDetail_DeleteLink_WhenDeleteSucceeds_RemovesLinkAndRefreshes()
    {
        var rendered = CreateRenderedSut();
        var link = new WorldLinkDto { Id = Guid.NewGuid(), Title = "L", Url = "https://example.com" };
        SetPrivateField(rendered.Instance, "_links", new List<WorldLinkDto> { link });
        rendered.DialogService.ShowMessageBox(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns((bool?)true);
        rendered.WorldApi.DeleteWorldLinkAsync(rendered.WorldId, link.Id).Returns(true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "DeleteLink", link);

        Assert.Empty(GetPrivateField<List<WorldLinkDto>>(rendered.Instance, "_links"));
        await rendered.TreeState.Received(1).RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_SaveDocumentEdit_WhenDocFound_UpdatesLocalDocument()
    {
        var rendered = CreateRenderedSut();
        var docId = Guid.NewGuid();
        var local = new WorldDocumentDto { Id = docId, Title = "Old", Description = "Old" };
        SetPrivateField(rendered.Instance, "_documents", new List<WorldDocumentDto> { local });
        SetPrivateField(rendered.Instance, "_editingDocumentId", docId);
        SetPrivateField(rendered.Instance, "_editDocumentTitle", "New");
        SetPrivateField(rendered.Instance, "_editDocumentDescription", "Desc");
        rendered.WorldApi.UpdateDocumentAsync(rendered.WorldId, docId, Arg.Any<WorldDocumentUpdateDto>()).Returns(new WorldDocumentDto
        {
            Id = docId,
            Title = "New",
            Description = "Desc"
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveDocumentEdit");

        Assert.Equal("New", local.Title);
        Assert.Equal("Desc", local.Description);
    }

    [Fact]
    public async Task WorldDetail_SaveDocumentEdit_WhenUpdateThrows_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();
        var docId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_editingDocumentId", docId);
        SetPrivateField(rendered.Instance, "_documents", new List<WorldDocumentDto> { new() { Id = docId, Title = "T" } });
        rendered.WorldApi.UpdateDocumentAsync(rendered.WorldId, docId, Arg.Any<WorldDocumentUpdateDto>())
            .Returns(Task.FromException<WorldDocumentDto?>(new Exception("boom")));

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "SaveDocumentEdit"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task WorldDetail_DeleteDocument_WhenConfirmedAndDeleteSucceeds_RemovesAndRefreshes()
    {
        var rendered = CreateRenderedSut();
        var docId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_documents", new List<WorldDocumentDto> { new() { Id = docId, Title = "Doc" } });
        rendered.DialogService.ShowMessageBox(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns((bool?)true);
        rendered.WorldApi.DeleteDocumentAsync(rendered.WorldId, docId).Returns(true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "DeleteDocument", docId);

        Assert.Empty(GetPrivateField<List<WorldDocumentDto>>(rendered.Instance, "_documents"));
        await rendered.TreeState.Received(1).RefreshAsync();
    }

    [Fact]
    public async Task WorldDetail_DownloadDocument_WhenDownloadExists_OpensUrl()
    {
        var rendered = CreateRenderedSut();
        var docId = Guid.NewGuid();
        rendered.WorldApi.DownloadDocumentAsync(docId).Returns(new DocumentDownloadResult(
            "https://example.com/download",
            "doc.pdf",
            "application/pdf",
            10));

        await InvokePrivateOnRendererAsync(rendered.Cut, "DownloadDocument", docId);

        await rendered.JsRuntime.Received(1).InvokeVoidAsync("open", Arg.Any<object?[]>());
    }

    [Fact]
    public async Task WorldDetail_LoadWorldAsync_WhenWorldNull_NavigatesDashboard()
    {
        var rendered = CreateRenderedSut();
        rendered.WorldApi.GetWorldAsync(rendered.WorldId).Returns((WorldDetailDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadWorldAsync");

        Assert.EndsWith("/dashboard", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_LoadWorldAsync_WhenWorldApiThrows_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();
        rendered.WorldApi.GetWorldAsync(rendered.WorldId).Returns(Task.FromException<WorldDetailDto?>(new Exception("boom")));

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "LoadWorldAsync"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task WorldDetail_LoadWorldAsync_WhenEmailMatchesMember_SetsCurrentUserAndGm()
    {
        var provider = new TestAuthStateProvider(true, "gm@example.com");
        var rendered = CreateRenderedSut(provider);
        var userId = Guid.NewGuid();
        rendered.WorldApi.GetWorldAsync(rendered.WorldId).Returns(new WorldDetailDto
        {
            Id = rendered.WorldId,
            Name = "World",
            Slug = "world",
            Members = new List<WorldMemberDto>
            {
                new() { UserId = userId, Email = "gm@example.com", Role = Chronicis.Shared.Enums.WorldRole.GM }
            }
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadWorldAsync");

        Assert.Equal(userId, GetPrivateField<Guid>(rendered.Instance, "_currentUserId"));
        Assert.True(GetPrivateField<bool>(rendered.Instance, "_isCurrentUserGM"));
    }

    [Fact]
    public async Task WorldDetail_LoadWorldAsync_WhenOnlyStandardEmailClaimPresent_SetsCurrentUser()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "member@example.com")
        ], "TestAuth"));
        var provider = new TestAuthStateProvider(principal);
        var rendered = CreateRenderedSut(provider);
        var userId = Guid.NewGuid();
        rendered.WorldApi.GetWorldAsync(rendered.WorldId).Returns(new WorldDetailDto
        {
            Id = rendered.WorldId,
            Name = "World",
            Slug = "world",
            Members = new List<WorldMemberDto>
            {
                new() { UserId = userId, Email = "member@example.com", Role = Chronicis.Shared.Enums.WorldRole.Player }
            }
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadWorldAsync");

        Assert.Equal(userId, GetPrivateField<Guid>(rendered.Instance, "_currentUserId"));
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_isCurrentUserGM"));
    }

    [Fact]
    public async Task WorldDetail_LoadWorldAsync_WhenOnlyPlainEmailClaimPresent_SetsCurrentUser()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("email", "plain@example.com")
        ], "TestAuth"));
        var provider = new TestAuthStateProvider(principal);
        var rendered = CreateRenderedSut(provider);
        var userId = Guid.NewGuid();
        rendered.WorldApi.GetWorldAsync(rendered.WorldId).Returns(new WorldDetailDto
        {
            Id = rendered.WorldId,
            Name = "World",
            Slug = "world",
            Members = new List<WorldMemberDto>
            {
                new() { UserId = userId, Email = "plain@example.com", Role = Chronicis.Shared.Enums.WorldRole.Player }
            }
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadWorldAsync");

        Assert.Equal(userId, GetPrivateField<Guid>(rendered.Instance, "_currentUserId"));
        Assert.False(GetPrivateField<bool>(rendered.Instance, "_isCurrentUserGM"));
    }

    [Fact]
    public void WorldDetail_Render_ResourcesTab_ShowsLinkAndDocumentBranches()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isLoading", false);
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World", Slug = "world" });
        SetPrivateField(rendered.Instance, "_activeTabIndex", 0);
        SetPrivateField(rendered.Instance, "_isCurrentUserGM", true);
        SetPrivateField(rendered.Instance, "_isAddingLink", true);
        var editingLinkId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_editingLinkId", editingLinkId);
        SetPrivateField(rendered.Instance, "_links", new List<WorldLinkDto>
        {
            new() { Id = editingLinkId, Title = "Roll20", Url = "https://roll20.net", Description = "editing row" },
            new() { Id = Guid.NewGuid(), Title = "DDB", Url = "https://dndbeyond.com", Description = "Game table" }
        });
        var editDocId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_editingDocumentId", editDocId);
        SetPrivateField(rendered.Instance, "_documents", new List<WorldDocumentDto>
        {
            new() { Id = editDocId, Title = "Editable", ContentType = "text/plain", FileSizeBytes = 10, UploadedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Title = "Viewable", Description = "desc", ContentType = "application/pdf", FileSizeBytes = 20, UploadedAt = DateTime.UtcNow }
        });
        rendered.Cut.Render();

        Assert.Contains("External Links", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Documents", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Upload", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorldDetail_Render_MembersTab_ShowsPublicSharingBranches()
    {
        var rendered = CreateRenderedSut();
        ComponentFactories.Add(t => t == typeof(WorldMembersPanel), _ => new StubPanel("members-stub"));
        SetPrivateField(rendered.Instance, "_isLoading", false);
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = true, PublicSlug = "slug" });
        SetPrivateField(rendered.Instance, "_activeTabIndex", 1);
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_publicSlug", "slug");
        SetPrivateField(rendered.Instance, "_slugIsAvailable", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        rendered.Cut.Render();

        Assert.Contains("Public Sharing", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("members-stub", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorldDetail_Render_SettingsTab_WhenNotGm_ShowsInfoAlert()
    {
        var rendered = CreateRenderedSut();
        ComponentFactories.Add(t => t == typeof(WorldResourceProviders), _ => new StubPanel("settings-stub"));
        SetPrivateField(rendered.Instance, "_isLoading", false);
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World", Slug = "world" });
        SetPrivateField(rendered.Instance, "_activeTabIndex", 2);
        SetPrivateField(rendered.Instance, "_isCurrentUserGM", false);
        rendered.Cut.Render();

        Assert.Contains("Only the world owner can manage settings.", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorldDetail_Render_ResourcesTab_WithDeterministicTabs_CoversRowBranches()
    {
        EnableDeterministicTabRendering();
        ComponentFactories.Add(t => t == typeof(WorldMembersPanel), _ => new StubPanel("members-stub"));
        ComponentFactories.Add(t => t == typeof(WorldResourceProviders), _ => new StubPanel("settings-stub"));
        var editingLinkId = Guid.NewGuid();
        var editDocId = Guid.NewGuid();
        var provider = new TestAuthStateProvider(true, "gm@example.com");
        var rendered = CreateRenderedSut(provider, (api, worldId) =>
        {
            api.GetWorldAsync(worldId).Returns(new WorldDetailDto
            {
                Id = worldId,
                Name = "World",
                Slug = "world",
                Members = new List<WorldMemberDto>
                {
                    new() { UserId = Guid.NewGuid(), Email = "gm@example.com", Role = Chronicis.Shared.Enums.WorldRole.GM }
                }
            });
            api.GetWorldLinksAsync(worldId).Returns(new List<WorldLinkDto>
            {
                new() { Id = editingLinkId, Title = "Editing", Url = "https://editing.example.com", Description = "editing row" },
                new() { Id = Guid.NewGuid(), Title = "View", Url = "https://view.example.com", Description = "view row" }
            });
            api.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto>
            {
                new() { Id = editDocId, Title = "Editable", ContentType = "text/plain", FileSizeBytes = 10, UploadedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Title = "Viewable", Description = "desc", ContentType = "application/pdf", FileSizeBytes = 20, UploadedAt = DateTime.UtcNow }
            });
        });
        rendered.Cut.WaitForAssertion(() => Assert.Contains("External Links", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase));

        SetPrivateField(rendered.Instance, "_editingDocumentId", editDocId);
        SetPrivateField(rendered.Instance, "_editingLinkId", editingLinkId);

        rendered.Cut.Render();

        Assert.Contains("External Links", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Add Link", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Upload", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorldDetail_Render_MembersAndSharing_WithDeterministicTabs_CoversSharingBranches()
    {
        EnableDeterministicTabRendering();
        ComponentFactories.Add(t => t == typeof(WorldMembersPanel), _ => new StubPanel("members-stub"));
        ComponentFactories.Add(t => t == typeof(WorldResourceProviders), _ => new StubPanel("settings-stub"));
        var rendered = CreateRenderedSut(configureWorldApi: (api, worldId) =>
        {
            api.GetWorldAsync(worldId).Returns(new WorldDetailDto
            {
                Id = worldId,
                Name = "World",
                Slug = "world",
                IsPublic = true,
                PublicSlug = "slug",
                Members = new List<WorldMemberDto>()
            });
        });
        SetPrivateField(rendered.Instance, "_isLoading", false);
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", true);
        SetPrivateField(rendered.Instance, "_publicSlug", "slug");
        SetPrivateField(rendered.Instance, "_slugIsAvailable", false);

        rendered.Cut.Render();

        Assert.Contains("members-stub", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Public Sharing", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preview", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorldDetail_Render_MembersAndSharing_WhenSlugAvailableButEmpty_DoesNotShowSuccessIcon()
    {
        EnableDeterministicTabRendering();
        ComponentFactories.Add(t => t == typeof(WorldMembersPanel), _ => new StubPanel("members-stub"));
        ComponentFactories.Add(t => t == typeof(WorldResourceProviders), _ => new StubPanel("settings-stub"));
        var rendered = CreateRenderedSut(configureWorldApi: (api, worldId) =>
        {
            api.GetWorldAsync(worldId).Returns(new WorldDetailDto
            {
                Id = worldId,
                Name = "World",
                Slug = "world",
                IsPublic = true,
                PublicSlug = "slug",
                Members = new List<WorldMemberDto>()
            });
        });
        SetPrivateField(rendered.Instance, "_isLoading", false);
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", true);
        SetPrivateField(rendered.Instance, "_publicSlug", string.Empty);

        rendered.Cut.Render();

        Assert.DoesNotContain("check_circle", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorldDetail_Render_MembersAndSharing_WhenWorldNotPublic_DoesNotShowPreviewRow()
    {
        EnableDeterministicTabRendering();
        ComponentFactories.Add(t => t == typeof(WorldMembersPanel), _ => new StubPanel("members-stub"));
        ComponentFactories.Add(t => t == typeof(WorldResourceProviders), _ => new StubPanel("settings-stub"));
        var rendered = CreateRenderedSut(configureWorldApi: (api, worldId) =>
        {
            api.GetWorldAsync(worldId).Returns(new WorldDetailDto
            {
                Id = worldId,
                Name = "World",
                Slug = "world",
                IsPublic = false,
                PublicSlug = "slug",
                Members = new List<WorldMemberDto>()
            });
        });
        SetPrivateField(rendered.Instance, "_isLoading", false);
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", false);
        SetPrivateField(rendered.Instance, "_publicSlug", "slug");

        rendered.Cut.Render();

        Assert.DoesNotContain("Preview", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorldDetail_Render_MembersAndSharing_WhenWorldPublicSlugEmpty_DoesNotShowPreviewRow()
    {
        EnableDeterministicTabRendering();
        ComponentFactories.Add(t => t == typeof(WorldMembersPanel), _ => new StubPanel("members-stub"));
        ComponentFactories.Add(t => t == typeof(WorldResourceProviders), _ => new StubPanel("settings-stub"));
        var rendered = CreateRenderedSut(configureWorldApi: (api, worldId) =>
        {
            api.GetWorldAsync(worldId).Returns(new WorldDetailDto
            {
                Id = worldId,
                Name = "World",
                Slug = "world",
                IsPublic = true,
                PublicSlug = string.Empty,
                Members = new List<WorldMemberDto>()
            });
        });
        SetPrivateField(rendered.Instance, "_isLoading", false);
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", false);
        SetPrivateField(rendered.Instance, "_publicSlug", "slug");

        rendered.Cut.Render();

        Assert.DoesNotContain("Preview", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorldDetail_BuildRenderTreeClosure_SharingSlugAvailableButEmpty_ExecutesConditionPath()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", true);
        SetPrivateField(rendered.Instance, "_publicSlug", string.Empty);

        InvokePrivateRenderFragment(rendered.Instance, "<BuildRenderTree>b__0_10");
    }

    [Fact]
    public void WorldDetail_BuildRenderTreeClosure_WorldNotPublic_ExecutesConditionPath()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", false);
        SetPrivateField(rendered.Instance, "_publicSlug", "slug");
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = false, PublicSlug = "slug" });

        InvokePrivateRenderFragment(rendered.Instance, "<BuildRenderTree>b__0_10");
    }

    [Fact]
    public void WorldDetail_BuildRenderTreeClosure_WorldPublicSlugEmpty_ExecutesConditionPath()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", false);
        SetPrivateField(rendered.Instance, "_publicSlug", "slug");
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = true, PublicSlug = string.Empty });

        InvokePrivateRenderFragment(rendered.Instance, "<BuildRenderTree>b__0_10");
    }

    [Fact]
    public void WorldDetail_BuildRenderTreeClosure_WorldNull_ExecutesConditionPath()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", false);
        SetPrivateField(rendered.Instance, "_publicSlug", "slug");
        SetPrivateField(rendered.Instance, "_world", null);

        InvokePrivateRenderFragment(rendered.Instance, "<BuildRenderTree>b__0_10");
    }

    [Fact]
    public void WorldDetail_BuildRenderTreeClosure_WorldPublicSlugNull_ExecutesConditionPath()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", false);
        SetPrivateField(rendered.Instance, "_publicSlug", "slug");
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = true, PublicSlug = null });

        InvokePrivateRenderFragment(rendered.Instance, "<BuildRenderTree>b__0_10");
    }

    [Fact]
    public void WorldDetail_BuildRenderTreeClosure_WorldPublicWithSlug_ExecutesConditionPath()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isPublic", true);
        SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
        SetPrivateField(rendered.Instance, "_slugIsAvailable", false);
        SetPrivateField(rendered.Instance, "_publicSlug", "slug");
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = true, PublicSlug = "public-world" });

        InvokePrivateRenderFragment(rendered.Instance, "<BuildRenderTree>b__0_10");
    }

    [Fact]
    public void WorldDetail_BuildRenderTreeClosure_SharingCondition_BruteForceMatrix()
    {
        var rendered = CreateRenderedSut();
        var worlds = new WorldDetailDto?[]
        {
            null,
            new() { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = false, PublicSlug = null },
            new() { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = false, PublicSlug = string.Empty },
            new() { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = false, PublicSlug = "slug" },
            new() { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = true, PublicSlug = null },
            new() { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = true, PublicSlug = string.Empty },
            new() { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = true, PublicSlug = "slug" },
            new() { Id = rendered.WorldId, Name = "World", Slug = "world", IsPublic = true, PublicSlug = " " }
        };
        var slugs = new[] { string.Empty, "slug", " " };

        foreach (var world in worlds)
        {
            foreach (var publicSlug in slugs)
            {
                SetPrivateField(rendered.Instance, "_isPublic", true);
                SetPrivateField(rendered.Instance, "_isCheckingSlug", false);
                SetPrivateField(rendered.Instance, "_slugIsAvailable", false);
                SetPrivateField(rendered.Instance, "_publicSlug", publicSlug);
                SetPrivateField(rendered.Instance, "_world", world);

                InvokePrivateRenderFragment(rendered.Instance, "<BuildRenderTree>b__0_10");
            }
        }
    }

    [Fact]
    public void WorldDetail_Render_SettingsTab_WhenGm_WithDeterministicTabs_ShowsProviders()
    {
        EnableDeterministicTabRendering();
        ComponentFactories.Add(t => t == typeof(WorldMembersPanel), _ => new StubPanel("members-stub"));
        ComponentFactories.Add(t => t == typeof(WorldResourceProviders), _ => new StubPanel("settings-gm-stub"));
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isLoading", false);
        SetPrivateField(rendered.Instance, "_world", new WorldDetailDto { Id = rendered.WorldId, Name = "World", Slug = "world" });
        SetPrivateField(rendered.Instance, "_isCurrentUserGM", true);

        rendered.Cut.Render();

        Assert.Contains("settings-gm-stub", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WorldDetail_Render_ResourcesTab_WithDeterministicTabs_InvokesLinkRowCallbacks()
    {
        EnableDeterministicTabRendering();
        ComponentFactories.Add(t => t == typeof(WorldMembersPanel), _ => new StubPanel("members-stub"));
        ComponentFactories.Add(t => t == typeof(WorldResourceProviders), _ => new StubPanel("settings-stub"));
        var linkId = Guid.NewGuid();
        var provider = new TestAuthStateProvider(true, "gm@example.com");
        var rendered = CreateRenderedSut(provider, (api, worldId) =>
        {
            api.GetWorldAsync(worldId).Returns(new WorldDetailDto
            {
                Id = worldId,
                Name = "World",
                Slug = "world",
                Members = new List<WorldMemberDto>
                {
                    new() { UserId = Guid.NewGuid(), Email = "gm@example.com", Role = Chronicis.Shared.Enums.WorldRole.GM }
                }
            });
            api.GetWorldLinksAsync(worldId).Returns(new List<WorldLinkDto>
            {
                new() { Id = linkId, Title = "Link", Url = "https://example.com", Description = "desc" }
            });
            api.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto>());
        });
        rendered.DialogService.ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DialogOptions>()).Returns((bool?)false);
        rendered.Cut.WaitForAssertion(() => Assert.Contains("External Links", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase));

        var linkDelete = rendered.Cut.FindComponents<MudIconButton>().Single(x => x.Instance.Icon == Icons.Material.Filled.Delete);
        await rendered.Cut.InvokeAsync(() => linkDelete.Instance.OnClick.InvokeAsync());

        var linkEdit = rendered.Cut.FindComponents<MudIconButton>().Single(x => x.Instance.Icon == Icons.Material.Filled.Edit);
        await rendered.Cut.InvokeAsync(() => linkEdit.Instance.OnClick.InvokeAsync());
    }

    [Fact]
    public async Task WorldDetail_Render_ResourcesTab_WithDeterministicTabs_InvokesDocumentRowCallbacks()
    {
        EnableDeterministicTabRendering();
        ComponentFactories.Add(t => t == typeof(WorldMembersPanel), _ => new StubPanel("members-stub"));
        ComponentFactories.Add(t => t == typeof(WorldResourceProviders), _ => new StubPanel("settings-stub"));
        var docId = Guid.NewGuid();
        var provider = new TestAuthStateProvider(true, "gm@example.com");
        var rendered = CreateRenderedSut(provider, (api, worldId) =>
        {
            api.GetWorldAsync(worldId).Returns(new WorldDetailDto
            {
                Id = worldId,
                Name = "World",
                Slug = "world",
                Members = new List<WorldMemberDto>
                {
                    new() { UserId = Guid.NewGuid(), Email = "gm@example.com", Role = Chronicis.Shared.Enums.WorldRole.GM }
                }
            });
            api.GetWorldLinksAsync(worldId).Returns(new List<WorldLinkDto>());
            api.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto>
            {
                new() { Id = docId, Title = "Doc", Description = "desc", ContentType = "application/pdf", FileSizeBytes = 20, UploadedAt = DateTime.UtcNow }
            });
            api.DownloadDocumentAsync(docId).Returns(new DocumentDownloadResult(
                "https://example.com/download",
                "doc.pdf",
                "application/pdf",
                10));
        });
        rendered.DialogService.ShowMessageBox(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DialogOptions>()).Returns((bool?)false);
        rendered.Cut.WaitForAssertion(() => Assert.Contains("Documents", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase));

        var download = rendered.Cut.FindComponents<MudIconButton>().Single(x => x.Instance.Icon == Icons.Material.Filled.Download);
        await rendered.Cut.InvokeAsync(() => download.Instance.OnClick.InvokeAsync());

        var deletes = rendered.Cut.FindComponents<MudIconButton>().Where(x => x.Instance.Icon == Icons.Material.Filled.Delete).ToList();
        var documentDelete = deletes.Last();
        await rendered.Cut.InvokeAsync(() => documentDelete.Instance.OnClick.InvokeAsync());

        var edits = rendered.Cut.FindComponents<MudIconButton>().Where(x => x.Instance.Icon == Icons.Material.Filled.Edit).ToList();
        var documentEdit = edits.Last();
        await rendered.Cut.InvokeAsync(() => documentEdit.Instance.OnClick.InvokeAsync());
    }

    private void EnableDeterministicTabRendering()
    {
        ComponentFactories.Add(t => t == typeof(MudTabs), _ => new MudTabsStub());
        ComponentFactories.Add(t => t == typeof(MudTabPanel), _ => new MudTabPanelStub());
    }

    private RenderedContext CreateRenderedSut(AuthenticationStateProvider? authStateProviderOverride = null, Action<IWorldApiService, Guid>? configureWorldApi = null)
    {
        var worldApi = Substitute.For<IWorldApiService>();
        var campaignApi = Substitute.For<ICampaignApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        var snackbar = Substitute.For<ISnackbar>();
        var dialogService = Substitute.For<IDialogService>();
        var jsRuntime = Substitute.For<IJSRuntime>();
        var authStateProvider = authStateProviderOverride ?? new TestAuthStateProvider();
        var worldId = Guid.NewGuid();

        worldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto
        {
            Id = worldId,
            Name = "World",
            Slug = "world",
            Members = new List<WorldMemberDto>()
        });
        worldApi.GetWorldLinksAsync(worldId).Returns(new List<WorldLinkDto>());
        worldApi.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto>());
        configureWorldApi?.Invoke(worldApi, worldId);

        Services.AddSingleton(worldApi);
        Services.AddSingleton(campaignApi);
        Services.AddSingleton(treeState);
        Services.AddSingleton(breadcrumbService);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(dialogService);
        Services.AddSingleton(jsRuntime);
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);

        var cut = RenderComponent<WorldDetail>(parameters => parameters.Add(p => p.WorldId, worldId));
        var navigation = Services.GetRequiredService<NavigationManager>();

        return new RenderedContext(cut, cut.Instance, worldApi, dialogService, breadcrumbService, treeState, jsRuntime, navigation, worldId);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.NotNull(method);
        var target = method!.IsStatic ? null : instance;
        var result = method.Invoke(target, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<WorldDetail> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Assert.NotNull(method);
            var target = method!.IsStatic ? null : cut.Instance;
            var result = method.Invoke(target, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }

    private static async Task<T> InvokePrivateWithResultAsync<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.NotNull(method);
        var target = method!.IsStatic ? null : instance;
        var result = method.Invoke(target, args);

        if (result is Task<T> taskOfT)
        {
            return await taskOfT;
        }

        return (T)result!;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }

    private static void InvokePrivateRenderFragment(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var builder = new Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder();
        method!.Invoke(instance, new object[] { builder });
    }

    private sealed class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly bool _isAuthenticated;
        private readonly string? _email;
        private readonly ClaimsPrincipal? _principalOverride;

        public TestAuthStateProvider(bool isAuthenticated = false, string? email = null)
        {
            _isAuthenticated = isAuthenticated;
            _email = email;
        }

        public TestAuthStateProvider(ClaimsPrincipal principal)
        {
            _principalOverride = principal;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_principalOverride != null)
            {
                return Task.FromResult(new AuthenticationState(_principalOverride));
            }

            var claims = new List<Claim>();
            if (!string.IsNullOrWhiteSpace(_email))
            {
                claims.Add(new Claim("https://chronicis.app/email", _email));
            }

            var identity = _isAuthenticated
                ? new ClaimsIdentity(claims, "TestAuth")
                : new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(principal));
        }
    }

    private sealed class StubPanel(string content) : ComponentBase
    {
        [Parameter] public Guid WorldId { get; set; }
        [Parameter] public Guid CurrentUserId { get; set; }
        [Parameter] public bool IsCurrentUserGM { get; set; }
        [Parameter] public EventCallback OnMembersChanged { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, content);
            builder.CloseElement();
        }
    }

    private sealed class MudTabsStub : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? Attributes { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-test", "mud-tabs-stub");
            builder.AddContent(2, ChildContent);
            builder.CloseElement();
        }
    }

    private sealed class MudTabPanelStub : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? Attributes { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "section");
            builder.AddAttribute(1, "data-test", "mud-tab-panel-stub");
            builder.AddContent(2, ChildContent);
            builder.CloseElement();
        }
    }

    private sealed record RenderedContext(
        IRenderedComponent<WorldDetail> Cut,
        WorldDetail Instance,
        IWorldApiService WorldApi,
        IDialogService DialogService,
        IBreadcrumbService BreadcrumbService,
        ITreeStateService TreeState,
        IJSRuntime JsRuntime,
        NavigationManager Navigation,
        Guid WorldId);
}
