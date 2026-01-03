using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Functions;

/// <summary>
/// Azure Functions for exporting world data
/// </summary>
public class ExportFunctions
{
    private readonly IExportService _exportService;
    private readonly IWorldService _worldService;
    private readonly ILogger<ExportFunctions> _logger;

    public ExportFunctions(
        IExportService exportService,
        IWorldService worldService,
        ILogger<ExportFunctions> logger)
    {
        _exportService = exportService;
        _worldService = worldService;
        _logger = logger;
    }

    /// <summary>
    /// Export a world to a downloadable zip archive containing markdown files
    /// </summary>
    [Function("ExportWorldToMarkdown")]
    public async Task<HttpResponseData> ExportWorldToMarkdown(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "worlds/{worldId:guid}/export")] HttpRequestData req,
        Guid worldId,
        FunctionContext context)
    {
        var user = context.GetRequiredUser();
        _logger.LogInformation("User {UserId} requested export of world {WorldId}", user.Id, worldId);

        // Get world info for filename
        var world = await _worldService.GetWorldAsync(worldId, user.Id);
        if (world == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "World not found or access denied" });
            return notFound;
        }

        // Generate the export
        var zipBytes = await _exportService.ExportWorldToMarkdownAsync(worldId, user.Id);

        if (zipBytes == null)
        {
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new { error = "Access denied or export failed" });
            return forbidden;
        }

        // Build safe filename
        var safeWorldName = string.Join("_", world.Name.Split(Path.GetInvalidFileNameChars()));
        if (safeWorldName.Length > 50) safeWorldName = safeWorldName.Substring(0, 50);
        var fileName = $"{safeWorldName}_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

        // Return the zip file
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/zip");
        response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        await response.Body.WriteAsync(zipBytes);

        _logger.LogInformation("Export completed for world {WorldId}. File: {FileName}, Size: {Size} bytes", 
            worldId, fileName, zipBytes.Length);

        return response;
    }
}
