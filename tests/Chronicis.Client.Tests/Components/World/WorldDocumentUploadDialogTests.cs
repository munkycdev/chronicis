using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.World;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.World;

[ExcludeFromCodeCoverage]
public class WorldDocumentUploadDialogTests : MudBlazorTestContext
{
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();

    public WorldDocumentUploadDialogTests()
    {
        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_snackbar);
    }

    [Theory]
    [InlineData(512L, "512 B")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    public void FormatFileSize_ReturnsExpectedUnits(long bytes, string expected)
    {
        var method = typeof(WorldDocumentUploadDialog)
            .GetMethod("FormatFileSize", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var result = (string?)method!.Invoke(null, [bytes]);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Upload_WhenNoFile_DoesNotCallApi()
    {
        var cut = RenderComponent<WorldDocumentUploadDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));

        await InvokePrivateOnRendererAsync(cut, "Upload");

        await _worldApi.DidNotReceive().RequestDocumentUploadAsync(Arg.Any<Guid>(), Arg.Any<WorldDocumentUploadRequestDto>());
    }

    [Fact]
    public async Task Upload_WhenRequestUploadReturnsNull_ResetsState()
    {
        var worldId = Guid.NewGuid();
        _worldApi.RequestDocumentUploadAsync(worldId, Arg.Any<WorldDocumentUploadRequestDto>())
            .Returns((WorldDocumentUploadResponseDto?)null);

        var cut = RenderComponent<WorldDocumentUploadDialog>(p => p.Add(x => x.WorldId, worldId));
        SetField(cut.Instance, "_selectedFile", CreateBrowserFile("notes.txt", "text/plain", 3));
        SetField(cut.Instance, "_fileData", new byte[] { 1, 2, 3 });

        await InvokePrivateOnRendererAsync(cut, "Upload");

        await _worldApi.Received(1).RequestDocumentUploadAsync(worldId, Arg.Any<WorldDocumentUploadRequestDto>());
        Assert.False(GetField<bool>(cut.Instance, "_isUploading"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_uploadStatus"));
    }

    [Fact]
    public async Task Upload_WhenBlobUploadThrows_ShowsError()
    {
        var worldId = Guid.NewGuid();
        _worldApi.RequestDocumentUploadAsync(worldId, Arg.Any<WorldDocumentUploadRequestDto>())
            .Returns(new WorldDocumentUploadResponseDto
            {
                DocumentId = Guid.NewGuid(),
                UploadUrl = "::invalid-url::",
                Title = "notes"
            });

        var cut = RenderComponent<WorldDocumentUploadDialog>(p => p.Add(x => x.WorldId, worldId));
        SetField(cut.Instance, "_selectedFile", CreateBrowserFile("notes.txt", "text/plain", 3));
        SetField(cut.Instance, "_fileData", new byte[] { 1, 2, 3 });

        await InvokePrivateOnRendererAsync(cut, "Upload");

        await _worldApi.DidNotReceive().ConfirmDocumentUploadAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Upload failed", StringComparison.OrdinalIgnoreCase)), Severity.Error);
        Assert.False(GetField<bool>(cut.Instance, "_isUploading"));
    }

    [Fact]
    public async Task OnFileSelected_WhenTooLarge_SetsValidationAndClearsFile()
    {
        var cut = RenderComponent<WorldDocumentUploadDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        var file = CreateBrowserFile("notes.txt", "text/plain", 250L * 1024 * 1024);

        await InvokePrivateOnRendererAsync(cut, "OnFileSelected", CreateInputFileChangeArgs(file));

        Assert.Contains("exceeds maximum of 200 MB", GetField<string>(cut.Instance, "_validationError"), StringComparison.OrdinalIgnoreCase);
        Assert.Null(GetField<IBrowserFile?>(cut.Instance, "_selectedFile"));
    }

    [Fact]
    public async Task OnFileSelected_WhenExtensionUnsupported_SetsValidationAndClearsFile()
    {
        var cut = RenderComponent<WorldDocumentUploadDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        var file = CreateBrowserFile("script.exe", "application/octet-stream", 100);

        await InvokePrivateOnRendererAsync(cut, "OnFileSelected", CreateInputFileChangeArgs(file));

        Assert.Contains("not supported", GetField<string>(cut.Instance, "_validationError"), StringComparison.OrdinalIgnoreCase);
        Assert.Null(GetField<IBrowserFile?>(cut.Instance, "_selectedFile"));
    }

    [Fact]
    public async Task OnFileSelected_WhenReadFails_SetsValidationAndClearsFileData()
    {
        var cut = RenderComponent<WorldDocumentUploadDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        var file = CreateBrowserFile("notes.txt", "text/plain", 100);
        file.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("stream failed"));

        await InvokePrivateOnRendererAsync(cut, "OnFileSelected", CreateInputFileChangeArgs(file));

        Assert.Contains("Failed to read file", GetField<string>(cut.Instance, "_validationError"), StringComparison.OrdinalIgnoreCase);
        Assert.Null(GetField<IBrowserFile?>(cut.Instance, "_selectedFile"));
        Assert.Null(GetField<byte[]?>(cut.Instance, "_fileData"));
    }

    [Fact]
    public async Task OnFileSelected_WhenValid_SetsTitleAndFileData()
    {
        var cut = RenderComponent<WorldDocumentUploadDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        var file = CreateBrowserFile("adventure-notes.txt", "text/plain", 3);

        await InvokePrivateOnRendererAsync(cut, "OnFileSelected", CreateInputFileChangeArgs(file));

        Assert.Equal("adventure-notes", GetField<string>(cut.Instance, "_title"));
        Assert.NotNull(GetField<byte[]?>(cut.Instance, "_fileData"));
        Assert.Equal(string.Empty, GetField<string>(cut.Instance, "_validationError"));
    }

    [Fact]
    public void HandlesUploadingRenderState_WhenUploading()
    {
        var cut = RenderComponent<WorldDocumentUploadDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        SetField(cut.Instance, "_isUploading", true);
        SetField(cut.Instance, "_uploadStatus", "Uploading file...");

        cut.Render();

        Assert.True(GetField<bool>(cut.Instance, "_isUploading"));
        Assert.Equal("Uploading file...", GetField<string>(cut.Instance, "_uploadStatus"));
    }

    [Fact]
    public void HandlesSelectedFileRenderState_WhenFileSelected()
    {
        var cut = RenderComponent<WorldDocumentUploadDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        var file = CreateBrowserFile("notes.txt", "text/plain", 3);
        SetField(cut.Instance, "_selectedFile", file);

        cut.Render();

        Assert.Same(file, GetField<IBrowserFile?>(cut.Instance, "_selectedFile"));
    }

    private static IBrowserFile CreateBrowserFile(string name, string contentType, long size)
    {
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns(name);
        file.ContentType.Returns(contentType);
        file.Size.Returns(size);
        file.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
        return file;
    }

    private static InputFileChangeEventArgs CreateInputFileChangeArgs(IBrowserFile file)
    {
        var constructors = typeof(InputFileChangeEventArgs)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var singleFileCtor = constructors.FirstOrDefault(c =>
        {
            var p = c.GetParameters();
            return p.Length == 1 && p[0].ParameterType == typeof(IBrowserFile);
        });
        if (singleFileCtor != null)
        {
            return (InputFileChangeEventArgs)singleFileCtor.Invoke([file]);
        }

        var listCtor = constructors.FirstOrDefault(c =>
        {
            var p = c.GetParameters();
            return p.Length == 1 && typeof(IReadOnlyList<IBrowserFile>).IsAssignableFrom(p[0].ParameterType);
        });
        if (listCtor != null)
        {
            return (InputFileChangeEventArgs)listCtor.Invoke([new List<IBrowserFile> { file }]);
        }

        throw new InvalidOperationException("No supported InputFileChangeEventArgs constructor found.");
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<WorldDocumentUploadDialog> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }
}
