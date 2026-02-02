namespace Chronicis.ResourceCompiler.Manifest.Models;

public sealed class ManifestOrderBy
{
    public string Field { get; init; } = string.Empty;
    public ManifestOrderByDirection Direction { get; init; } = ManifestOrderByDirection.Asc;
}
