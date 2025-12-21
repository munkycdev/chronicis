// Services/IHashtagApiService.cs
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

/// <summary>
/// Service for calling hashtag-related API endpoints
/// </summary>
public interface IHashtagApiService
{
    /// <summary>
    /// Get all hashtags with usage counts
    /// </summary>
    Task<List<HashtagDto>> GetAllHashtagsAsync();

    /// <summary>
    /// Get a specific hashtag by name
    /// </summary>
    Task<HashtagDto?> GetHashtagByNameAsync(string name);

    /// <summary>
    /// Link a hashtag to an article
    /// </summary>
    Task<bool> LinkHashtagAsync(string hashtagName, Guid articleId);

    Task<HashtagPreviewDto?> GetHashtagPreviewAsync(string name);
}
