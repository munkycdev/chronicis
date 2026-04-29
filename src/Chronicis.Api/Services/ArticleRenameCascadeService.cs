using Chronicis.Api.Data;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Services;

/// <summary>
/// Propagates article title renames by rewriting wiki-link span inner text across all
/// back-linked articles and by recording the previous title as an <see cref="ArticleAlias"/>.
///
/// <para>
/// Transaction note: the title save in <c>ArticlesController.UpdateArticle</c> commits
/// before this service runs. If this cascade throws, the rename is already persisted and
/// back-link text will be stale until a manual retry. This is accepted POC behaviour.
/// </para>
/// </summary>
public sealed class ArticleRenameCascadeService : IArticleRenameCascadeService
{
    private readonly ChronicisDbContext _db;
    private readonly IWikiLinkTitleRewriter _rewriter;
    private readonly ILogger<ArticleRenameCascadeService> _logger;

    public ArticleRenameCascadeService(
        ChronicisDbContext db,
        IWikiLinkTitleRewriter rewriter,
        ILogger<ArticleRenameCascadeService> logger)
    {
        _db = db;
        _rewriter = rewriter;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task CascadeTitleChangeAsync(
        Guid renamedArticleId,
        string oldTitle,
        string newTitle,
        CancellationToken cancellationToken = default)
    {
        // Case-insensitive no-op guard
        if (string.Equals(oldTitle, newTitle, StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("New title cannot be whitespace.", nameof(newTitle));

        // Collect source article IDs where DisplayText is null (user accepted default title)
        var sourceIds = await _db.ArticleLinks
            .Where(al => al.TargetArticleId == renamedArticleId && al.DisplayText == null)
            .Select(al => al.SourceArticleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var sourceArticles = await _db.Articles
            .Where(a => sourceIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var rewriteCount = 0;
        foreach (var article in sourceArticles)
        {
            var (newBody, changed) = _rewriter.Rewrite(article.Body, renamedArticleId, newTitle);
            if (changed)
            {
                article.Body = newBody;
                // Intentionally NOT updating ModifiedAt / LastModifiedBy per spec.
                rewriteCount++;
            }
        }

        // Append alias for old title on the renamed article (case-insensitive dedup)
        var aliasAdded = false;
        if (!string.IsNullOrWhiteSpace(oldTitle))
        {
            var normalised = oldTitle.ToUpperInvariant();
            var exists = await _db.ArticleAliases
                .AnyAsync(a => a.ArticleId == renamedArticleId &&
                               a.AliasText.ToUpper() == normalised,
                          cancellationToken);

            if (!exists)
            {
                _db.ArticleAliases.Add(new ArticleAlias
                {
                    Id = Guid.NewGuid(),
                    ArticleId = renamedArticleId,
                    AliasText = oldTitle,
                    CreatedAt = DateTime.UtcNow,
                });
                aliasAdded = true;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogTraceSanitized(
            "Cascade rename {ArticleId} '{Old}' -> '{New}' touched {Count} source article(s), alias added: {AliasAdded}",
            renamedArticleId, oldTitle, newTitle, rewriteCount, aliasAdded);
    }
}
