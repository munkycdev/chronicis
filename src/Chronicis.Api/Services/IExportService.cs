namespace Chronicis.Api.Services;

/// <summary>
/// Service interface for exporting world data to downloadable archives
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export all articles in a world to a zip archive containing markdown files
    /// organized in a folder structure matching the tree hierarchy
    /// </summary>
    /// <param name="worldId">The world to export</param>
    /// <param name="userId">The requesting user (for permission checks)</param>
    /// <returns>Zip archive as byte array, or null if access denied</returns>
    Task<byte[]?> ExportWorldToMarkdownAsync(Guid worldId, Guid userId);
}
