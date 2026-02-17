namespace Chronicis.Shared.Tests.DTOs;

/// <summary>
/// Tests for Article-related DTOs to ensure proper initialization and data integrity.
/// </summary>
public class ArticleDtosTests
{
    // ────────────────────────────────────────────────────────────────
    //  ArticleDto
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ArticleDto_HasParameterlessConstructor()
    {
        var dto = new ArticleDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void ArticleDto_DefaultValues_AreCorrect()
    {
        var dto = new ArticleDto();

        Assert.Equal(string.Empty, dto.Title);
        Assert.Equal(string.Empty, dto.Slug);
        Assert.Equal(string.Empty, dto.Body);
        Assert.False(dto.HasChildren);
        Assert.Equal(0, dto.ChildCount);
        Assert.NotNull(dto.Breadcrumbs);
        Assert.Empty(dto.Breadcrumbs);
        Assert.NotNull(dto.Aliases);
        Assert.Empty(dto.Aliases);
        Assert.NotNull(dto.ExternalLinks);
        Assert.Empty(dto.ExternalLinks);
    }

    [Fact]
    public void ArticleDto_Properties_CanBeSetAndRetrieved()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var dto = new ArticleDto
        {
            Id = id,
            Title = "Test Article",
            Slug = "test-article",
            ParentId = parentId,
            WorldId = worldId,
            Body = "Article body",
            Type = ArticleType.WikiArticle,
            Visibility = ArticleVisibility.Public,
            CreatedAt = now,
            CreatedBy = createdBy,
            HasChildren = true,
            ChildCount = 5
        };

        Assert.Equal(id, dto.Id);
        Assert.Equal("Test Article", dto.Title);
        Assert.Equal("test-article", dto.Slug);
        Assert.Equal(parentId, dto.ParentId);
        Assert.Equal(worldId, dto.WorldId);
        Assert.Equal("Article body", dto.Body);
        Assert.Equal(ArticleType.WikiArticle, dto.Type);
        Assert.Equal(ArticleVisibility.Public, dto.Visibility);
        Assert.Equal(now, dto.CreatedAt);
        Assert.Equal(createdBy, dto.CreatedBy);
        Assert.True(dto.HasChildren);
        Assert.Equal(5, dto.ChildCount);
    }

    // ────────────────────────────────────────────────────────────────
    //  ArticleTreeDto
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ArticleTreeDto_HasParameterlessConstructor()
    {
        var dto = new ArticleTreeDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void ArticleTreeDto_DefaultValues_AreCorrect()
    {
        var dto = new ArticleTreeDto();

        Assert.Equal(string.Empty, dto.Title);
        Assert.Equal(string.Empty, dto.Slug);
        Assert.False(dto.HasChildren);
        Assert.Equal(0, dto.ChildCount);
        Assert.False(dto.HasAISummary);
        Assert.False(dto.IsVirtualGroup);
        Assert.NotNull(dto.Aliases);
        Assert.Empty(dto.Aliases);
    }

    [Fact]
    public void ArticleTreeDto_Properties_CanBeSetAndRetrieved()
    {
        var id = Guid.NewGuid();
        var dto = new ArticleTreeDto
        {
            Id = id,
            Title = "Test",
            Slug = "test",
            Type = ArticleType.Character,
            HasChildren = true,
            ChildCount = 3,
            HasAISummary = true,
            IsVirtualGroup = true
        };

        Assert.Equal(id, dto.Id);
        Assert.Equal("Test", dto.Title);
        Assert.True(dto.HasAISummary);
        Assert.True(dto.IsVirtualGroup);
    }

    // ────────────────────────────────────────────────────────────────
    //  ArticleCreateDto
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ArticleCreateDto_HasParameterlessConstructor()
    {
        var dto = new ArticleCreateDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void ArticleCreateDto_DefaultValues_AreCorrect()
    {
        var dto = new ArticleCreateDto();

        Assert.Equal(string.Empty, dto.Title);
        Assert.Equal(string.Empty, dto.Body);
        Assert.Equal(ArticleType.WikiArticle, dto.Type);
        Assert.Equal(ArticleVisibility.Public, dto.Visibility);
    }

    [Fact]
    public void ArticleCreateDto_Properties_CanBeSetAndRetrieved()
    {
        var parentId = Guid.NewGuid();
        var dto = new ArticleCreateDto
        {
            Title = "New Article",
            Slug = "new-article",
            ParentId = parentId,
            Body = "Content",
            Type = ArticleType.Session,
            Visibility = ArticleVisibility.Private
        };

        Assert.Equal("New Article", dto.Title);
        Assert.Equal("new-article", dto.Slug);
        Assert.Equal(parentId, dto.ParentId);
        Assert.Equal(ArticleType.Session, dto.Type);
        Assert.Equal(ArticleVisibility.Private, dto.Visibility);
    }

    // ────────────────────────────────────────────────────────────────
    //  ArticleUpdateDto
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ArticleUpdateDto_HasParameterlessConstructor()
    {
        var dto = new ArticleUpdateDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void ArticleUpdateDto_DefaultValues_AreCorrect()
    {
        var dto = new ArticleUpdateDto();

        Assert.Equal(string.Empty, dto.Title);
        Assert.Equal(string.Empty, dto.Body);
        Assert.Null(dto.Visibility);
        Assert.Null(dto.Type);
    }

    [Fact]
    public void ArticleUpdateDto_Properties_CanBeSetAndRetrieved()
    {
        var date = DateTime.UtcNow;
        var dto = new ArticleUpdateDto
        {
            Title = "Updated Title",
            Slug = "updated-slug",
            Body = "Updated body",
            EffectiveDate = date,
            Visibility = ArticleVisibility.MembersOnly,
            Type = ArticleType.Character
        };

        Assert.Equal("Updated Title", dto.Title);
        Assert.Equal("updated-slug", dto.Slug);
        Assert.Equal(date, dto.EffectiveDate);
        Assert.Equal(ArticleVisibility.MembersOnly, dto.Visibility);
        Assert.Equal(ArticleType.Character, dto.Type);
    }

    // ────────────────────────────────────────────────────────────────
    //  BreadcrumbDto
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void BreadcrumbDto_HasParameterlessConstructor()
    {
        var dto = new BreadcrumbDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void BreadcrumbDto_DefaultValues_AreCorrect()
    {
        var dto = new BreadcrumbDto();

        Assert.Equal(string.Empty, dto.Title);
        Assert.Equal(string.Empty, dto.Slug);
        Assert.False(dto.IsWorld);
    }

    [Fact]
    public void BreadcrumbDto_Properties_CanBeSetAndRetrieved()
    {
        var id = Guid.NewGuid();
        var dto = new BreadcrumbDto
        {
            Id = id,
            Title = "Breadcrumb",
            Slug = "breadcrumb",
            Type = ArticleType.WikiArticle,
            IsWorld = true
        };

        Assert.Equal(id, dto.Id);
        Assert.Equal("Breadcrumb", dto.Title);
        Assert.True(dto.IsWorld);
    }

    // ────────────────────────────────────────────────────────────────
    //  ArticleMoveDto
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ArticleMoveDto_HasParameterlessConstructor()
    {
        var dto = new ArticleMoveDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void ArticleMoveDto_DefaultValues_AreCorrect()
    {
        var dto = new ArticleMoveDto();
        Assert.Null(dto.NewParentId);
    }

    [Fact]
    public void ArticleMoveDto_Properties_CanBeSetAndRetrieved()
    {
        var newParentId = Guid.NewGuid();
        var dto = new ArticleMoveDto
        {
            NewParentId = newParentId
        };

        Assert.Equal(newParentId, dto.NewParentId);
    }

    // ────────────────────────────────────────────────────────────────
    //  ArticleAliasDto
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ArticleAliasDto_HasParameterlessConstructor()
    {
        var dto = new ArticleAliasDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void ArticleAliasDto_DefaultValues_AreCorrect()
    {
        var dto = new ArticleAliasDto();
        Assert.Equal(string.Empty, dto.AliasText);
    }

    [Fact]
    public void ArticleAliasDto_Properties_CanBeSetAndRetrieved()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new ArticleAliasDto
        {
            Id = id,
            AliasText = "Alternative Name",
            AliasType = "Nickname",
            EffectiveDate = now,
            CreatedAt = now
        };

        Assert.Equal(id, dto.Id);
        Assert.Equal("Alternative Name", dto.AliasText);
        Assert.Equal("Nickname", dto.AliasType);
        Assert.Equal(now, dto.EffectiveDate);
    }

    // ────────────────────────────────────────────────────────────────
    //  ArticleAliasesUpdateDto
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ArticleAliasesUpdateDto_HasParameterlessConstructor()
    {
        var dto = new ArticleAliasesUpdateDto();
        Assert.NotNull(dto);
    }

    [Fact]
    public void ArticleAliasesUpdateDto_DefaultValues_AreCorrect()
    {
        var dto = new ArticleAliasesUpdateDto();
        Assert.Equal(string.Empty, dto.Aliases);
    }

    [Fact]
    public void ArticleAliasesUpdateDto_Properties_CanBeSetAndRetrieved()
    {
        var dto = new ArticleAliasesUpdateDto
        {
            Aliases = "Alias1, Alias2, Alias3"
        };

        Assert.Equal("Alias1, Alias2, Alias3", dto.Aliases);
    }
}
