namespace Chronicis.Api.Services;

/// <summary>
/// Service for synchronizing hashtags when articles are saved
/// </summary>
public interface IHashtagSyncService
{
    /// <summary>
    /// Synchronizes hashtags for an article based on its current body content.
    /// - Removes hashtags that no longer exist in the body
    /// - Adds new hashtags found in the body
    /// - Updates positions for existing hashtags
    /// - Auto-links hashtags to articles with matching titles
    /// </summary>
    /// <param name="articleId">The article to sync hashtags for</param>
    /// <param name="body">The current body text of the article</param>
    Task SyncHashtagsAsync(Guid articleId, string body);

    /// <summary>
    /// Attempts to auto-link any unlinked hashtags that match this article's title.
    /// Called when an article is created or updated.
    /// </summary>
    /// <param name="articleId">The article whose title to check</param>
    Task LinkHashtagsToArticleByTitleAsync(Guid articleId);
}
