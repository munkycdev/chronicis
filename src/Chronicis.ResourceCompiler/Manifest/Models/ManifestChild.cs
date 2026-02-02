namespace Chronicis.ResourceCompiler.Manifest.Models;

public sealed class ManifestChild
{
    public string Entity { get; init; } = string.Empty;
    public string ForeignKeyField { get; init; } = string.Empty;
    public ManifestOrderBy? OrderBy { get; init; }
    public int? MaxDepth { get; init; }
}
