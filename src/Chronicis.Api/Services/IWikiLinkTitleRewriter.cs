namespace Chronicis.Api.Services;

/// <summary>
/// Rewrites the visible display text of wiki-link spans when the target article is renamed.
/// Only rewrites spans that accepted the target's title as the default display text
/// (i.e., no user-overridden <c>data-display</c> attribute is present).
/// </summary>
public interface IWikiLinkTitleRewriter
{
    /// <summary>
    /// Rewrites inner text of wiki-link spans targeting the given article GUID to
    /// <paramref name="newTitle"/>. Returns <c>(newBody, true)</c> when at least one
    /// eligible span was rewritten; returns <c>(body, false)</c> when no rewrite applies.
    /// The body is returned verbatim when no eligible spans are found.
    /// </summary>
    (string Body, bool Changed) Rewrite(string? body, Guid targetArticleId, string newTitle);
}
