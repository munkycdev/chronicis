using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ImageAccessServiceTests
{
    [Fact]
    public async Task GetImageDownloadUrlAsync_ReturnsNotFound_WhenDocumentMissing()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var blob = Substitute.For<IBlobStorageService>();
        var sut = new ImageAccessService(db, blob, NullLogger<ImageAccessService>.Instance);

        var result = await sut.GetImageDownloadUrlAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ServiceStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetImageDownloadUrlAsync_ReturnsForbidden_WhenUserHasNoWorldAccess()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: ownerId);
        db.Worlds.Add(world);
        db.WorldDocuments.Add(new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            FileName = "map.png",
            Title = "map.png",
            BlobPath = "worlds/map.png",
            ContentType = "image/png",
            FileSizeBytes = 1,
            UploadedById = ownerId,
            UploadedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var blob = Substitute.For<IBlobStorageService>();
        var sut = new ImageAccessService(db, blob, NullLogger<ImageAccessService>.Instance);

        var result = await sut.GetImageDownloadUrlAsync(db.WorldDocuments.Single().Id, userId);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task GetImageDownloadUrlAsync_ReturnsValidationError_WhenDocumentIsNotImage()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: ownerId);
        db.Worlds.Add(world);
        var doc = new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            FileName = "doc.txt",
            Title = "doc.txt",
            BlobPath = "worlds/doc.txt",
            ContentType = "text/plain",
            FileSizeBytes = 1,
            UploadedById = ownerId,
            UploadedAt = DateTime.UtcNow
        };
        db.WorldDocuments.Add(doc);
        await db.SaveChangesAsync();

        var blob = Substitute.For<IBlobStorageService>();
        var sut = new ImageAccessService(db, blob, NullLogger<ImageAccessService>.Instance);

        var result = await sut.GetImageDownloadUrlAsync(doc.Id, ownerId);

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
    }

    [Fact]
    public async Task GetImageDownloadUrlAsync_ReturnsSuccess_ForAccessibleImage()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: ownerId);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        var doc = new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = world.Id,
            FileName = "image.jpg",
            Title = "image.jpg",
            BlobPath = "worlds/image.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1,
            UploadedById = ownerId,
            UploadedAt = DateTime.UtcNow
        };
        db.WorldDocuments.Add(doc);
        await db.SaveChangesAsync();

        var blob = Substitute.For<IBlobStorageService>();
        blob.GenerateDownloadSasUrlAsync(doc.BlobPath).Returns("https://example.com/sas");
        var sut = new ImageAccessService(db, blob, NullLogger<ImageAccessService>.Instance);

        var result = await sut.GetImageDownloadUrlAsync(doc.Id, userId);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.Equal("https://example.com/sas", result.Value);
    }
}
