using Chronicis.Api.Data;
using Chronicis.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Service for validating article operations.
/// </summary>
public interface IArticleValidationService
{
    Task<ValidationResult> ValidateCreateAsync(ArticleCreateDto dto);
    Task<ValidationResult> ValidateUpdateAsync(int articleId, ArticleUpdateDto dto);
    Task<ValidationResult> ValidateDeleteAsync(int articleId);
}

public class ArticleValidationService : IArticleValidationService
{
    private readonly ChronicisDbContext _context;

    public ArticleValidationService(ChronicisDbContext context)
    {
        _context = context;
    }

    public async Task<ValidationResult> ValidateCreateAsync(ArticleCreateDto dto)
    {
        var result = new ValidationResult();

        // Parent must exist if specified
        if (dto.ParentId.HasValue)
        {
            var parentExists = await _context.Articles
                .AnyAsync(a => a.Id == dto.ParentId.Value);
            
            if (!parentExists)
            {
                result.AddError("ParentId", "Parent article does not exist");
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateUpdateAsync(int articleId, ArticleUpdateDto dto)
    {
        var result = new ValidationResult();

        // Article must exist
        var articleExists = await _context.Articles
            .AnyAsync(a => a.Id == articleId);
        
        if (!articleExists)
        {
            result.AddError("Id", "Article not found");
        }

        // Title is required
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            result.AddError("Title", "Title is required");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateDeleteAsync(int articleId)
    {
        var result = new ValidationResult();

        // Article must exist
        var article = await _context.Articles
            .Include(a => a.Children)
            .FirstOrDefaultAsync(a => a.Id == articleId);
        
        if (article == null)
        {
            result.AddError("Id", "Article not found");
            return result;
        }

        return result;
    }
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool IsValid => _errors.Count == 0;

    public IReadOnlyDictionary<string, List<string>> Errors => _errors;

    public void AddError(string field, string message)
    {
        if (!_errors.ContainsKey(field))
        {
            _errors[field] = new List<string>();
        }
        _errors[field].Add(message);
    }

    public string GetFirstError()
    {
        return _errors.Values.FirstOrDefault()?.FirstOrDefault() ?? "Validation failed";
    }
}
