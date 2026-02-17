using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Compiler;

public sealed class CompilerRunResult
{
    public CompilerRunResult(IReadOnlyList<Warning> warnings)
    {
        Warnings = warnings;
    }

    public IReadOnlyList<Warning> Warnings { get; }

    public bool HasErrors => Warnings.Any(warning => warning.Severity == WarningSeverity.Error);
}
