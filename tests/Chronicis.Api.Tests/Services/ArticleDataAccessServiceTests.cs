using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ArticleDataAccessServiceTests
{
    [Fact]
    public async Task AddArticleAsync_AndSaveChangesAsync_PersistUpdates()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var sut = new ArticleDataAccessService(db, Substitute.For<IWorldDocumentService>(), new ReadAccessPolicyService());
        var article = TestHelpers.CreateArticle(type: ArticleType.Tutorial, worldId: Guid.Empty);

        await sut.AddArticleAsync(article);
        article.Title = "Updated";
        await sut.SaveChangesAsync();

        Assert.Equal("Updated", db.Articles.Single(a => a.Id == article.Id).Title);
    }

    [Fact]
    public async Task FindReadableAndResolveMethods_RespectMembershipAndTutorialAccess()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var readableWorld = TestHelpers.CreateWorld(ownerId: userId);
        var unreadableWorld = TestHelpers.CreateWorld(ownerId: Guid.NewGuid());
        db.Worlds.AddRange(readableWorld, unreadableWorld);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: readableWorld.Id, userId: userId));

        var readable = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: userId,
            title: "Readable",
            type: ArticleType.WikiArticle);

        var unreadable = TestHelpers.CreateArticle(
            worldId: unreadableWorld.Id,
            createdBy: unreadableWorld.OwnerId,
            title: "Unreadable",
            type: ArticleType.WikiArticle);

        var tutorial = TestHelpers.CreateArticle(
            worldId: Guid.Empty,
            createdBy: userId,
            title: "Tutorial",
            type: ArticleType.Tutorial);

        var privateNotOwned = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: Guid.NewGuid(),
            title: "Private Not Owned",
            type: ArticleType.WikiArticle,
            visibility: ArticleVisibility.Private);

        db.Articles.AddRange(readable, unreadable, tutorial, privateNotOwned);
        await db.SaveChangesAsync();

        var sut = new ArticleDataAccessService(db, Substitute.For<IWorldDocumentService>(), new ReadAccessPolicyService());

        Assert.NotNull(await sut.FindReadableArticleAsync(readable.Id, userId));
        Assert.NotNull(await sut.FindReadableArticleAsync(tutorial.Id, userId));
        Assert.Null(await sut.FindReadableArticleAsync(unreadable.Id, userId));
        Assert.Null(await sut.FindReadableArticleAsync(privateNotOwned.Id, userId));

        var resolved = await sut.ResolveReadableLinksAsync(
            [readable.Id, unreadable.Id, tutorial.Id, privateNotOwned.Id],
            userId);
        Assert.Equal(2, resolved.Count);
        Assert.Contains(resolved, r => r.ArticleId == readable.Id);
        Assert.Contains(resolved, r => r.ArticleId == tutorial.Id);

        var readableWorldResult = await sut.TryGetReadableArticleWorldAsync(readable.Id, userId);
        Assert.True(readableWorldResult.Found);
        Assert.Equal(readableWorld.Id, readableWorldResult.WorldId);

        var tutorialResult = await sut.TryGetReadableArticleWorldAsync(tutorial.Id, userId);
        Assert.True(tutorialResult.Found);
        Assert.Equal(Guid.Empty, tutorialResult.WorldId);

        var missingResult = await sut.TryGetReadableArticleWorldAsync(unreadable.Id, userId);
        Assert.False(missingResult.Found);
        Assert.Null(missingResult.WorldId);

        var privateMissingResult = await sut.TryGetReadableArticleWorldAsync(privateNotOwned.Id, userId);
        Assert.False(privateMissingResult.Found);
        Assert.Null(privateMissingResult.WorldId);
    }

    [Fact]
    public async Task IsTutorialSlugUniqueAsync_CoversParentAndExcludeBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var parent = TestHelpers.CreateArticle(type: ArticleType.Tutorial, worldId: Guid.Empty, title: "Parent");
        var rootTutorial = TestHelpers.CreateArticle(
            type: ArticleType.Tutorial,
            worldId: Guid.Empty,
            title: "Root",
            slug: "duplicate-root");
        var childTutorial = TestHelpers.CreateArticle(
            type: ArticleType.Tutorial,
            worldId: Guid.Empty,
            parentId: parent.Id,
            title: "Child",
            slug: "duplicate-child");
        db.Articles.AddRange(parent, rootTutorial, childTutorial);
        await db.SaveChangesAsync();

        var sut = new ArticleDataAccessService(db, Substitute.For<IWorldDocumentService>(), new ReadAccessPolicyService());

        Assert.False(await sut.IsTutorialSlugUniqueAsync("duplicate-root", null));
        Assert.False(await sut.IsTutorialSlugUniqueAsync("duplicate-child", parent.Id));
        Assert.True(await sut.IsTutorialSlugUniqueAsync("duplicate-child", parent.Id, childTutorial.Id));
        Assert.True(await sut.IsTutorialSlugUniqueAsync("new-slug", null));
    }

    [Fact]
    public async Task GenerateTutorialSlugAsync_CoversParentAndExcludeBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var parent = TestHelpers.CreateArticle(type: ArticleType.Tutorial, worldId: Guid.Empty, title: "Parent");
        var root1 = TestHelpers.CreateArticle(
            type: ArticleType.Tutorial,
            worldId: Guid.Empty,
            title: "Root1",
            slug: "hello-world");
        var root2 = TestHelpers.CreateArticle(
            type: ArticleType.Tutorial,
            worldId: Guid.Empty,
            title: "Root2",
            slug: "hello-world-2");
        var child = TestHelpers.CreateArticle(
            type: ArticleType.Tutorial,
            worldId: Guid.Empty,
            parentId: parent.Id,
            title: "Child",
            slug: "child-slug");
        db.Articles.AddRange(parent, root1, root2, child);
        await db.SaveChangesAsync();

        var sut = new ArticleDataAccessService(db, Substitute.For<IWorldDocumentService>(), new ReadAccessPolicyService());

        var generatedRoot = await sut.GenerateTutorialSlugAsync("Hello World", null);
        Assert.Equal("hello-world-3", generatedRoot);

        var generatedChild = await sut.GenerateTutorialSlugAsync("Child Slug", parent.Id);
        Assert.Equal("child-slug-2", generatedChild);

        var generatedWithExclude = await sut.GenerateTutorialSlugAsync("Child Slug", parent.Id, child.Id);
        Assert.Equal("child-slug", generatedWithExclude);
    }

    [Fact]
    public async Task DeleteArticleAndDescendantsAsync_RemovesHierarchyLinksAndImages()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var worldDocumentService = Substitute.For<IWorldDocumentService>();
        var sut = new ArticleDataAccessService(db, worldDocumentService, new ReadAccessPolicyService());

        var world = TestHelpers.CreateWorld(ownerId: Guid.NewGuid());
        db.Worlds.Add(world);

        var root = TestHelpers.CreateArticle(worldId: world.Id, type: ArticleType.WikiArticle, title: "Root");
        var child = TestHelpers.CreateArticle(worldId: world.Id, parentId: root.Id, type: ArticleType.WikiArticle, title: "Child");
        var target = TestHelpers.CreateArticle(worldId: world.Id, type: ArticleType.WikiArticle, title: "Target");
        db.Articles.AddRange(root, child, target);

        db.ArticleLinks.AddRange(
            new ArticleLink
            {
                Id = Guid.NewGuid(),
                SourceArticleId = root.Id,
                TargetArticleId = target.Id,
                DisplayText = "root->target",
                Position = 1,
                CreatedAt = DateTime.UtcNow
            },
            new ArticleLink
            {
                Id = Guid.NewGuid(),
                SourceArticleId = target.Id,
                TargetArticleId = root.Id,
                DisplayText = "target->root",
                Position = 2,
                CreatedAt = DateTime.UtcNow
            },
            new ArticleLink
            {
                Id = Guid.NewGuid(),
                SourceArticleId = child.Id,
                TargetArticleId = target.Id,
                DisplayText = "child->target",
                Position = 3,
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        await sut.DeleteArticleAndDescendantsAsync(root.Id);
        await sut.DeleteArticleAndDescendantsAsync(Guid.NewGuid());

        Assert.DoesNotContain(db.Articles, a => a.Id == root.Id || a.Id == child.Id);
        Assert.Contains(db.Articles, a => a.Id == target.Id);
        Assert.DoesNotContain(db.ArticleLinks, l => l.SourceArticleId == root.Id || l.TargetArticleId == root.Id);
        Assert.DoesNotContain(db.ArticleLinks, l => l.SourceArticleId == child.Id || l.TargetArticleId == child.Id);

        await worldDocumentService.Received(1).DeleteArticleImagesAsync(root.Id);
        await worldDocumentService.Received(1).DeleteArticleImagesAsync(child.Id);
    }

    [Fact]
    public async Task GetBacklinksAndOutgoingLinks_ReturnExpectedDtos()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var sut = new ArticleDataAccessService(db, Substitute.For<IWorldDocumentService>(), new ReadAccessPolicyService());
        var world = TestHelpers.CreateWorld(ownerId: Guid.NewGuid());
        db.Worlds.Add(world);

        var source = TestHelpers.CreateArticle(worldId: world.Id, title: "Source", slug: "source");
        var target = TestHelpers.CreateArticle(worldId: world.Id, title: "Target", slug: "target");
        db.Articles.AddRange(source, target);
        db.ArticleLinks.Add(new ArticleLink
        {
            Id = Guid.NewGuid(),
            SourceArticleId = source.Id,
            TargetArticleId = target.Id,
            DisplayText = "snippet",
            Position = 1,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var backlinks = await sut.GetBacklinksAsync(target.Id);
        var outgoing = await sut.GetOutgoingLinksAsync(source.Id);

        Assert.Single(backlinks);
        Assert.Equal(source.Id, backlinks[0].ArticleId);
        Assert.Equal("Source", backlinks[0].Title);

        Assert.Single(outgoing);
        Assert.Equal(target.Id, outgoing[0].ArticleId);
        Assert.Equal("Target", outgoing[0].Title);
    }

    [Fact]
    public async Task GetReadableArticleWithAliasesAndUpsertAliases_CoversAllPaths()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var world = TestHelpers.CreateWorld(ownerId: Guid.NewGuid());
        var memberId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: memberId));

        var article = TestHelpers.CreateArticle(worldId: world.Id, createdBy: memberId, title: "Alias Target");
        db.Articles.Add(article);
        db.ArticleAliases.AddRange(
            new ArticleAlias
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                AliasText = "KeepAlias",
                CreatedAt = DateTime.UtcNow
            },
            new ArticleAlias
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                AliasText = "RemoveAlias",
                CreatedAt = DateTime.UtcNow
            });

        var tutorial = TestHelpers.CreateArticle(
            worldId: Guid.Empty,
            createdBy: memberId,
            type: ArticleType.Tutorial,
            title: "Tutorial Alias Target");
        db.Articles.Add(tutorial);
        await db.SaveChangesAsync();

        var sut = new ArticleDataAccessService(db, Substitute.For<IWorldDocumentService>(), new ReadAccessPolicyService());

        var readable = await sut.GetReadableArticleWithAliasesAsync(article.Id, memberId);
        Assert.NotNull(readable);
        Assert.Equal(2, readable!.Aliases.Count);

        var unreadable = await sut.GetReadableArticleWithAliasesAsync(article.Id, outsiderId);
        Assert.Null(unreadable);

        var tutorialReadable = await sut.GetReadableArticleWithAliasesAsync(tutorial.Id, outsiderId);
        Assert.NotNull(tutorialReadable);

        await sut.UpsertAliasesAsync(readable, ["keepalias", "NewAlias"], memberId);

        var aliases = db.ArticleAliases
            .Where(a => a.ArticleId == article.Id)
            .Select(a => a.AliasText)
            .ToList();

        Assert.Equal(2, aliases.Count);
        Assert.Contains("KeepAlias", aliases);
        Assert.Contains("NewAlias", aliases);
        Assert.DoesNotContain("RemoveAlias", aliases);

        var updated = db.Articles.Single(a => a.Id == article.Id);
        Assert.Equal(memberId, updated.LastModifiedBy);
        Assert.NotNull(updated.ModifiedAt);
    }
}
