using Chronicis.Api.Data;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;

namespace Chronicis.Api.Tests;

/// <summary>
/// Shared test utilities and common test data factories.
/// </summary>
public static class TestHelpers
{
    // ────────────────────────────────────────────────────────────────
    //  Fixed GUIDs for common test entities
    // ────────────────────────────────────────────────────────────────

    public static class FixedIds
    {
        // Worlds
        public static readonly Guid World1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public static readonly Guid World2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        // Users
        public static readonly Guid User1 = Guid.Parse("40000000-0000-0000-0000-000000000001");
        public static readonly Guid User2 = Guid.Parse("40000000-0000-0000-0000-000000000002");
        public static readonly Guid User3 = Guid.Parse("40000000-0000-0000-0000-000000000003");

        // World Members
        public static readonly Guid Member1 = Guid.Parse("50000000-0000-0000-0000-000000000001");
        public static readonly Guid Member2 = Guid.Parse("50000000-0000-0000-0000-000000000002");
        public static readonly Guid Member3 = Guid.Parse("50000000-0000-0000-0000-000000000003");

        // Articles
        public static readonly Guid Article1 = Guid.Parse("10000000-0000-0000-0000-000000000001");
        public static readonly Guid Article2 = Guid.Parse("10000000-0000-0000-0000-000000000002");
        public static readonly Guid Article3 = Guid.Parse("10000000-0000-0000-0000-000000000003");
        public static readonly Guid Article4 = Guid.Parse("10000000-0000-0000-0000-000000000004");
        public static readonly Guid Article5 = Guid.Parse("10000000-0000-0000-0000-000000000005");

        // Campaigns & Arcs
        public static readonly Guid Campaign1 = Guid.Parse("20000000-0000-0000-0000-000000000001");
        public static readonly Guid Arc1 = Guid.Parse("30000000-0000-0000-0000-000000000001");
    }

    // ────────────────────────────────────────────────────────────────
    //  User factories
    // ────────────────────────────────────────────────────────────────

    public static User CreateUser(
        Guid? id = null,
        string? auth0UserId = null,
        string? email = null,
        string? displayName = null)
    {
        var userId = id ?? Guid.NewGuid();
        return new User
        {
            Id = userId,
            Auth0UserId = auth0UserId ?? $"auth0|{userId:N}",
            Email = email ?? $"user{userId:N}@test.com",
            DisplayName = displayName ?? $"User {userId:N}",
            HasCompletedOnboarding = true
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  World factories
    // ────────────────────────────────────────────────────────────────

    public static World CreateWorld(
        Guid? id = null,
        Guid? ownerId = null,
        string? name = null,
        string? slug = null)
    {
        var worldId = id ?? Guid.NewGuid();
        var worldName = name ?? $"Test World {worldId:N}";
        return new World
        {
            Id = worldId,
            Name = worldName,
            Slug = slug ?? worldName.ToLowerInvariant().Replace(" ", "-"),
            OwnerId = ownerId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static WorldMember CreateWorldMember(
        Guid? id = null,
        Guid? worldId = null,
        Guid? userId = null,
        WorldRole role = WorldRole.Player)
    {
        return new WorldMember
        {
            Id = id ?? Guid.NewGuid(),
            WorldId = worldId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  Article factories
    // ────────────────────────────────────────────────────────────────

    public static Article CreateArticle(
        Guid? id = null,
        Guid? worldId = null,
        Guid? parentId = null,
        Guid? createdBy = null,
        string? title = null,
        string? slug = null,
        string? body = null,
        ArticleType type = ArticleType.WikiArticle,
        ArticleVisibility visibility = ArticleVisibility.Public,
        Guid? campaignId = null,
        Guid? arcId = null,
        DateTime? effectiveDate = null)
    {
        var articleId = id ?? Guid.NewGuid();
        var articleTitle = title ?? $"Test Article {articleId:N}";
        return new Article
        {
            Id = articleId,
            WorldId = worldId ?? Guid.NewGuid(),
            ParentId = parentId,
            Title = articleTitle,
            Slug = slug ?? articleTitle.ToLowerInvariant().Replace(" ", "-"),
            Body = body,
            Type = type,
            Visibility = visibility,
            CreatedBy = createdBy ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            EffectiveDate = effectiveDate ?? DateTime.UtcNow,
            CampaignId = campaignId,
            ArcId = arcId
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  Campaign & Arc factories
    // ────────────────────────────────────────────────────────────────

    public static Campaign CreateCampaign(
        Guid? id = null,
        Guid? worldId = null,
        string? name = null,
        bool isActive = false)
    {
        return new Campaign
        {
            Id = id ?? Guid.NewGuid(),
            WorldId = worldId ?? Guid.NewGuid(),
            Name = name ?? "Test Campaign",
            IsActive = isActive
        };
    }

    public static Arc CreateArc(
        Guid? id = null,
        Guid? campaignId = null,
        string? name = null,
        int sortOrder = 1)
    {
        return new Arc
        {
            Id = id ?? Guid.NewGuid(),
            CampaignId = campaignId ?? Guid.NewGuid(),
            Name = name ?? "Test Arc",
            SortOrder = sortOrder
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  Common seed data scenarios
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds a basic world with owner and members.
    /// Returns (world, owner, member).
    /// </summary>
    public static (World world, User owner, User member) SeedBasicWorld(
        ChronicisDbContext context,
        Guid? worldId = null,
        Guid? ownerId = null,
        Guid? memberId = null)
    {
        var owner = CreateUser(id: ownerId ?? FixedIds.User1);
        var member = CreateUser(id: memberId ?? FixedIds.User2);
        
        context.Users.AddRange(owner, member);

        var world = CreateWorld(
            id: worldId ?? FixedIds.World1,
            ownerId: owner.Id,
            name: "Test World",
            slug: "test-world");
        
        context.Worlds.Add(world);

        context.WorldMembers.AddRange(
            CreateWorldMember(id: FixedIds.Member1, worldId: world.Id, userId: owner.Id, role: WorldRole.GM),
            CreateWorldMember(id: FixedIds.Member2, worldId: world.Id, userId: member.Id, role: WorldRole.Player)
        );

        context.SaveChanges();

        return (world, owner, member);
    }

    /// <summary>
    /// Seeds a basic article hierarchy: Root -> Child -> Grandchild.
    /// Requires world to already exist.
    /// </summary>
    public static (Article root, Article child, Article grandchild) SeedArticleHierarchy(
        ChronicisDbContext context,
        Guid worldId,
        Guid userId)
    {
        var root = CreateArticle(
            id: FixedIds.Article1,
            worldId: worldId,
            createdBy: userId,
            title: "Root Article",
            slug: "root-article");

        var child = CreateArticle(
            id: FixedIds.Article2,
            worldId: worldId,
            parentId: root.Id,
            createdBy: userId,
            title: "Child Article",
            slug: "child-article");

        var grandchild = CreateArticle(
            id: FixedIds.Article3,
            worldId: worldId,
            parentId: child.Id,
            createdBy: userId,
            title: "Grandchild Article",
            slug: "grandchild-article");

        context.Articles.AddRange(root, child, grandchild);
        context.SaveChanges();

        return (root, child, grandchild);
    }
}
