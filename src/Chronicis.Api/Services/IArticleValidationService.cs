using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IArticleValidationService
{
    Task<ValidationResult> ValidateCreateAsync(ArticleCreateDto dto);
    Task<ValidationResult> ValidateDeleteAsync(Guid articleId);
    Task<ValidationResult> ValidateUpdateAsync(Guid articleId, ArticleUpdateDto dto);
}
