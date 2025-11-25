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
    /// </summary>
    /// <param name="articleId">The article to sync hashtags for</param>
    /// <param name="body">The current body text of the article</param>
    Task SyncHashtagsAsync(int articleId, string body);
}
