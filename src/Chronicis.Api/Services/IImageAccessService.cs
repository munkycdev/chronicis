using Chronicis.Api.Models;

namespace Chronicis.Api.Services;

public interface IImageAccessService
{
    Task<ServiceResult<string>> GetImageDownloadUrlAsync(Guid documentId, Guid userId);
}

