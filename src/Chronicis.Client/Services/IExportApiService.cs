namespace Chronicis.Client.Services;

/// <summary>
/// Service interface for export operations
/// </summary>
public interface IExportApiService
{
    /// <summary>
    /// Export a world to a markdown zip archive and trigger browser download
    /// </summary>
    /// <param name="worldId">The world to export</param>
    /// <param name="worldName">The world name (for filename)</param>
    /// <returns>True if download was triggered successfully</returns>
    Task<bool> ExportWorldToMarkdownAsync(Guid worldId, string worldName);
}
