using Chronicis.Shared.Enums;

namespace Chronicis.Api.Services;

internal static class ArticleSlugPathResolver
{
    internal static async Task<(Guid Id, ArticleType Type)?> ResolveAsync(
        IReadOnlyList<string> slugs,
        Func<string, Guid?, bool, Task<(Guid Id, ArticleType Type)?>> resolveSegmentAsync,
        Func<string, Guid?, ArticleType?, Task<(Guid Id, ArticleType Type)?>>? resolveCompatibilityAsync = null)
    {
        Guid? currentParentId = null;
        ArticleType? currentArticleType = null;
        (Guid Id, ArticleType Type)? resolvedArticle = null;

        for (var i = 0; i < slugs.Count; i++)
        {
            var slug = slugs[i];
            var isRootLevel = (i == 0);

            resolvedArticle = await resolveSegmentAsync(slug, currentParentId, isRootLevel);

            if (resolvedArticle == null && resolveCompatibilityAsync != null)
            {
                resolvedArticle = await resolveCompatibilityAsync(slug, currentParentId, currentArticleType);
            }

            if (resolvedArticle == null)
            {
                return null;
            }

            currentParentId = resolvedArticle.Value.Id;
            currentArticleType = resolvedArticle.Value.Type;
        }

        return resolvedArticle;
    }
}
