using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Raw.Models;

public sealed class RawLoadResult
{
    public RawLoadResult(IReadOnlyList<RawEntitySet> entitySets, IReadOnlyList<Warning> warnings)
    {
        EntitySets = entitySets;
        Warnings = warnings;
    }

    public IReadOnlyList<RawEntitySet> EntitySets { get; }
    public IReadOnlyList<Warning> Warnings { get; }

    public bool HasErrors => Warnings.Any(warning => warning.Severity == WarningSeverity.Error);
}
