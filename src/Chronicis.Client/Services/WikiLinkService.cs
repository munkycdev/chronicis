using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for wiki link operations including creating articles from autocomplete
/// and finding wiki folder locations.
/// </summary>
public interface IWikiLinkService
{
    /// <summary>
    /// Creates a new article from autocomplete and returns it.
    /// The article is created in the Wiki folder if one exists.
    /// </summary>
    Task<ArticleDto?> CreateArticleFromAutocompleteAsync(string articleName, Guid worldId);
    
    /// <summary>
    /// Finds the Wiki folder for a given world.
    /// </summary>
    Task<Guid?> FindWikiFolderAsync(Guid worldId);
}

public class WikiLinkService : IWikiLinkService
{
    private readonly IArticleApiService _articleApi;
    private readonly ILogger<WikiLinkService> _logger;

    public WikiLinkService(IArticleApiService articleApi, ILogger<WikiLinkService> logger)
    {
        _articleApi = articleApi;
        _logger = logger;
    }

    public async Task<ArticleDto?> CreateArticleFromAutocompleteAsync(string articleName, Guid worldId)
    {
        if (string.IsNullOrWhiteSpace(articleName))
        {
            _logger.LogWarning("Cannot create article with empty name");
            return null;
        }

        try
        {
            _logger.LogInformation("Creating new article from autocomplete: {Name} in world {WorldId}", 
                articleName, worldId);

            // Find the Wiki folder for this world
            Guid? wikiParentId = await FindWikiFolderAsync(worldId);
            
            if (wikiParentId == null)
            {
                _logger.LogWarning("No Wiki folder found for world {WorldId}, creating at world root", worldId);
            }

            var createDto = new ArticleCreateDto
            {
                Title = articleName,
                Body = string.Empty,
                ParentId = wikiParentId,
                EffectiveDate = DateTime.Now,
                WorldId = worldId
            };

            var created = await _articleApi.CreateArticleAsync(createDto);
            
            if (created != null)
            {
                _logger.LogInformation("Created article {Id} with title {Title} under parent {ParentId}", 
                    created.Id, created.Title, wikiParentId);
            }
            else
            {
                _logger.LogWarning("Failed to create article from autocomplete");
            }

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating article from autocomplete: {Name}", articleName);
            return null;
        }
    }

    public async Task<Guid?> FindWikiFolderAsync(Guid worldId)
    {
        try
        {
            // The Wiki folder is typically a child of the World article
            var rootArticles = await _articleApi.GetRootArticlesAsync(worldId);
            
            if (rootArticles.Any())
            {
                var worldArticle = rootArticles.First();
                var worldChildren = await _articleApi.GetChildrenAsync(worldArticle.Id);
                
                // Look for an article named "Wiki" (case-insensitive)
                var wikiFolder = worldChildren.FirstOrDefault(a => 
                    a.Title.Equals("Wiki", StringComparison.OrdinalIgnoreCase));
                
                if (wikiFolder != null)
                {
                    _logger.LogDebug("Found Wiki folder {WikiId} under world {WorldId}", wikiFolder.Id, worldId);
                    return wikiFolder.Id;
                }
            }
            
            // Fallback: check if Wiki is at root level
            var rootWiki = rootArticles.FirstOrDefault(a => 
                a.Title.Equals("Wiki", StringComparison.OrdinalIgnoreCase));
            
            if (rootWiki != null)
            {
                _logger.LogDebug("Found Wiki folder {WikiId} at root level", rootWiki.Id);
                return rootWiki.Id;
            }
            
            _logger.LogDebug("No Wiki folder found for world {WorldId}", worldId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding Wiki folder for world {WorldId}", worldId);
            return null;
        }
    }
}
