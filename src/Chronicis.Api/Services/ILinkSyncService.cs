namespace Chronicis.Api.Services;

/// <summary>
/// Service for synchronizing wiki links in the database based on article content.
/// </summary>
public interface ILinkSyncService
{
    /// <summary>
    /// Synchronizes the ArticleLink table for the given article.
    /// Removes all existing links for this article and creates new ones based on the body content.
    /// </summary>
    /// <param name="sourceArticleId">The ID of the article whose links should be synced.</param>
    /// <param name="body">The article body containing wiki links to parse.</param>
    Task SyncLinksAsync(Guid sourceArticleId, string? body);
}
