namespace Chronicis.Api.Services;

/// <summary>
/// Write-orchestration service that propagates an article title rename across all
/// wiki-link spans in backlinked articles, and records the previous title as an alias.
///
/// <para>
/// Transaction note: the title save in <c>ArticlesController.UpdateArticle</c> has already
/// committed before this service is invoked. If cascade throws, the rename itself is
/// already persisted; the backlink text will be stale until manually corrected.
/// This is acceptable POC behaviour at 1 000-article scale.
/// </para>
/// </summary>
public interface IArticleRenameCascadeService
{
    /// <summary>
    /// Propagates a title rename. For every article whose <c>ArticleLink</c> row points at
    /// <paramref name="renamedArticleId"/> with a <c>null</c> <c>DisplayText</c> (i.e. the
    /// user accepted the target's title as display text), rewrites matching wiki-link spans
    /// in the source article body to use <paramref name="newTitle"/>.
    ///
    /// Also appends <paramref name="oldTitle"/> as an <c>ArticleAlias</c> on the renamed
    /// article (when non-whitespace and not already present, case-insensitive).
    ///
    /// Safe to call when <paramref name="oldTitle"/> == <paramref name="newTitle"/>
    /// (case-insensitive no-op).
    /// </summary>
    Task CascadeTitleChangeAsync(
        Guid renamedArticleId,
        string oldTitle,
        string newTitle,
        CancellationToken cancellationToken = default);
}
