using Chronicis.ResourceCompiler.Compilation.Models;
using Chronicis.ResourceCompiler.Indexing.Models;

namespace Chronicis.ResourceCompiler.Output;

public sealed class OutputWriter
{
    public Task WriteAsync(
        string outputRoot,
        IReadOnlyList<CompiledDocument> documents,
        IReadOnlyDictionary<string, PkIndex> pkIndexes,
        IReadOnlyDictionary<string, FkIndex> fkIndexes,
        CancellationToken cancellationToken)
    {
        _ = outputRoot;
        _ = documents;
        _ = pkIndexes;
        _ = fkIndexes;
        _ = cancellationToken;
        throw new NotImplementedException();
    }
}
