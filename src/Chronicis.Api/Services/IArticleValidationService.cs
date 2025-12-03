using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public interface IArticleValidationService
{
    Task<ValidationResult> ValidateCreateAsync(ArticleCreateDto dto);
    Task<ValidationResult> ValidateDeleteAsync(int articleId);
    Task<ValidationResult> ValidateUpdateAsync(int articleId, ArticleUpdateDto dto);
}