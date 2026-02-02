namespace Chronicis.ResourceCompiler.Manifest.Models;

public sealed class ManifestChild
{
    public string Name { get; init; } = string.Empty;
    public string Entity { get; init; } = string.Empty;
    public string ForeignKey { get; init; } = string.Empty;
    public ManifestOrderBy? OrderBy { get; init; }
    public int? MaxDepth { get; init; }
}
