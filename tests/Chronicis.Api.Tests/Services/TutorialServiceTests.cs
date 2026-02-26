using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Chronicis.Api.Data;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class TutorialServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly TutorialService _sut;
    private readonly User _sysAdminUser;
    private bool _disposed;

    public TutorialServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _currentUserService = Substitute.For<ICurrentUserService>();
        _sysAdminUser = TestHelpers.CreateUser(id: TestHelpers.FixedIds.User1, email: "sysadmin@test.com");

        _currentUserService.GetRequiredUserAsync().Returns(_sysAdminUser);
        _currentUserService.GetCurrentUserAsync().Returns(_sysAdminUser);
        _currentUserService.IsSysAdminAsync().Returns(true);

        _sut = new TutorialService(_context, _currentUserService, NullLogger<TutorialService>.Instance);
    }

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

    [Fact]
    public async Task ResolveAsync_ExactMatch_ReturnsExactTutorial()
    {
        var expectedArticleId = SeedTutorialMapping("Page:Settings", "Settings Tutorial", "Settings body");
        SeedTutorialMapping("Page:Default", "Default Tutorial", "Default body");

        var result = await _sut.ResolveAsync("Page:Settings");

        Assert.NotNull(result);
        Assert.Equal(expectedArticleId, result!.ArticleId);
        Assert.Equal("Settings Tutorial", result.Title);
        Assert.Equal("Settings body", result.Body);
    }

    [Fact]
    public async Task ResolveAsync_ArticleTypeFallback_UsesAnyWhenSpecificMissing()
    {
        var anyArticleId = SeedTutorialMapping("ArticleType:Any", "Any Article Tutorial", "Any body");
        SeedTutorialMapping("Page:Default", "Default Tutorial", "Default body");

        var result = await _sut.ResolveAsync("ArticleType:Session");

        Assert.NotNull(result);
        Assert.Equal(anyArticleId, result!.ArticleId);
        Assert.Equal("Any Article Tutorial", result.Title);
    }

    [Fact]
    public async Task ResolveAsync_FallsBackToDefault_WhenNoExactOrAnyMatch()
    {
        var defaultArticleId = SeedTutorialMapping("Page:Default", "Default Tutorial", "Default body");

        var result = await _sut.ResolveAsync("Page:Dashboard");

        Assert.NotNull(result);
        Assert.Equal(defaultArticleId, result!.ArticleId);
        Assert.Equal("Default Tutorial", result.Title);
    }

    [Fact]
    public async Task CreateMappingAsync_NotSysAdmin_ThrowsUnauthorized()
    {
        _currentUserService.IsSysAdminAsync().Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.CreateMappingAsync(new TutorialMappingCreateDto
            {
                PageType = "Page:Dashboard",
                PageTypeName = "Dashboard",
                Title = "Dashboard Tutorial"
            }));
    }

    [Fact]
    public async Task TutorialCrud_SysAdmin_CreatesUpdatesAndDeletesMappings()
    {
        var created = await _sut.CreateMappingAsync(new TutorialMappingCreateDto
        {
            PageType = "Page:Dashboard",
            PageTypeName = "Dashboard",
            Title = "Dashboard Tutorial",
            Body = "<p>hello</p>"
        });

        var createdArticle = await _context.Articles.FindAsync(created.ArticleId);
        Assert.NotNull(createdArticle);
        Assert.Equal(ArticleType.Tutorial, createdArticle!.Type);
        Assert.Equal(Guid.Empty, createdArticle.WorldId);

        var attachedArticle = TestHelpers.CreateArticle(
            worldId: Guid.Empty,
            createdBy: _sysAdminUser.Id,
            title: "Attached Tutorial",
            slug: "attached-tutorial",
            type: ArticleType.Tutorial,
            visibility: ArticleVisibility.Public);
        _context.Articles.Add(attachedArticle);
        await _context.SaveChangesAsync();

        var updated = await _sut.UpdateMappingAsync(created.Id, new TutorialMappingUpdateDto
        {
            PageType = "Page:Dashboard",
            PageTypeName = "Dashboard Updated",
            ArticleId = attachedArticle.Id
        });

        Assert.NotNull(updated);
        Assert.Equal("Dashboard Updated", updated!.PageTypeName);
        Assert.Equal(attachedArticle.Id, updated.ArticleId);

        var mappings = await _sut.GetMappingsAsync();
        Assert.Contains(mappings, m => m.Id == created.Id && m.ArticleId == attachedArticle.Id);

        var deleted = await _sut.DeleteMappingAsync(created.Id);
        Assert.True(deleted);
        Assert.Null(await _context.TutorialPages.FindAsync(created.Id));
    }

    [Fact]
    public void NormalizeRequired_WhenValid_TrimsValue()
    {
        var method = typeof(TutorialService).GetMethod("NormalizeRequired", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (string?)method!.Invoke(null, ["  value  ", "pageType"]);

        Assert.Equal("value", result);
    }

    [Fact]
    public void NormalizeRequired_WhenWhitespace_ThrowsArgumentException()
    {
        var method = typeof(TutorialService).GetMethod("NormalizeRequired", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var ex = Assert.Throws<TargetInvocationException>(() => method!.Invoke(null, ["   ", "pageType"]));
        var inner = Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Equal("pageType", inner.ParamName);
    }

    private Guid SeedTutorialMapping(string pageType, string title, string body)
    {
        var article = TestHelpers.CreateArticle(
            worldId: Guid.Empty,
            createdBy: _sysAdminUser.Id,
            title: title,
            slug: Slugify(title),
            body: body,
            type: ArticleType.Tutorial,
            visibility: ArticleVisibility.Public);

        var mapping = new TutorialPage
        {
            Id = Guid.NewGuid(),
            PageType = pageType,
            PageTypeName = pageType,
            ArticleId = article.Id,
            Article = article,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        _context.Articles.Add(article);
        _context.TutorialPages.Add(mapping);
        _context.SaveChanges();

        return article.Id;
    }

    private static string Slugify(string value)
    {
        return value
            .ToLowerInvariant()
            .Replace(" ", "-", StringComparison.Ordinal)
            .Replace(":", string.Empty, StringComparison.Ordinal);
    }
}
