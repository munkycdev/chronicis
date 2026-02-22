using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public class WorldDocumentServiceTests
{
    private readonly WorldDocumentService _sut;

    public WorldDocumentServiceTests()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        _sut = new WorldDocumentService(
            db,
            Substitute.For<IBlobStorageService>(),
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            NullLogger<WorldDocumentService>.Instance);
    }

    // ── ValidateFileUpload ──────────────────────────────────

    [Fact]
    public void Validate_ThrowsOnZeroSize()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _sut.ValidateFileUpload(new WorldDocumentUploadRequestDto { FileSizeBytes = 0, FileName = "a.txt" }));
        Assert.Contains("greater than zero", ex.Message);
    }

    [Fact]
    public void Validate_ThrowsOnExceedingMaxSize()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _sut.ValidateFileUpload(new WorldDocumentUploadRequestDto { FileSizeBytes = 300_000_000, FileName = "a.txt" }));
        Assert.Contains("exceeds maximum", ex.Message);
    }

    [Fact]
    public void Validate_ThrowsOnEmptyFilename()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _sut.ValidateFileUpload(new WorldDocumentUploadRequestDto { FileSizeBytes = 1, FileName = " " }));
        Assert.Contains("Filename is required", ex.Message);
    }

    [Fact]
    public void Validate_ThrowsOnDisallowedExtension()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _sut.ValidateFileUpload(new WorldDocumentUploadRequestDto { FileSizeBytes = 1, FileName = "a.exe" }));
        Assert.Contains("not allowed", ex.Message);
    }

    [Fact]
    public void Validate_ThrowsOnNoExtension()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _sut.ValidateFileUpload(new WorldDocumentUploadRequestDto { FileSizeBytes = 1, FileName = "no-extension" }));
        Assert.Contains("not allowed", ex.Message);
    }

    [Fact]
    public void Validate_PassesForValidFile()
    {
        // Should not throw
        _sut.ValidateFileUpload(new WorldDocumentUploadRequestDto
        {
            FileSizeBytes = 1024,
            FileName = "notes.txt",
            ContentType = "text/plain"
        });
    }

    [Fact]
    public void Validate_PassesWithMismatchedContentType()
    {
        // Mismatched content type logs warning but doesn't throw
        _sut.ValidateFileUpload(new WorldDocumentUploadRequestDto
        {
            FileSizeBytes = 1024,
            FileName = "notes.txt",
            ContentType = "application/octet-stream"
        });
    }

    [Fact]
    public void Validate_PassesWithNullContentType()
    {
        _sut.ValidateFileUpload(new WorldDocumentUploadRequestDto
        {
            FileSizeBytes = 1024,
            FileName = "notes.txt",
            ContentType = default
        });
    }

    // ── MapToDto ────────────────────────────────────────────

    [Fact]
    public void MapToDto_MapsAllFields()
    {
        var doc = new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            ArticleId = Guid.NewGuid(),
            FileName = "notes.md",
            Title = "Notes",
            ContentType = "text/markdown",
            FileSizeBytes = 55,
            Description = "desc",
            UploadedAt = DateTime.UtcNow,
            UploadedById = Guid.NewGuid()
        };

        var dto = WorldDocumentService.MapToDto(doc);

        Assert.Equal(doc.Id, dto.Id);
        Assert.Equal(doc.WorldId, dto.WorldId);
        Assert.Equal(doc.ArticleId, dto.ArticleId);
        Assert.Equal(doc.FileName, dto.FileName);
        Assert.Equal(doc.Title, dto.Title);
        Assert.Equal(doc.ContentType, dto.ContentType);
        Assert.Equal(doc.FileSizeBytes, dto.FileSizeBytes);
        Assert.Equal(doc.Description, dto.Description);
        Assert.Equal(doc.UploadedAt, dto.UploadedAt);
        Assert.Equal(doc.UploadedById, dto.UploadedById);
    }
}
