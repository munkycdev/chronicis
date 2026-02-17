using System.Diagnostics.CodeAnalysis;

namespace Chronicis.ResourceCompiler.Manifest.Models;

[ExcludeFromCodeCoverage]
public sealed class ManifestOrderBy
{
    public string Field { get; init; } = string.Empty;
    public ManifestOrderByDirection? Direction { get; init; }
}
