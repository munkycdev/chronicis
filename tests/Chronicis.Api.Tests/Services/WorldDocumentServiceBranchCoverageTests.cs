using System.Reflection;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldDocumentServiceBranchCoverageTests
{
    [Fact]
    public void WorldDocumentService_MapToDto_MapsAllFields()
    {
        var map = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(WorldDocumentService), "MapToDto");
        var uploadedAt = DateTime.UtcNow;
        var document = new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            ArticleId = Guid.NewGuid(),
            FileName = "notes.md",
            Title = "Notes",
            ContentType = "text/markdown",
            FileSizeBytes = 55,
            Description = "desc",
            UploadedAt = uploadedAt,
            UploadedById = Guid.NewGuid()
        };

        var dto = (WorldDocumentDto)map.Invoke(null, [document])!;

        Assert.Equal(document.Id, dto.Id);
        Assert.Equal(document.WorldId, dto.WorldId);
        Assert.Equal(document.ArticleId, dto.ArticleId);
        Assert.Equal(document.FileName, dto.FileName);
        Assert.Equal(document.Title, dto.Title);
        Assert.Equal(document.ContentType, dto.ContentType);
        Assert.Equal(document.FileSizeBytes, dto.FileSizeBytes);
        Assert.Equal(document.Description, dto.Description);
        Assert.Equal(document.UploadedAt, dto.UploadedAt);
        Assert.Equal(document.UploadedById, dto.UploadedById);
    }

    [Fact]
    public void WorldDocumentService_ValidateFileUpload_CoversBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var service = new WorldDocumentService(
            db,
            Substitute.For<IBlobStorageService>(),
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            NullLogger<WorldDocumentService>.Instance);

        var validate = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(WorldDocumentService), "ValidateFileUpload");

        Assert.Throws<TargetInvocationException>(() => validate.Invoke(service, [new WorldDocumentUploadRequestDto { FileSizeBytes = 0, FileName = "a.txt" }]));
        Assert.Throws<TargetInvocationException>(() => validate.Invoke(service, [new WorldDocumentUploadRequestDto { FileSizeBytes = 300_000_000, FileName = "a.txt" }]));
        Assert.Throws<TargetInvocationException>(() => validate.Invoke(service, [new WorldDocumentUploadRequestDto { FileSizeBytes = 1, FileName = " " }]));
        Assert.Throws<TargetInvocationException>(() => validate.Invoke(service, [new WorldDocumentUploadRequestDto { FileSizeBytes = 1, FileName = "a.exe" }]));
        Assert.Throws<TargetInvocationException>(() => validate.Invoke(service, [new WorldDocumentUploadRequestDto { FileSizeBytes = 1, FileName = "no-extension" }]));

        validate.Invoke(service, [new WorldDocumentUploadRequestDto
        {
            FileSizeBytes = 1,
            FileName = "a.txt",
            ContentType = "text/plain"
        }]);

        validate.Invoke(service, [new WorldDocumentUploadRequestDto
        {
            FileSizeBytes = 1,
            FileName = "a.txt",
            ContentType = "application/octet-stream"
        }]);

        var mapField = typeof(WorldDocumentService).GetField("ExtensionToMimeType", BindingFlags.NonPublic | BindingFlags.Static)!;
        var map = (Dictionary<string, string>)mapField.GetValue(null)!;
        var removed = map[".txt"];
        map.Remove(".txt");
        try
        {
            validate.Invoke(service, [new WorldDocumentUploadRequestDto
            {
                FileSizeBytes = 1,
                FileName = "a.txt",
                ContentType = "text/plain"
            }]);
        }
        finally
        {
            map[".txt"] = removed;
        }
    }
}
