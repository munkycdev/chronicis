using System.Diagnostics.CodeAnalysis;

namespace Chronicis.ResourceCompiler.Manifest.Models;

[ExcludeFromCodeCoverage]
public sealed class ManifestOutputIndex
{
    public string Blob { get; init; } = string.Empty;
    public IReadOnlyList<string> Fields { get; init; } = Array.Empty<string>();
}
