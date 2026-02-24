using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class ArticleValidationServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly ArticleValidationService _service;

    public ArticleValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new ArticleValidationService(_context);

        SeedTestData();
    }

    private bool _disposed = false;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _context.Dispose();
        }

        _disposed = true;
    }

    private void SeedTestData()
    {
        // Seed basic world with owner
        var (world, owner, _) = TestHelpers.SeedBasicWorld(_context);

        // Seed article hierarchy
        TestHelpers.SeedArticleHierarchy(_context, world.Id, owner.Id);

        // Add a campaign and arc for session validation
        var campaign = TestHelpers.CreateCampaign(
            id: TestHelpers.FixedIds.Campaign1,
            worldId: world.Id);

        var arc = TestHelpers.CreateArc(
            id: TestHelpers.FixedIds.Arc1,
            campaignId: campaign.Id);

        _context.Campaigns.Add(campaign);
        _context.Arcs.Add(arc);
        _context.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    //  ValidateCreateAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateCreateAsync_ValidArticle_Succeeds()
    {
        var dto = new ArticleCreateDto
        {
            Title = "New Article",
            WorldId = TestHelpers.FixedIds.World1,
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public
        };

        var result = await _service.ValidateCreateAsync(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateCreateAsync_ValidParentId_Succeeds()
    {
        var dto = new ArticleCreateDto
        {
            Title = "Child Article",
            WorldId = TestHelpers.FixedIds.World1,
            ParentId = TestHelpers.FixedIds.Article1,
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public
        };

        var result = await _service.ValidateCreateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateCreateAsync_InvalidParentId_Fails()
    {
        var dto = new ArticleCreateDto
        {
            Title = "Orphan Article",
            WorldId = TestHelpers.FixedIds.World1,
            ParentId = Guid.NewGuid(), // Non-existent parent
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public
        };

        var result = await _service.ValidateCreateAsync(dto);

        Assert.False(result.IsValid);
        Assert.Contains("ParentId", result.Errors.Keys);
        Assert.Contains("does not exist", result.Errors["ParentId"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCreateAsync_NullParentId_Succeeds()
    {
        var dto = new ArticleCreateDto
        {
            Title = "Root Article",
            WorldId = TestHelpers.FixedIds.World1,
            ParentId = null,
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public
        };

        var result = await _service.ValidateCreateAsync(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateCreateAsync_SessionArticleType_Fails()
    {
        var dto = new ArticleCreateDto
        {
            Title = "Legacy Session",
            WorldId = TestHelpers.FixedIds.World1,
            ArcId = TestHelpers.FixedIds.Arc1,
            CampaignId = TestHelpers.FixedIds.Campaign1,
            Type = ArticleType.Session,
            Visibility = ArticleVisibility.Public
        };

        var result = await _service.ValidateCreateAsync(dto);

        Assert.False(result.IsValid);
        Assert.Contains("Type", result.Errors.Keys);
        Assert.Contains("deprecated", result.Errors["Type"][0], StringComparison.OrdinalIgnoreCase);
    }

    // ────────────────────────────────────────────────────────────────
    //  ValidateUpdateAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateUpdateAsync_ValidUpdate_Succeeds()
    {
        var dto = new ArticleUpdateDto
        {
            Title = "Updated Title",
            Body = "Updated body"
        };

        var result = await _service.ValidateUpdateAsync(TestHelpers.FixedIds.Article1, dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateUpdateAsync_EmptyTitle_Fails()
    {
        var dto = new ArticleUpdateDto
        {
            Title = "",
            Body = "Updated body"
        };

        var result = await _service.ValidateUpdateAsync(TestHelpers.FixedIds.Article1, dto);

        Assert.False(result.IsValid);
        Assert.Contains("Title", result.Errors.Keys);
        Assert.Contains("required", result.Errors["Title"][0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateUpdateAsync_WhitespaceTitle_Fails()
    {
        var dto = new ArticleUpdateDto
        {
            Title = "   ",
            Body = "Updated body"
        };

        var result = await _service.ValidateUpdateAsync(TestHelpers.FixedIds.Article1, dto);

        Assert.False(result.IsValid);
        Assert.Contains("Title", result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateUpdateAsync_NullTitle_Fails()
    {
        var dto = new ArticleUpdateDto
        {
            Title = null!,
            Body = "Updated body"
        };

        var result = await _service.ValidateUpdateAsync(TestHelpers.FixedIds.Article1, dto);

        Assert.False(result.IsValid);
        Assert.Contains("Title", result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateUpdateAsync_NonExistentArticle_Fails()
    {
        var dto = new ArticleUpdateDto
        {
            Title = "Updated Title"
        };

        var result = await _service.ValidateUpdateAsync(Guid.NewGuid(), dto);

        Assert.False(result.IsValid);
        Assert.Contains("Id", result.Errors.Keys);
        Assert.Contains("not found", result.Errors["Id"][0], StringComparison.OrdinalIgnoreCase);
    }

    // ────────────────────────────────────────────────────────────────
    //  ValidateDeleteAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateDeleteAsync_ArticleWithoutChildren_Succeeds()
    {
        var result = await _service.ValidateDeleteAsync(TestHelpers.FixedIds.Article3);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateDeleteAsync_ArticleWithChildren_Succeeds()
    {
        // The current implementation allows deletion of articles with children
        // (presumably cascade delete or orphan handling happens elsewhere)
        var result = await _service.ValidateDeleteAsync(TestHelpers.FixedIds.Article1);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateDeleteAsync_NonExistentArticle_Fails()
    {
        var result = await _service.ValidateDeleteAsync(Guid.NewGuid());

        Assert.False(result.IsValid);
        Assert.Contains("Id", result.Errors.Keys);
        Assert.Contains("not found", result.Errors["Id"][0], StringComparison.OrdinalIgnoreCase);
    }

    // ────────────────────────────────────────────────────────────────
    //  ValidationResult helper tests
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidationResult_AddError_AddsToErrors()
    {
        var result = new ValidationResult();

        result.AddError("Field1", "Error message 1");
        result.AddError("Field2", "Error message 2");

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Field1", result.Errors.Keys);
        Assert.Contains("Field2", result.Errors.Keys);
    }

    [Fact]
    public void ValidationResult_AddError_MultipleErrorsForSameField()
    {
        var result = new ValidationResult();

        result.AddError("Field1", "Error 1");
        result.AddError("Field1", "Error 2");

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(2, result.Errors["Field1"].Count);
    }

    [Fact]
    public void ValidationResult_GetFirstError_ReturnsFirstErrorMessage()
    {
        var result = new ValidationResult();

        result.AddError("Field1", "First error");
        result.AddError("Field2", "Second error");

        var firstError = result.GetFirstError();

        Assert.Equal("First error", firstError);
    }

    [Fact]
    public void ValidationResult_GetFirstError_NoErrors_ReturnsDefaultMessage()
    {
        var result = new ValidationResult();

        var firstError = result.GetFirstError();

        Assert.Equal("Validation failed", firstError);
    }
}
