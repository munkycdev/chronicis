using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for managing world documents with blob storage integration.
/// </summary>
public class WorldDocumentService : IWorldDocumentService
{
    private readonly ChronicisDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WorldDocumentService> _logger;

    // File validation constants
    private const long MaxFileSizeBytes = 209_715_200; // 200 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".xlsx", ".pptx", ".txt", ".md",
        ".png", ".jpg", ".jpeg", ".gif", ".webp"
    };

    private static readonly Dictionary<string, string> ExtensionToMimeType = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".pdf", "application/pdf" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".txt", "text/plain" },
        { ".md", "text/markdown" },
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif", "image/gif" },
        { ".webp", "image/webp" }
    };

    public WorldDocumentService(
        ChronicisDbContext db,
        IBlobStorageService blobStorage,
        IConfiguration configuration,
        ILogger<WorldDocumentService> logger)
    {
        _db = db;
        _blobStorage = blobStorage;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorldDocumentUploadResponseDto> RequestUploadAsync(
        Guid worldId,
        Guid userId,
        WorldDocumentUploadRequestDto request)
    {
        _logger.LogDebug("User {UserId} requesting upload for world {WorldId}: {FileName}",
            userId, worldId, request.FileName);

        // Verify user owns the world
        var world = await _db.Worlds
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == worldId && w.OwnerId == userId);

        if (world == null)
        {
            throw new UnauthorizedAccessException("World not found or access denied");
        }

        // Validate file
        ValidateFileUpload(request);

        // Generate unique title (handle duplicates)
        var title = await GenerateUniqueTitleAsync(worldId, request.FileName);

        // Create pending document record
        var document = new WorldDocument
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            FileName = request.FileName,
            Title = title,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            Description = request.Description,
            UploadedById = userId,
            UploadedAt = DateTime.UtcNow,
            BlobPath = "" // Will be set after blob path is generated
        };

        // Generate blob path and SAS URL
        var blobPath = _blobStorage.BuildBlobPath(worldId, document.Id, request.FileName);
        document.BlobPath = blobPath;

        var sasUrl = await _blobStorage.GenerateUploadSasUrlAsync(
            worldId,
            document.Id,
            request.FileName,
            request.ContentType);

        // Save pending document (blob doesn't exist yet)
        _db.WorldDocuments.Add(document);
        await _db.SaveChangesAsync();

        _logger.LogDebug("Created pending document {DocumentId} for world {WorldId}",
            document.Id, worldId);

        return new WorldDocumentUploadResponseDto
        {
            DocumentId = document.Id,
            UploadUrl = sasUrl,
            Title = title
        };
    }

    public async Task<WorldDocumentDto> ConfirmUploadAsync(
        Guid worldId,
        Guid documentId,
        Guid userId)
    {
        _logger.LogDebug("User {UserId} confirming upload for document {DocumentId}",
            userId, documentId);

        // Get the pending document
        var document = await _db.WorldDocuments
            .Include(d => d.World)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.WorldId == worldId);

        if (document == null)
        {
            throw new InvalidOperationException("Document not found");
        }

        // Verify user owns the world
        if (document.World.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only world owner can upload documents");
        }

        // Verify blob exists in storage
        var metadata = await _blobStorage.GetBlobMetadataAsync(document.BlobPath);

        if (metadata == null)
        {
            // Blob upload failed or didn't complete
            _logger.LogWarning("Blob not found for document {DocumentId}: {BlobPath}",
                documentId, document.BlobPath);
            throw new InvalidOperationException("File upload did not complete. Please try again.");
        }

        // Update document with actual blob metadata
        document.FileSizeBytes = metadata.SizeBytes;
        document.ContentType = metadata.ContentType;

        await _db.SaveChangesAsync();

        _logger.LogDebug("Confirmed upload for document {DocumentId}, size: {SizeBytes} bytes",
            documentId, metadata.SizeBytes);

        return MapToDto(document);
    }

    public async Task<List<WorldDocumentDto>> GetWorldDocumentsAsync(Guid worldId, Guid userId)
    {
        _logger.LogDebug("User {UserId} getting documents for world {WorldId}",
            userId, worldId);

        // Verify user has access to the world (owner or member)
        var hasAccess = await _db.Worlds
            .AsNoTracking()
            .AnyAsync(w => w.Id == worldId && 
                (w.OwnerId == userId || w.Members.Any(m => m.UserId == userId)));

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("World not found or access denied");
        }

        var documents = await _db.WorldDocuments
            .AsNoTracking()
            .Where(d => d.WorldId == worldId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return documents.Select(MapToDto).ToList();
    }

    public async Task<DocumentContentResult> GetDocumentContentAsync(Guid documentId, Guid userId)
    {
        _logger.LogDebug("User {UserId} requesting document download URL for {DocumentId}",
            userId, documentId);

        var document = await GetAuthorizedDocumentAsync(documentId, userId);
        var contentType = string.IsNullOrWhiteSpace(document.ContentType)
            ? "application/octet-stream"
            : document.ContentType;

        // Generate read-only SAS URL for direct download from blob storage
        var downloadUrl = await _blobStorage.GenerateDownloadSasUrlAsync(document.BlobPath);

        return new DocumentContentResult(
            downloadUrl,
            document.FileName,
            contentType,
            document.FileSizeBytes);
    }

    public async Task<WorldDocumentDto> UpdateDocumentAsync(
        Guid worldId,
        Guid documentId,
        Guid userId,
        WorldDocumentUpdateDto update)
    {
        _logger.LogDebug("User {UserId} updating document {DocumentId}",
            userId, documentId);

        var document = await _db.WorldDocuments
            .Include(d => d.World)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.WorldId == worldId);

        if (document == null)
        {
            throw new InvalidOperationException("Document not found");
        }

        // Only owner can update
        if (document.World.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only world owner can update documents");
        }

        // Update metadata
        if (!string.IsNullOrWhiteSpace(update.Title))
        {
            document.Title = update.Title.Trim();
        }

        document.Description = string.IsNullOrWhiteSpace(update.Description)
            ? null
            : update.Description.Trim();

        await _db.SaveChangesAsync();

        _logger.LogDebug("Updated document {DocumentId}", documentId);

        return MapToDto(document);
    }

    public async Task DeleteDocumentAsync(Guid worldId, Guid documentId, Guid userId)
    {
        _logger.LogDebug("User {UserId} deleting document {DocumentId}",
            userId, documentId);

        var document = await _db.WorldDocuments
            .Include(d => d.World)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.WorldId == worldId);

        if (document == null)
        {
            throw new InvalidOperationException("Document not found");
        }

        // Only owner can delete
        if (document.World.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only world owner can delete documents");
        }

        // Delete blob from storage
        try
        {
            await _blobStorage.DeleteBlobAsync(document.BlobPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob for document {DocumentId}: {BlobPath}",
                documentId, document.BlobPath);
            // Continue with database deletion even if blob deletion fails
        }

        // Delete from database
        _db.WorldDocuments.Remove(document);
        await _db.SaveChangesAsync();

        _logger.LogDebug("Deleted document {DocumentId}", documentId);
    }

    // ===== Private Helper Methods =====

    private void ValidateFileUpload(WorldDocumentUploadRequestDto request)
    {
        // Validate file size
        if (request.FileSizeBytes <= 0)
        {
            throw new ArgumentException("File size must be greater than zero");
        }

        if (request.FileSizeBytes > MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB");
        }

        // Validate filename
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("Filename is required");
        }

        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            var allowed = string.Join(", ", AllowedExtensions);
            throw new ArgumentException($"File type '{extension}' is not allowed. Allowed types: {allowed}");
        }

        // Validate content type matches extension
        if (!string.IsNullOrWhiteSpace(request.ContentType))
        {
            if (ExtensionToMimeType.TryGetValue(extension, out var expectedMimeType))
            {
                if (!request.ContentType.Equals(expectedMimeType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Content type mismatch for {FileName}: expected {Expected}, got {Actual}",
                        request.FileName, expectedMimeType, request.ContentType);
                }
            }
        }
    }

    private async Task<string> GenerateUniqueTitleAsync(Guid worldId, string fileName)
    {
        // Start with filename without extension as title
        var baseTitle = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var title = baseTitle;

        // Check for existing documents with same title
        var existingTitles = await _db.WorldDocuments
            .AsNoTracking()
            .Where(d => d.WorldId == worldId && d.Title.StartsWith(baseTitle))
            .Select(d => d.Title)
            .ToListAsync();

        if (!existingTitles.Contains(title))
        {
            return title; // Title is unique, use as-is
        }

        // Title exists, find next available number
        var counter = 2;
        while (existingTitles.Contains(title))
        {
            title = $"{baseTitle} ({counter})";
            counter++;

            // Safety check to prevent infinite loop
            if (counter > 1000)
            {
                throw new InvalidOperationException("Too many documents with similar names");
            }
        }

        _logger.LogDebug("Generated unique title for {FileName}: {Title}", fileName, title);
        return title;
    }

    private static WorldDocumentDto MapToDto(WorldDocument document)
    {
        return new WorldDocumentDto
        {
            Id = document.Id,
            WorldId = document.WorldId,
            FileName = document.FileName,
            Title = document.Title,
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            Description = document.Description,
            UploadedAt = document.UploadedAt,
            UploadedById = document.UploadedById
        };
    }

    private async Task<WorldDocument> GetAuthorizedDocumentAsync(
        Guid documentId,
        Guid userId,
        Guid? worldId = null)
    {
        var query = _db.WorldDocuments
            .Include(d => d.World)
            .AsQueryable();

        if (worldId.HasValue)
        {
            query = query.Where(d => d.WorldId == worldId.Value);
        }

        var document = await query.FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            throw new InvalidOperationException("Document not found");
        }

        var hasAccess = document.World.OwnerId == userId ||
            await _db.WorldMembers.AnyAsync(m => m.WorldId == document.WorldId && m.UserId == userId);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        return document;
    }
}
