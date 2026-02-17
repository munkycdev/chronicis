using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Compilation.Models;

public sealed class CompilationResult
{
    public CompilationResult(IReadOnlyList<CompiledDocument> documents, IReadOnlyList<Warning> warnings)
    {
        Documents = documents;
        Warnings = warnings;
    }

    public IReadOnlyList<CompiledDocument> Documents { get; }
    public IReadOnlyList<Warning> Warnings { get; }

    public bool HasErrors => Warnings.Any(warning => warning.Severity == WarningSeverity.Error);
}
