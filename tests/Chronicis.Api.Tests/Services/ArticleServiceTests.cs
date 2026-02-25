using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;


[ExcludeFromCodeCoverage]
public class ArticleServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly ArticleService _service;
    private readonly IArticleHierarchyService _hierarchyService;

    public ArticleServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);

        // Mock hierarchy service for breadcrumb building
        _hierarchyService = Substitute.For<IArticleHierarchyService>();

        _service = new ArticleService(
            _context,
            NullLogger<ArticleService>.Instance,
            _hierarchyService);

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
        // Seed basic world with owner and member
        var (world, owner, member) = TestHelpers.SeedBasicWorld(_context);

        // Create a second user who is NOT a member
        var nonMember = TestHelpers.CreateUser(id: TestHelpers.FixedIds.User3);
        _context.Users.Add(nonMember);

        // Seed article hierarchy: Root -> Child -> Grandchild
        TestHelpers.SeedArticleHierarchy(_context, world.Id, owner.Id);

        // Add a private article
        _context.Articles.Add(TestHelpers.CreateArticle(
            id: TestHelpers.FixedIds.Article4,
            worldId: world.Id,
            createdBy: owner.Id,
            title: "Private Article",
            slug: "private-article",
            visibility: ArticleVisibility.Private));

        // Add a public article created by member (not owner)
        _context.Articles.Add(TestHelpers.CreateArticle(
            id: TestHelpers.FixedIds.Article5,
            worldId: world.Id,
            createdBy: member.Id,
            title: "Member Article",
            slug: "member-article",
            visibility: ArticleVisibility.Public));

        _context.SaveChanges();
    }

    // ────────────────────────────────────────────────────────────────
    //  GetRootArticlesAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRootArticlesAsync_Member_ReturnsRootArticles()
    {
        var roots = await _service.GetRootArticlesAsync(TestHelpers.FixedIds.User1);

        // Should get Root Article, Private Article, and Member Article
        Assert.Equal(3, roots.Count);
        Assert.Contains(roots, a => a.Title == "Root Article");
        Assert.Contains(roots, a => a.Title == "Private Article");
        Assert.Contains(roots, a => a.Title == "Member Article");
    }

    [Fact]
    public async Task GetRootArticlesAsync_Member_FiltersPrivateArticlesNotOwnedByUser()
    {
        var roots = await _service.GetRootArticlesAsync(TestHelpers.FixedIds.User2);

        // User2 (member) should see Root Article and Member Article, but NOT Private Article
        Assert.Equal(2, roots.Count);
        Assert.Contains(roots, a => a.Title == "Root Article");
        Assert.Contains(roots, a => a.Title == "Member Article");
        Assert.DoesNotContain(roots, a => a.Title == "Private Article");
    }

    [Fact]
    public async Task GetRootArticlesAsync_NonMember_ReturnsEmpty()
    {
        var roots = await _service.GetRootArticlesAsync(TestHelpers.FixedIds.User3);

        Assert.Empty(roots);
    }

    [Fact]
    public async Task GetRootArticlesAsync_WithWorldFilter_ReturnsOnlyFromThatWorld()
    {
        // Create second world with articles
        var user = await _context.Users.FindAsync(TestHelpers.FixedIds.User1);
        var world2 = TestHelpers.CreateWorld(id: TestHelpers.FixedIds.World2, ownerId: user!.Id);
        _context.Worlds.Add(world2);
        _context.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world2.Id, userId: user.Id, role: WorldRole.GM));
        _context.Articles.Add(TestHelpers.CreateArticle(worldId: world2.Id, createdBy: user.Id, title: "World2 Article"));
        await _context.SaveChangesAsync();

        var roots = await _service.GetRootArticlesAsync(TestHelpers.FixedIds.User1, TestHelpers.FixedIds.World1);

        Assert.All(roots, a => Assert.Equal(TestHelpers.FixedIds.World1, a.WorldId));
        Assert.DoesNotContain(roots, a => a.Title == "World2 Article");
    }

    [Fact]
    public async Task GetRootArticlesAsync_SetsHasChildrenCorrectly()
    {
        var roots = await _service.GetRootArticlesAsync(TestHelpers.FixedIds.User1);

        var rootWithChildren = roots.First(a => a.Title == "Root Article");
        var rootWithoutChildren = roots.First(a => a.Title == "Private Article");

        Assert.True(rootWithChildren.HasChildren);
        Assert.Equal(1, rootWithChildren.ChildCount);
        Assert.False(rootWithoutChildren.HasChildren);
        Assert.Equal(0, rootWithoutChildren.ChildCount);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetAllArticlesAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllArticlesAsync_Member_ReturnsFlatList()
    {
        var articles = await _service.GetAllArticlesAsync(TestHelpers.FixedIds.User1);

        // Should get all 5 articles in flat list
        Assert.Equal(5, articles.Count);
        Assert.Contains(articles, a => a.Title == "Root Article");
        Assert.Contains(articles, a => a.Title == "Child Article");
        Assert.Contains(articles, a => a.Title == "Grandchild Article");
    }

    [Fact]
    public async Task GetAllArticlesAsync_FiltersPrivateArticles()
    {
        var articles = await _service.GetAllArticlesAsync(TestHelpers.FixedIds.User2);

        // User2 should not see the private article
        Assert.DoesNotContain(articles, a => a.Title == "Private Article");
    }

    [Fact]
    public async Task GetAllArticlesAsync_WithWorldFilter_ReturnsOnlyFromThatWorld()
    {
        var articles = await _service.GetAllArticlesAsync(TestHelpers.FixedIds.User1, TestHelpers.FixedIds.World1);

        Assert.All(articles, a => Assert.Equal(TestHelpers.FixedIds.World1, a.WorldId));
    }

    // ────────────────────────────────────────────────────────────────
    //  GetChildrenAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetChildrenAsync_ReturnsImmediateChildren()
    {
        var children = await _service.GetChildrenAsync(TestHelpers.FixedIds.Article1, TestHelpers.FixedIds.User1);

        Assert.Single(children);
        Assert.Equal("Child Article", children[0].Title);
        Assert.True(children[0].HasChildren);
    }

    [Fact]
    public async Task GetChildrenAsync_NoChildren_ReturnsEmpty()
    {
        var children = await _service.GetChildrenAsync(TestHelpers.FixedIds.Article3, TestHelpers.FixedIds.User1);

        Assert.Empty(children);
    }

    [Fact]
    public async Task GetChildrenAsync_NonMember_ReturnsEmpty()
    {
        var children = await _service.GetChildrenAsync(TestHelpers.FixedIds.Article1, TestHelpers.FixedIds.User3);

        Assert.Empty(children);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetArticleDetailAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetArticleDetailAsync_Member_ReturnsArticleWithDetails()
    {
        // Mock breadcrumbs
        _hierarchyService.BuildBreadcrumbsAsync(Arg.Any<Guid>(), Arg.Any<HierarchyWalkOptions>())
            .Returns(new List<BreadcrumbDto>());

        var article = await _service.GetArticleDetailAsync(TestHelpers.FixedIds.Article1, TestHelpers.FixedIds.User1);

        Assert.NotNull(article);
        Assert.Equal("Root Article", article!.Title);
        Assert.Equal(TestHelpers.FixedIds.User1, article.CreatedBy);
        Assert.NotNull(article.Breadcrumbs);
    }

    [Fact]
    public async Task GetArticleDetailAsync_PrivateArticle_OwnerCanSee()
    {
        // Mock breadcrumbs
        _hierarchyService.BuildBreadcrumbsAsync(Arg.Any<Guid>(), Arg.Any<HierarchyWalkOptions>())
            .Returns(new List<BreadcrumbDto>());

        var article = await _service.GetArticleDetailAsync(TestHelpers.FixedIds.Article4, TestHelpers.FixedIds.User1);

        Assert.NotNull(article);
        Assert.Equal("Private Article", article!.Title);
    }

    [Fact]
    public async Task GetArticleDetailAsync_PrivateArticle_NonOwnerCannotSee()
    {
        var article = await _service.GetArticleDetailAsync(TestHelpers.FixedIds.Article4, TestHelpers.FixedIds.User2);

        Assert.Null(article);
    }

    [Fact]
    public async Task GetArticleDetailAsync_NonMember_ReturnsNull()
    {
        var article = await _service.GetArticleDetailAsync(TestHelpers.FixedIds.Article1, TestHelpers.FixedIds.User3);

        Assert.Null(article);
    }

    [Fact]
    public async Task GetArticleDetailAsync_NonExistent_ReturnsNull()
    {
        var article = await _service.GetArticleDetailAsync(Guid.NewGuid(), TestHelpers.FixedIds.User1);

        Assert.Null(article);
    }

    // ────────────────────────────────────────────────────────────────
    //  MoveArticleAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveArticleAsync_ValidMove_Succeeds()
    {
        var result = await _service.MoveArticleAsync(
            TestHelpers.FixedIds.Article3,
            TestHelpers.FixedIds.Article1,
            null,
            TestHelpers.FixedIds.User1);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);

        var moved = await _context.Articles.FindAsync(TestHelpers.FixedIds.Article3);
        Assert.Equal(TestHelpers.FixedIds.Article1, moved!.ParentId);
    }

    [Fact]
    public async Task MoveArticleAsync_ToRoot_Succeeds()
    {
        var result = await _service.MoveArticleAsync(
            TestHelpers.FixedIds.Article2,
            null,
            null,
            TestHelpers.FixedIds.User1);

        Assert.True(result.Success);

        var moved = await _context.Articles.FindAsync(TestHelpers.FixedIds.Article2);
        Assert.Null(moved!.ParentId);
    }

    [Fact]
    public async Task MoveArticleAsync_ToSameParent_DoesNothing()
    {
        var originalModified = (await _context.Articles.FindAsync(TestHelpers.FixedIds.Article2))!.ModifiedAt;

        var result = await _service.MoveArticleAsync(
            TestHelpers.FixedIds.Article2,
            TestHelpers.FixedIds.Article1,
            null,
            TestHelpers.FixedIds.User1);

        Assert.True(result.Success);

        var article = await _context.Articles.FindAsync(TestHelpers.FixedIds.Article2);
        Assert.Equal(originalModified, article!.ModifiedAt); // Unchanged
    }

    [Fact]
    public async Task MoveArticleAsync_ToNonExistentParent_Fails()
    {
        var result = await _service.MoveArticleAsync(
            TestHelpers.FixedIds.Article2,
            Guid.NewGuid(),
            null,
            TestHelpers.FixedIds.User1);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task MoveArticleAsync_CreatesCycle_Fails()
    {
        // Try to move Root under its grandchild
        var result = await _service.MoveArticleAsync(
            TestHelpers.FixedIds.Article1,
            TestHelpers.FixedIds.Article3,
            null,
            TestHelpers.FixedIds.User1);

        Assert.False(result.Success);
        Assert.Contains("child of itself or its descendants", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MoveArticleAsync_ToSelf_Fails()
    {
        var result = await _service.MoveArticleAsync(
            TestHelpers.FixedIds.Article1,
            TestHelpers.FixedIds.Article1,
            null,
            TestHelpers.FixedIds.User1);

        Assert.False(result.Success);
        Assert.Contains("child of itself or its descendants", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MoveArticleAsync_NonMember_Fails()
    {
        var result = await _service.MoveArticleAsync(
            TestHelpers.FixedIds.Article2,
            null,
            null,
            TestHelpers.FixedIds.User3);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MoveArticleAsync_UpdatesModifiedFields()
    {
        var result = await _service.MoveArticleAsync(
            TestHelpers.FixedIds.Article3,
            null,
            null,
            TestHelpers.FixedIds.User1);

        Assert.True(result.Success);

        var moved = await _context.Articles.FindAsync(TestHelpers.FixedIds.Article3);
        Assert.NotNull(moved!.ModifiedAt);
        Assert.Equal(TestHelpers.FixedIds.User1, moved.LastModifiedBy);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetArticleByPathAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetArticleByPathAsync_ValidPath_ReturnsArticle()
    {
        // Mock hierarchy service to return breadcrumbs
        _hierarchyService.BuildBreadcrumbsAsync(Arg.Any<Guid>(), Arg.Any<HierarchyWalkOptions>())
            .Returns(new List<BreadcrumbDto>());

        var article = await _service.GetArticleByPathAsync(
            "test-world/root-article",
            TestHelpers.FixedIds.User1);

        Assert.NotNull(article);
        Assert.Equal("Root Article", article!.Title);
    }

    [Fact]
    public async Task GetArticleByPathAsync_NestedPath_ReturnsArticle()
    {
        _hierarchyService.BuildBreadcrumbsAsync(Arg.Any<Guid>(), Arg.Any<HierarchyWalkOptions>())
            .Returns(new List<BreadcrumbDto>());

        var article = await _service.GetArticleByPathAsync(
            "test-world/root-article/child-article/grandchild-article",
            TestHelpers.FixedIds.User1);

        Assert.NotNull(article);
        Assert.Equal("Grandchild Article", article!.Title);
    }

    [Fact]
    public async Task GetArticleByPathAsync_InvalidWorldSlug_ReturnsNull()
    {
        var article = await _service.GetArticleByPathAsync(
            "nonexistent-world/root-article",
            TestHelpers.FixedIds.User1);

        Assert.Null(article);
    }

    [Fact]
    public async Task GetArticleByPathAsync_InvalidArticleSlug_ReturnsNull()
    {
        var article = await _service.GetArticleByPathAsync(
            "test-world/nonexistent-article",
            TestHelpers.FixedIds.User1);

        Assert.Null(article);
    }

    [Fact]
    public async Task GetArticleByPathAsync_OnlyWorldSlug_ReturnsNull()
    {
        var article = await _service.GetArticleByPathAsync(
            "test-world",
            TestHelpers.FixedIds.User1);

        Assert.Null(article);
    }

    [Fact]
    public async Task GetArticleByPathAsync_EmptyPath_ReturnsNull()
    {
        var article = await _service.GetArticleByPathAsync(
            "",
            TestHelpers.FixedIds.User1);

        Assert.Null(article);
    }

    [Fact]
    public async Task GetArticleByPathAsync_NonMember_ReturnsNull()
    {
        var article = await _service.GetArticleByPathAsync(
            "test-world/root-article",
            TestHelpers.FixedIds.User3);

        Assert.Null(article);
    }

    // ────────────────────────────────────────────────────────────────
    //  IsSlugUniqueAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsSlugUniqueAsync_RootLevel_UniqueSlugsAllowed()
    {
        var isUnique = await _service.IsSlugUniqueAsync(
            "new-article",
            null,
            TestHelpers.FixedIds.World1,
            TestHelpers.FixedIds.User1);

        Assert.True(isUnique);
    }

    [Fact]
    public async Task IsSlugUniqueAsync_RootLevel_DuplicateSlugsNotAllowed()
    {
        var isUnique = await _service.IsSlugUniqueAsync(
            "root-article",
            null,
            TestHelpers.FixedIds.World1,
            TestHelpers.FixedIds.User1);

        Assert.False(isUnique);
    }

    [Fact]
    public async Task IsSlugUniqueAsync_ChildLevel_DifferentParentsAllowed()
    {
        // Create two articles with same slug under different parents
        _context.Articles.Add(TestHelpers.CreateArticle(
            worldId: TestHelpers.FixedIds.World1,
            parentId: TestHelpers.FixedIds.Article4,
            createdBy: TestHelpers.FixedIds.User1,
            slug: "duplicate-slug"));
        await _context.SaveChangesAsync();

        var isUnique = await _service.IsSlugUniqueAsync(
            "duplicate-slug",
            TestHelpers.FixedIds.Article1, // Different parent
            TestHelpers.FixedIds.World1,
            TestHelpers.FixedIds.User1);

        Assert.True(isUnique);
    }

    [Fact]
    public async Task IsSlugUniqueAsync_ChildLevel_SameParentNotAllowed()
    {
        var isUnique = await _service.IsSlugUniqueAsync(
            "child-article",
            TestHelpers.FixedIds.Article1,
            TestHelpers.FixedIds.World1,
            TestHelpers.FixedIds.User1);

        Assert.False(isUnique);
    }

    [Fact]
    public async Task IsSlugUniqueAsync_ExcludeArticleId_IgnoresSelf()
    {
        var isUnique = await _service.IsSlugUniqueAsync(
            "root-article",
            null,
            TestHelpers.FixedIds.World1,
            TestHelpers.FixedIds.User1,
            excludeArticleId: TestHelpers.FixedIds.Article1);

        Assert.True(isUnique);
    }

    // ────────────────────────────────────────────────────────────────
    //  GenerateUniqueSlugAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateUniqueSlugAsync_NoConflict_ReturnsBaseSlug()
    {
        var slug = await _service.GenerateUniqueSlugAsync(
            "Unique Article",
            null,
            TestHelpers.FixedIds.World1,
            TestHelpers.FixedIds.User1);

        Assert.Equal("unique-article", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_Conflict_AppendsNumber()
    {
        var slug = await _service.GenerateUniqueSlugAsync(
            "Root Article",
            null,
            TestHelpers.FixedIds.World1,
            TestHelpers.FixedIds.User1);

        Assert.Equal("root-article-2", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ExcludeArticleId_IgnoresSelf()
    {
        var slug = await _service.GenerateUniqueSlugAsync(
            "Root Article",
            null,
            TestHelpers.FixedIds.World1,
            TestHelpers.FixedIds.User1,
            excludeArticleId: TestHelpers.FixedIds.Article1);

        Assert.Equal("root-article", slug);
    }

    // ────────────────────────────────────────────────────────────────
    //  BuildArticlePathAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildArticlePathAsync_CallsHierarchyService()
    {
        _hierarchyService.BuildPathAsync(TestHelpers.FixedIds.Article1)
            .Returns(Task.FromResult("test-world/root-article"));

        var path = await _service.BuildArticlePathAsync(TestHelpers.FixedIds.Article1, TestHelpers.FixedIds.User1);

        Assert.Equal("test-world/root-article", path);
        await _hierarchyService.Received(1).BuildPathAsync(TestHelpers.FixedIds.Article1);
    }
}
