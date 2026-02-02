using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Indexing;

public sealed class IndexBuildResult
{
    public IndexBuildResult(
        IReadOnlyDictionary<string, PkIndex> pkIndexes,
        IReadOnlyList<FkIndex> fkIndexes,
        IReadOnlyList<Warning> warnings)
    {
        PkIndexes = pkIndexes;
        FkIndexes = fkIndexes;
        Warnings = warnings;
    }

    public IReadOnlyDictionary<string, PkIndex> PkIndexes { get; }
    public IReadOnlyList<FkIndex> FkIndexes { get; }
    public IReadOnlyList<Warning> Warnings { get; }

    public bool HasErrors => Warnings.Any(warning => warning.Severity == WarningSeverity.Error);
}
