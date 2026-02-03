using System.Linq;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Output;

public sealed class OutputWriteResult
{
    public OutputWriteResult(IReadOnlyList<Warning> warnings)
    {
        Warnings = warnings;
    }

    public IReadOnlyList<Warning> Warnings { get; }
    public bool HasErrors => Warnings.Any(warning => warning.Severity == WarningSeverity.Error);
}
