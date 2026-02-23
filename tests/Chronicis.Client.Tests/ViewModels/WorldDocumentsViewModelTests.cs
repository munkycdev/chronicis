using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class WorldDocumentsViewModelTests
{
    private sealed record Sut(
        WorldDocumentsViewModel Vm,
        IWorldApiService WorldApi,
        ITreeStateService TreeState,
        IUserNotifier Notifier,
        IDialogService DialogService,
        IAppNavigator Navigator);

    private static Sut CreateSut()
    {
        var worldApi = Substitute.For<IWorldApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var notifier = Substitute.For<IUserNotifier>();
        var dialogService = Substitute.For<IDialogService>();
        var navigator = Substitute.For<IAppNavigator>();
        var logger = Substitute.For<ILogger<WorldDocumentsViewModel>>();
        var vm = new WorldDocumentsViewModel(worldApi, treeState, notifier, dialogService, navigator, logger);
        return new Sut(vm, worldApi, treeState, notifier, dialogService, navigator);
    }

    private static WorldDocumentDto MakeDoc(string title = "Handbook", string contentType = "application/pdf") =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            ContentType = contentType,
            FileSizeBytes = 1024,
            UploadedAt = DateTime.UtcNow
        };

    // ---------------------------------------------------------------------------
    // LoadAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_PopulatesDocuments()
    {
        var c = CreateSut();
        var docs = new List<WorldDocumentDto> { MakeDoc() };
        c.WorldApi.GetWorldDocumentsAsync(Arg.Any<Guid>()).Returns(docs);

        await c.Vm.LoadAsync(Guid.NewGuid());

        Assert.Equal(docs, c.Vm.Documents);
    }

    // ---------------------------------------------------------------------------
    // StartEditDocument / CancelDocumentEdit
    // ---------------------------------------------------------------------------

    [Fact]
    public void StartEditDocument_SetsEditingState()
    {
        var c = CreateSut();
        var doc = new WorldDocumentDto { Id = Guid.NewGuid(), Title = "Guide", Description = "desc", ContentType = "application/pdf", FileSizeBytes = 0, UploadedAt = DateTime.UtcNow };
        c.Vm.StartEditDocument(doc);

        Assert.Equal(doc.Id, c.Vm.EditingDocumentId);
        Assert.Equal("Guide", c.Vm.EditDocumentTitle);
        Assert.Equal("desc", c.Vm.EditDocumentDescription);
    }

    [Fact]
    public void CancelDocumentEdit_ClearsEditingState()
    {
        var c = CreateSut();
        c.Vm.StartEditDocument(MakeDoc());
        c.Vm.CancelDocumentEdit();

        Assert.Null(c.Vm.EditingDocumentId);
        Assert.Empty(c.Vm.EditDocumentTitle);
    }

    // ---------------------------------------------------------------------------
    // SaveDocumentEditAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SaveDocumentEditAsync_WhenNoEditingId_DoesNothing()
    {
        var c = CreateSut();
        await c.Vm.SaveDocumentEditAsync();
        await c.WorldApi.DidNotReceive().UpdateDocumentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<WorldDocumentUpdateDto>());
    }

    [Fact]
    public async Task SaveDocumentEditAsync_OnSuccess_UpdatesLocalDocAndClearsState()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        var doc = MakeDoc();
        c.WorldApi.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto> { doc });
        await c.Vm.LoadAsync(worldId);
        c.Vm.StartEditDocument(doc);
        c.Vm.EditDocumentTitle = "Updated";

        var updatedDoc = new WorldDocumentDto { Id = doc.Id, Title = "Updated", ContentType = doc.ContentType, FileSizeBytes = doc.FileSizeBytes, UploadedAt = doc.UploadedAt };
        c.WorldApi.UpdateDocumentAsync(worldId, doc.Id, Arg.Any<WorldDocumentUpdateDto>()).Returns(updatedDoc);

        await c.Vm.SaveDocumentEditAsync();

        Assert.Null(c.Vm.EditingDocumentId);
        Assert.Equal("Updated", c.Vm.Documents.First().Title);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveDocumentEditAsync_WhenApiThrows_ShowsErrorAndClearsFlag()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        var doc = MakeDoc();
        c.WorldApi.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto> { doc });
        await c.Vm.LoadAsync(worldId);
        c.Vm.StartEditDocument(doc);
        c.WorldApi.UpdateDocumentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<WorldDocumentUpdateDto>()).ThrowsAsync(new Exception("fail"));

        await c.Vm.SaveDocumentEditAsync();

        Assert.False(c.Vm.IsSavingDocument);
        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // DeleteDocumentAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task DeleteDocumentAsync_WhenDocNotFound_DoesNothing()
    {
        var c = CreateSut();
        c.WorldApi.GetWorldDocumentsAsync(Arg.Any<Guid>()).Returns(new List<WorldDocumentDto>());
        await c.Vm.LoadAsync(Guid.NewGuid());
        await c.Vm.DeleteDocumentAsync(Guid.NewGuid());
        await c.WorldApi.DidNotReceive().DeleteDocumentAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteDocumentAsync_OnSuccess_RemovesFromList()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        var doc = MakeDoc();
        c.WorldApi.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto> { doc });
        await c.Vm.LoadAsync(worldId);
        c.WorldApi.DeleteDocumentAsync(worldId, doc.Id).Returns(true);

        await c.Vm.DeleteDocumentAsync(doc.Id);

        Assert.Empty(c.Vm.Documents);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenApiFails_ShowsError()
    {
        var c = CreateSut();
        var worldId = Guid.NewGuid();
        var doc = MakeDoc();
        c.WorldApi.GetWorldDocumentsAsync(worldId).Returns(new List<WorldDocumentDto> { doc });
        await c.Vm.LoadAsync(worldId);
        c.WorldApi.DeleteDocumentAsync(worldId, doc.Id).Returns(false);

        await c.Vm.DeleteDocumentAsync(doc.Id);

        c.Notifier.Received(1).Error(Arg.Any<string>());
        Assert.Single(c.Vm.Documents);
    }

    // ---------------------------------------------------------------------------
    // DownloadDocumentAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task DownloadDocumentAsync_WhenApiReturnsNull_ShowsError()
    {
        var c = CreateSut();
        c.WorldApi.DownloadDocumentAsync(Arg.Any<Guid>()).Returns((DocumentDownloadResult?)null);

        await c.Vm.DownloadDocumentAsync(Guid.NewGuid());

        c.Notifier.Received(1).Error(Arg.Any<string>());
        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    [Fact]
    public async Task DownloadDocumentAsync_OnSuccess_NavigatesToDownloadUrl()
    {
        var c = CreateSut();
        var result = new DocumentDownloadResult("https://blob.example.com/file.pdf", "file.pdf", "application/pdf", 1024);
        c.WorldApi.DownloadDocumentAsync(Arg.Any<Guid>()).Returns(result);

        await c.Vm.DownloadDocumentAsync(Guid.NewGuid());

        c.Navigator.Received(1).NavigateTo("https://blob.example.com/file.pdf");
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task DownloadDocumentAsync_WhenApiThrows_ShowsError()
    {
        var c = CreateSut();
        c.WorldApi.DownloadDocumentAsync(Arg.Any<Guid>()).ThrowsAsync(new Exception("network"));

        await c.Vm.DownloadDocumentAsync(Guid.NewGuid());

        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // GetDocumentIcon (static)
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("application/pdf", Icons.Material.Filled.PictureAsPdf)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", Icons.Material.Filled.Description)]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Icons.Material.Filled.TableChart)]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation", Icons.Material.Filled.Slideshow)]
    [InlineData("text/plain", Icons.Material.Filled.TextSnippet)]
    [InlineData("text/markdown", Icons.Material.Filled.Article)]
    [InlineData("image/png", Icons.Material.Filled.Image)]
    [InlineData("application/octet-stream", Icons.Material.Filled.InsertDriveFile)]
    public void GetDocumentIcon_ReturnsExpectedIcon(string contentType, string expectedIcon)
    {
        Assert.Equal(expectedIcon, WorldDocumentsViewModel.GetDocumentIcon(contentType));
    }

    // ---------------------------------------------------------------------------
    // FormatFileSize (static)
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void FormatFileSize_ReturnsExpectedString(long bytes, string expected)
    {
        Assert.Equal(expected, WorldDocumentsViewModel.FormatFileSize(bytes));
    }
}
