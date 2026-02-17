using System.Diagnostics.CodeAnalysis;

namespace Chronicis.ResourceCompiler.Manifest.Models;

[ExcludeFromCodeCoverage]
public sealed class ManifestIdentity
{
    public string SlugField { get; init; } = string.Empty;
    public string IdTemplate { get; init; } = string.Empty;
}
