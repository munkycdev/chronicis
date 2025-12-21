using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services;

public interface IAutoHashtagApiService
{
    /// <summary>
    /// Preview auto-hashtag changes without applying them
    /// </summary>
    Task<AutoHashtagResponse> PreviewAutoHashtagAsync(List<Guid>? articleIds = null);

    /// <summary>
    /// Apply auto-hashtag changes to articles
    /// </summary>
    Task<AutoHashtagResponse> ApplyAutoHashtagAsync(List<Guid>? articleIds = null);
}
