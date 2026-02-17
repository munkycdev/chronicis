using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Manifest.Models;

public sealed class ManifestLoadResult
{
    public ManifestLoadResult(Manifest? manifest, IReadOnlyList<Warning> warnings)
    {
        Manifest = manifest;
        Warnings = warnings;
    }

    public Manifest? Manifest { get; }
    public IReadOnlyList<Warning> Warnings { get; }

    public bool HasErrors => Warnings.Any(warning => warning.Severity == WarningSeverity.Error);
}
