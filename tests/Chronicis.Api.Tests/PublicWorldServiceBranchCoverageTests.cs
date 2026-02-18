using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Api.Tests;

public class PublicWorldServiceBranchCoverageTests
{
    [Fact]
    public void PublicWorldService_CollectArticleIds_CoversBranches()
    {
        var collect = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(PublicWorldService), "CollectArticleIds");
        var root = new ArticleTreeDto { Id = Guid.NewGuid(), Children = new List<ArticleTreeDto>() };
        var child = new ArticleTreeDto { Id = Guid.NewGuid(), Children = null };
        root.Children!.Add(child);

        var ids = new HashSet<Guid>();
        collect.Invoke(null, [root, ids]);

        Assert.Contains(root.Id, ids);
        Assert.Contains(child.Id, ids);
    }
}
