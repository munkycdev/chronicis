using System.Diagnostics.CodeAnalysis;

namespace Chronicis.ResourceCompiler.Manifest.Models;

[ExcludeFromCodeCoverage]
public sealed class ManifestOutput
{
    public string BlobTemplate { get; init; } = string.Empty;
    public ManifestOutputIndex? Index { get; init; }
}
