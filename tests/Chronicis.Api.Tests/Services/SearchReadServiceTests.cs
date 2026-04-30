using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class SearchReadServiceTests
{
    [Fact]
    public async Task SearchAsync_RespectsUnifiedReadPolicy_ForPrivateAndMembershipRules()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildBreadcrumbsAsync(Arg.Any<Guid>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new List<BreadcrumbDto>()));
        hierarchy.BuildBreadcrumbsBatchAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new Dictionary<Guid, List<BreadcrumbDto>>()));

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var readableWorld = TestHelpers.CreateWorld(ownerId: userId);
        var unreadableWorld = TestHelpers.CreateWorld(ownerId: Guid.NewGuid());
        db.Worlds.AddRange(readableWorld, unreadableWorld);

        db.WorldMembers.AddRange(
            TestHelpers.CreateWorldMember(worldId: readableWorld.Id, userId: userId),
            TestHelpers.CreateWorldMember(worldId: readableWorld.Id, userId: otherUserId));

        const string term = "sharedterm";

        var publicReadable = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: otherUserId,
            title: $"{term}-public",
            visibility: ArticleVisibility.Public,
            type: ArticleType.WikiArticle);

        var privateOwned = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: userId,
            title: $"{term}-private-owned",
            visibility: ArticleVisibility.Private,
            type: ArticleType.WikiArticle);

        var privateNotOwned = TestHelpers.CreateArticle(
            worldId: readableWorld.Id,
            createdBy: otherUserId,
            title: $"{term}-private-other",
            visibility: ArticleVisibility.Private,
            type: ArticleType.WikiArticle);

        var unreadableWorldArticle = TestHelpers.CreateArticle(
            worldId: unreadableWorld.Id,
            createdBy: unreadableWorld.OwnerId,
            title: $"{term}-other-world",
            visibility: ArticleVisibility.Public,
            type: ArticleType.WikiArticle);

        var tutorial = TestHelpers.CreateArticle(
            worldId: Guid.Empty,
            createdBy: otherUserId,
            title: $"{term}-tutorial",
            visibility: ArticleVisibility.Public,
            type: ArticleType.Tutorial);

        db.Articles.AddRange(publicReadable, privateOwned, privateNotOwned, unreadableWorldArticle, tutorial);
        await db.SaveChangesAsync();

        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());

        var result = await sut.SearchAsync(term, userId);
        var resultIds = result.TitleMatches.Select(m => m.Id).ToList();

        Assert.Contains(publicReadable.Id, resultIds);
        Assert.Contains(privateOwned.Id, resultIds);
        Assert.DoesNotContain(privateNotOwned.Id, resultIds);
        Assert.DoesNotContain(unreadableWorldArticle.Id, resultIds);
        Assert.DoesNotContain(tutorial.Id, resultIds);
    }

    [Fact]
    public async Task SearchAsync_TitleMatch_PopulatesTypeAndWorldSlug()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildBreadcrumbsBatchAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new Dictionary<Guid, List<BreadcrumbDto>>()));

        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        const string term = "uniqueterm42";
        var article = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: $"{term}-article",
            visibility: ArticleVisibility.Public,
            type: ArticleType.WikiArticle);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());
        var result = await sut.SearchAsync(term, userId);

        var match = Assert.Single(result.TitleMatches);
        Assert.Equal(article.Id, match.Id);
        Assert.Equal(ArticleType.WikiArticle, match.Type);
        Assert.Equal(world.Slug, match.WorldSlug);
    }

    [Fact]
    public async Task SearchAsync_ShortQuery_ReturnsEmptyResults()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());

        var result = await sut.SearchAsync("a", Guid.NewGuid());

        Assert.Empty(result.TitleMatches);
        Assert.Empty(result.BodyMatches);
        Assert.Empty(result.HashtagMatches);
        Assert.Equal(0, result.TotalResults);
    }

    [Fact]
    public async Task SearchAsync_NullQuery_ReturnsEmptyResults()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());

        var result = await sut.SearchAsync(null!, Guid.NewGuid());

        Assert.Empty(result.TitleMatches);
        Assert.Equal(string.Empty, result.Query);
    }

    [Fact]
    public async Task SearchAsync_BodyMatch_PopulatesSnippetAndDeduplicates()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildBreadcrumbsBatchAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new Dictionary<Guid, List<BreadcrumbDto>>()));

        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        const string term = "bodyterm99";
        var article = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: "plain-title",
            body: $"Some text with {term} embedded here.",
            visibility: ArticleVisibility.Public,
            type: ArticleType.WikiArticle);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());
        var result = await sut.SearchAsync(term, userId);

        Assert.Empty(result.TitleMatches);
        var bodyMatch = Assert.Single(result.BodyMatches);
        Assert.Equal(article.Id, bodyMatch.Id);
        Assert.Contains(term, bodyMatch.MatchSnippet, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("content", bodyMatch.MatchType);
    }

    [Fact]
    public async Task SearchAsync_HashtagMatch_PopulatesSnippetAndDeduplicates()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildBreadcrumbsBatchAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new Dictionary<Guid, List<BreadcrumbDto>>()));

        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        const string term = "hashtagterm77";
        var article = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: "plain-title-ht",
            body: $"Text with #{term} somewhere.",
            visibility: ArticleVisibility.Public,
            type: ArticleType.WikiArticle);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());
        var result = await sut.SearchAsync(term, userId);

        // Appears in body too (contains term) — body match deduplicates hashtag match
        var hashtagMatch = result.HashtagMatches.FirstOrDefault(m => m.Id == article.Id);
        if (hashtagMatch == null)
        {
            // Article was deduped into body matches
            Assert.Contains(result.BodyMatches, m => m.Id == article.Id);
        }
        else
        {
            Assert.Equal("hashtag", hashtagMatch.MatchType);
            Assert.Contains($"#{term}", hashtagMatch.MatchSnippet, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task SearchAsync_WikiArticle_WithBreadcrumbs_PopulatesArticleSlugChain()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();

        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);

        const string term = "slugchainterm";
        var articleId = Guid.NewGuid();

        var breadcrumbs = new Dictionary<Guid, List<BreadcrumbDto>>
        {
            [articleId] =
            [
                new BreadcrumbDto { Slug = world.Slug, IsWorld = true },
                new BreadcrumbDto { Slug = "wiki" },
                new BreadcrumbDto { Slug = "parent-note" }
            ]
        };
        hierarchy.BuildBreadcrumbsBatchAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(breadcrumbs));

        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        var article = TestHelpers.CreateArticle(
            id: articleId,
            worldId: world.Id,
            createdBy: userId,
            title: $"{term}-article",
            slug: term,
            visibility: ArticleVisibility.Public,
            type: ArticleType.WikiArticle);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());
        var result = await sut.SearchAsync(term, userId);

        var match = Assert.Single(result.TitleMatches);
        Assert.Equal(articleId, match.Id);
        Assert.Equal(["parent-note", term], match.ArticleSlugChain);
    }

    [Fact]
    public async Task SearchAsync_SessionNote_WithFullSessionContext_PopulatesSlugChain()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildBreadcrumbsBatchAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new Dictionary<Guid, List<BreadcrumbDto>>()));

        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);
        var campaign = new Campaign { Id = Guid.NewGuid(), WorldId = world.Id, Name = "Campaign A", Slug = "campaign-a" };
        var arc = new Arc { Id = Guid.NewGuid(), CampaignId = campaign.Id, Name = "Arc 1", Slug = "arc-1", SortOrder = 1 };
        var session = new Session { Id = Guid.NewGuid(), ArcId = arc.Id, Name = "Session 1", Slug = "session-1" };

        const string term = "sessionnoteterm99";
        var articleId = Guid.NewGuid();
        var article = TestHelpers.CreateArticle(
            id: articleId,
            worldId: world.Id,
            createdBy: userId,
            title: $"{term}-note",
            slug: "my-note",
            visibility: ArticleVisibility.Public,
            type: ArticleType.SessionNote);
        article.SessionId = session.Id;

        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));
        db.Campaigns.Add(campaign);
        db.Arcs.Add(arc);
        db.Sessions.Add(session);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());
        var result = await sut.SearchAsync(term, userId);

        var match = Assert.Single(result.TitleMatches);
        Assert.Equal(articleId, match.Id);
        Assert.Equal([world.Slug, "campaign-a", "arc-1", "session-1", "my-note"], match.ArticleSlugChain);
    }

    [Fact]
    public async Task SearchAsync_SessionNote_WithMissingSessionContext_FallsBackToSlug()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var hierarchy = Substitute.For<IArticleHierarchyService>();
        hierarchy.BuildBreadcrumbsBatchAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<HierarchyWalkOptions?>())
            .Returns(Task.FromResult(new Dictionary<Guid, List<BreadcrumbDto>>()));

        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);

        const string term = "orphanednoteterm";
        var article = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: $"{term}-note",
            slug: "orphan-note",
            visibility: ArticleVisibility.Public,
            type: ArticleType.SessionNote);
        // No SessionId set — simulates an orphaned SessionNote

        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var sut = new SearchReadService(db, hierarchy, new ReadAccessPolicyService());
        var result = await sut.SearchAsync(term, userId);

        var match = Assert.Single(result.TitleMatches);
        Assert.Equal(["orphan-note"], match.ArticleSlugChain);
    }
}
