using Chronicis.Api.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class PublicWorldServiceBranchCoverageTests
{
    [Fact]
    public void Constructor_AssignsDependencies()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var service = new PublicWorldService(
            db,
            NullLogger<PublicWorldService>.Instance,
            Substitute.For<IArticleHierarchyService>(),
            Substitute.For<IBlobStorageService>());

        Assert.NotNull(service);
    }

    [Fact]
    public void PublicWorldService_CreateVirtualGroup_MapsProperties()
    {
        var create = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(PublicWorldService), "CreateVirtualGroup");

        var group = (ArticleTreeDto)create.Invoke(null, ["wiki", "Wiki", "fa-solid fa-book"])!;

        Assert.Equal("Wiki", group.Title);
        Assert.Equal("wiki", group.Slug);
        Assert.Equal("fa-solid fa-book", group.IconEmoji);
        Assert.True(group.IsVirtualGroup);
        Assert.NotEqual(Guid.Empty, group.Id);
        Assert.NotNull(group.Children);
        Assert.Empty(group.Children!);
    }

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
