using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IAutoHashtagService
{
    /// <summary>
    /// Process articles to find and optionally insert hashtags based on article title references
    /// </summary>
    /// <param name="userId">User ID to process articles for</param>
    /// <param name="dryRun">If true, preview changes without applying them</param>
    /// <param name="articleIds">Optional list of specific article IDs to process. If null, processes all.</param>
    /// <returns>Results showing changes found/applied</returns>
    Task<AutoHashtagResponse> ProcessArticlesAsync(Guid userId, bool dryRun, List<Guid>? articleIds = null);
}
