using System.Reflection;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class WorldDocumentServiceBranchCoverageTests
{
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
