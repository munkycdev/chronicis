namespace Chronicis.Shared.Tests.Models;

/// <summary>
/// Tests for the Article domain model.
/// </summary>
public class ArticleTests
{
    [Fact]
    public void Article_HasParameterlessConstructor()
    {
        var article = new Article();
        Assert.NotNull(article);
    }

    [Fact]
    public void Article_DefaultValues_AreCorrect()
    {
        var article = new Article();

        Assert.Equal(string.Empty, article.Title);
        Assert.Equal(string.Empty, article.Slug);
        Assert.Equal(ArticleType.WikiArticle, article.Type);
        Assert.Equal(ArticleVisibility.Public, article.Visibility);
        Assert.NotNull(article.OutgoingLinks);
        Assert.NotNull(article.IncomingLinks);
        Assert.NotNull(article.Aliases);
        Assert.NotNull(article.ExternalLinks);
        Assert.NotNull(article.Images);
        Assert.False(article.SummaryIncludeWebSources);
    }

    [Fact]
    public void Article_CreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var article = new Article();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(article.CreatedAt, before, after);
    }

    [Fact]
    public void Article_EffectiveDate_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var article = new Article();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(article.EffectiveDate, before, after);
    }

    [Fact]
    public void Article_Properties_CanBeSetAndRetrieved()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var article = new Article
        {
            Id = id,
            ParentId = parentId,
            WorldId = worldId,
            Title = "Test Article",
            Slug = "test-article",
            Body = "Article content",
            Type = ArticleType.Session,
            Visibility = ArticleVisibility.Private,
            CreatedBy = createdBy,
            CreatedAt = now,
            IconEmoji = "📝"
        };

        Assert.Equal(id, article.Id);
        Assert.Equal(parentId, article.ParentId);
        Assert.Equal(worldId, article.WorldId);
        Assert.Equal("Test Article", article.Title);
        Assert.Equal("test-article", article.Slug);
        Assert.Equal("Article content", article.Body);
        Assert.Equal(ArticleType.Session, article.Type);
        Assert.Equal(ArticleVisibility.Private, article.Visibility);
        Assert.Equal(createdBy, article.CreatedBy);
        Assert.Equal(now, article.CreatedAt);
        Assert.Equal("📝", article.IconEmoji);
    }

    [Fact]
    public void Article_SessionSpecificFields_CanBeSet()
    {
        var sessionDate = DateTime.UtcNow;
        var article = new Article
        {
            Type = ArticleType.Session,
            SessionDate = sessionDate,
            InGameDate = "Mirtul 15, 1492 DR"
        };

        Assert.Equal(sessionDate, article.SessionDate);
        Assert.Equal("Mirtul 15, 1492 DR", article.InGameDate);
    }

    [Fact]
    public void Article_CharacterSpecificFields_CanBeSet()
    {
        var playerId = Guid.NewGuid();
        var article = new Article
        {
            Type = ArticleType.Character,
            PlayerId = playerId
        };

        Assert.Equal(playerId, article.PlayerId);
    }

    [Fact]
    public void Article_AISummaryFields_CanBeSet()
    {
        var summaryGeneratedAt = DateTime.UtcNow;
        var templateId = Guid.NewGuid();

        var article = new Article
        {
            SummaryTemplateId = templateId,
            SummaryCustomPrompt = "Custom prompt",
            SummaryIncludeWebSources = true,
            AISummary = "Generated summary",
            AISummaryGeneratedAt = summaryGeneratedAt
        };

        Assert.Equal(templateId, article.SummaryTemplateId);
        Assert.Equal("Custom prompt", article.SummaryCustomPrompt);
        Assert.True(article.SummaryIncludeWebSources);
        Assert.Equal("Generated summary", article.AISummary);
        Assert.Equal(summaryGeneratedAt, article.AISummaryGeneratedAt);
    }

    [Fact]
    public void Article_ChildCount_ReturnsZero_WhenNoChildren()
    {
        var article = new Article();
        Assert.Equal(0, article.ChildCount);
    }

    [Fact]
    public void Article_ChildCount_ReturnsCorrectCount_WhenChildrenExist()
    {
        var article = new Article
        {
            Children = new List<Article>
            {
                new() { Title = "Child 1" },
                new() { Title = "Child 2" },
                new() { Title = "Child 3" }
            }
        };

        Assert.Equal(3, article.ChildCount);
    }

    [Fact]
    public void Article_NavigationProperties_InitializeAsEmpty()
    {
        var article = new Article();

        Assert.Empty(article.OutgoingLinks);
        Assert.Empty(article.IncomingLinks);
        Assert.Empty(article.Aliases);
        Assert.Empty(article.ExternalLinks);
        Assert.Empty(article.Images);
    }

    [Fact]
    public void Article_NullableNavigationProperties_AreNull()
    {
        var article = new Article();

        Assert.Null(article.Parent);
        Assert.Null(article.Children);
        Assert.Null(article.World);
        Assert.Null(article.Campaign);
        Assert.Null(article.Arc);
        Assert.Null(article.SummaryTemplate);
        Assert.Null(article.Modifier);
        Assert.Null(article.Player);
    }

    [Fact]
    public void Article_HierarchyFields_SupportNesting()
    {
        var parentId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var arcId = Guid.NewGuid();

        var article = new Article
        {
            ParentId = parentId,
            WorldId = worldId,
            CampaignId = campaignId,
            ArcId = arcId
        };

        Assert.Equal(parentId, article.ParentId);
        Assert.Equal(worldId, article.WorldId);
        Assert.Equal(campaignId, article.CampaignId);
        Assert.Equal(arcId, article.ArcId);
    }

    [Fact]
    public void Article_AuditFields_TrackChanges()
    {
        var createdBy = Guid.NewGuid();
        var modifiedBy = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var modifiedAt = DateTime.UtcNow;

        var article = new Article
        {
            CreatedBy = createdBy,
            LastModifiedBy = modifiedBy,
            CreatedAt = createdAt,
            ModifiedAt = modifiedAt
        };

        Assert.Equal(createdBy, article.CreatedBy);
        Assert.Equal(modifiedBy, article.LastModifiedBy);
        Assert.Equal(createdAt, article.CreatedAt);
        Assert.Equal(modifiedAt, article.ModifiedAt);
    }
}
