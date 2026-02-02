using Chronicis.ResourceCompiler.Compilation.Models;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Raw.Models;

namespace Chronicis.ResourceCompiler.Compilation;

public sealed class DocumentAssembler
{
    public Task<IReadOnlyList<CompiledDocument>> AssembleAsync(
        Manifest.Models.Manifest manifest,
        IReadOnlyDictionary<string, RawEntitySet> rawEntities,
        IReadOnlyDictionary<string, PkIndex> pkIndexes,
        IReadOnlyDictionary<string, FkIndex> fkIndexes,
        CancellationToken cancellationToken)
    {
        _ = manifest;
        _ = rawEntities;
        _ = pkIndexes;
        _ = fkIndexes;
        _ = cancellationToken;
        throw new NotImplementedException();
    }
}
