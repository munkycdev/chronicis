using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

public class ImageAccessService : IImageAccessService
{
    private readonly ChronicisDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<ImageAccessService> _logger;

    public ImageAccessService(
        ChronicisDbContext context,
        IBlobStorageService blobStorage,
        ILogger<ImageAccessService> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<ServiceResult<string>> GetImageDownloadUrlAsync(Guid documentId, Guid userId)
    {
        var document = await _context.WorldDocuments
            .AsNoTracking()
            .Include(d => d.World)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            return ServiceResult<string>.NotFound();
        }

        var hasAccess = document.World.OwnerId == userId
            || await _context.WorldMembers.AnyAsync(m => m.WorldId == document.WorldId && m.UserId == userId);

        if (!hasAccess)
        {
            return ServiceResult<string>.Forbidden();
        }

        if (!document.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Non-image document {DocumentId} requested via image proxy", documentId);
            return ServiceResult<string>.ValidationError("Document is not an image");
        }

        var downloadUrl = await _blobStorage.GenerateDownloadSasUrlAsync(document.BlobPath);
        return ServiceResult<string>.Success(downloadUrl);
    }
}

