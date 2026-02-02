using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Raw.Models;

namespace Chronicis.ResourceCompiler.Indexing;

public sealed class IndexBuilder
{
    public Task<PkIndex> BuildPkIndexAsync(ManifestEntity entity, RawEntitySet rawEntitySet, CancellationToken cancellationToken)
    {
        _ = entity;
        _ = rawEntitySet;
        _ = cancellationToken;
        throw new NotImplementedException();
    }

    public Task<FkIndex> BuildFkIndexAsync(ManifestChild relationship, RawEntitySet rawEntitySet, CancellationToken cancellationToken)
    {
        _ = relationship;
        _ = rawEntitySet;
        _ = cancellationToken;
        throw new NotImplementedException();
    }
}
