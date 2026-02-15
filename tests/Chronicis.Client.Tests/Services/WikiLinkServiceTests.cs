using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class WikiLinkServiceTests
{
    private readonly IArticleApiService _articleApi;
    private readonly ILogger<WikiLinkService> _logger;
    private readonly WikiLinkService _sut;

    public WikiLinkServiceTests()
    {
        _articleApi = Substitute.For<IArticleApiService>();
        _logger = Substitute.For<ILogger<WikiLinkService>>();
        _sut = new WikiLinkService(_articleApi, _logger);
    }

    // ════════════════════════════════════════════════════════════════
    //  CreateArticleFromAutocompleteAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateArticleFromAutocompleteAsync_WithNullName_ReturnsNull()
    {
        // Act
        var result = await _sut.CreateArticleFromAutocompleteAsync(null!, Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateArticleFromAutocompleteAsync_WithEmptyName_ReturnsNull()
    {
        // Act
        var result = await _sut.CreateArticleFromAutocompleteAsync(string.Empty, Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateArticleFromAutocompleteAsync_WithWhitespaceName_ReturnsNull()
    {
        // Act
        var result = await _sut.CreateArticleFromAutocompleteAsync("   ", Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateArticleFromAutocompleteAsync_WithValidName_CreatesArticle()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var articleName = "New Article";
        var createdArticle = new ArticleDto { Id = Guid.NewGuid(), Title = articleName };

        _articleApi.GetRootArticlesAsync(worldId).Returns(new List<ArticleTreeDto>());
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(createdArticle);

        // Act
        var result = await _sut.CreateArticleFromAutocompleteAsync(articleName, worldId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(articleName, result.Title);
        await _articleApi.Received(1).CreateArticleAsync(Arg.Is<ArticleCreateDto>(dto =>
            dto.Title == articleName &&
            dto.WorldId == worldId &&
            dto.Body == string.Empty));
    }

    [Fact]
    public async Task CreateArticleFromAutocompleteAsync_FindsWikiFolder_CreatesArticleInside()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var worldArticleId = Guid.NewGuid();
        var wikiFolderId = Guid.NewGuid();
        var articleName = "New Article";
        var createdArticle = new ArticleDto { Id = Guid.NewGuid(), Title = articleName };

        var rootArticles = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = worldArticleId, Title = "World" }
        };
        var worldChildren = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = wikiFolderId, Title = "Wiki" }
        };

        _articleApi.GetRootArticlesAsync(worldId).Returns(rootArticles);
        _articleApi.GetChildrenAsync(worldArticleId).Returns(worldChildren);
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(createdArticle);

        // Act
        var result = await _sut.CreateArticleFromAutocompleteAsync(articleName, worldId);

        // Assert
        Assert.NotNull(result);
        await _articleApi.Received(1).CreateArticleAsync(Arg.Is<ArticleCreateDto>(dto =>
            dto.ParentId == wikiFolderId));
    }

    [Fact]
    public async Task CreateArticleFromAutocompleteAsync_WhenApiThrows_ReturnsNull()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        _articleApi.GetRootArticlesAsync(worldId).Returns(Task.FromException<List<ArticleTreeDto>>(new Exception("API error")));

        // Act
        var result = await _sut.CreateArticleFromAutocompleteAsync("Article Name", worldId);

        // Assert
        Assert.Null(result);
    }

    // ════════════════════════════════════════════════════════════════
    //  FindWikiFolderAsync Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FindWikiFolderAsync_WithNoRootArticles_ReturnsNull()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        _articleApi.GetRootArticlesAsync(worldId).Returns(new List<ArticleTreeDto>());

        // Act
        var result = await _sut.FindWikiFolderAsync(worldId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindWikiFolderAsync_WithWikiFolderAsChildOfWorld_ReturnsId()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var worldArticleId = Guid.NewGuid();
        var wikiFolderId = Guid.NewGuid();

        var rootArticles = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = worldArticleId, Title = "World" }
        };
        var worldChildren = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = wikiFolderId, Title = "Wiki" },
            new ArticleTreeDto { Id = Guid.NewGuid(), Title = "Other" }
        };

        _articleApi.GetRootArticlesAsync(worldId).Returns(rootArticles);
        _articleApi.GetChildrenAsync(worldArticleId).Returns(worldChildren);

        // Act
        var result = await _sut.FindWikiFolderAsync(worldId);

        // Assert
        Assert.Equal(wikiFolderId, result);
    }

    [Fact]
    public async Task FindWikiFolderAsync_WithWikiFolderAtRoot_ReturnsId()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var worldArticleId = Guid.NewGuid();
        var wikiFolderId = Guid.NewGuid();

        var rootArticles = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = worldArticleId, Title = "World" },
            new ArticleTreeDto { Id = wikiFolderId, Title = "Wiki" }
        };

        _articleApi.GetRootArticlesAsync(worldId).Returns(rootArticles);
        _articleApi.GetChildrenAsync(worldArticleId).Returns(new List<ArticleTreeDto>());

        // Act
        var result = await _sut.FindWikiFolderAsync(worldId);

        // Assert
        Assert.Equal(wikiFolderId, result);
    }

    [Fact]
    public async Task FindWikiFolderAsync_IsCaseInsensitive()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var worldArticleId = Guid.NewGuid();
        var wikiFolderId = Guid.NewGuid();

        var rootArticles = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = worldArticleId, Title = "World" }
        };
        var worldChildren = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = wikiFolderId, Title = "WIKI" }
        };

        _articleApi.GetRootArticlesAsync(worldId).Returns(rootArticles);
        _articleApi.GetChildrenAsync(worldArticleId).Returns(worldChildren);

        // Act
        var result = await _sut.FindWikiFolderAsync(worldId);

        // Assert
        Assert.Equal(wikiFolderId, result);
    }

    [Fact]
    public async Task FindWikiFolderAsync_WithNoWikiFolder_ReturnsNull()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var worldArticleId = Guid.NewGuid();

        var rootArticles = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = worldArticleId, Title = "World" }
        };
        var worldChildren = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = Guid.NewGuid(), Title = "Not Wiki" }
        };

        _articleApi.GetRootArticlesAsync(worldId).Returns(rootArticles);
        _articleApi.GetChildrenAsync(worldArticleId).Returns(worldChildren);

        // Act
        var result = await _sut.FindWikiFolderAsync(worldId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindWikiFolderAsync_WhenApiThrows_ReturnsNull()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        _articleApi.GetRootArticlesAsync(worldId).Returns(Task.FromException<List<ArticleTreeDto>>(new Exception("API error")));

        // Act
        var result = await _sut.FindWikiFolderAsync(worldId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindWikiFolderAsync_PrefersChildOfWorldOverRoot()
    {
        // Arrange - Wiki exists both as child of world AND at root level
        var worldId = Guid.NewGuid();
        var worldArticleId = Guid.NewGuid();
        var wikiAsChildId = Guid.NewGuid();
        var wikiAtRootId = Guid.NewGuid();

        var rootArticles = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = worldArticleId, Title = "World" },
            new ArticleTreeDto { Id = wikiAtRootId, Title = "Wiki" } // This should NOT be returned
        };
        var worldChildren = new List<ArticleTreeDto>
        {
            new ArticleTreeDto { Id = wikiAsChildId, Title = "Wiki" } // This SHOULD be returned
        };

        _articleApi.GetRootArticlesAsync(worldId).Returns(rootArticles);
        _articleApi.GetChildrenAsync(worldArticleId).Returns(worldChildren);

        // Act
        var result = await _sut.FindWikiFolderAsync(worldId);

        // Assert
        Assert.Equal(wikiAsChildId, result); // Should return the child, not the root
    }
}
