using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chronicis.Api.Tests.Services;

/// <summary>
/// Asserts that the Article index configuration matches the slug-uniqueness design
/// established in Phase 09 (UrlRestructure_SessionNoteSlugScope).
/// </summary>
[ExcludeFromCodeCoverage]
public class SlugSchemaGuardrailTests
{
    private static Microsoft.EntityFrameworkCore.Metadata.IEntityType ArticleEntityType()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var ctx = new ChronicisDbContext(options);
        return ctx.Model.FindEntityType(typeof(Chronicis.Shared.Models.Article))!;
    }

    [Fact]
    public void Articles_OldRootIndex_DoesNotExist()
    {
        var entity = ArticleEntityType();
        var ix = entity.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_Articles_WorldId_Slug_Root");

        Assert.Null(ix);
    }

    [Fact]
    public void Articles_RootNonSessionNoteIndex_ExistsWithCorrectFilter()
    {
        var entity = ArticleEntityType();
        var ix = entity.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_Articles_WorldId_Slug_RootNonSessionNote");

        Assert.NotNull(ix);
        Assert.True(ix!.IsUnique);
        Assert.Equal("[ParentId] IS NULL AND [Type] <> 11", ix.GetFilter());
        Assert.Equal(2, ix.Properties.Count);
        Assert.Contains(ix.Properties, p => p.Name == "WorldId");
        Assert.Contains(ix.Properties, p => p.Name == "Slug");
    }

    [Fact]
    public void Articles_SessionNoteSlugIndex_ExistsWithCorrectFilter()
    {
        var entity = ArticleEntityType();
        var ix = entity.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_Articles_SessionId_Slug_SessionNote");

        Assert.NotNull(ix);
        Assert.True(ix!.IsUnique);
        Assert.Equal("[Type] = 11 AND [SessionId] IS NOT NULL", ix.GetFilter());
        Assert.Equal(2, ix.Properties.Count);
        Assert.Contains(ix.Properties, p => p.Name == "SessionId");
        Assert.Contains(ix.Properties, p => p.Name == "Slug");
    }

    [Fact]
    public void Articles_ParentSlugIndex_StillExists()
    {
        var entity = ArticleEntityType();
        var ix = entity.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_Articles_ParentId_Slug");

        Assert.NotNull(ix);
        Assert.True(ix!.IsUnique);
    }
}
