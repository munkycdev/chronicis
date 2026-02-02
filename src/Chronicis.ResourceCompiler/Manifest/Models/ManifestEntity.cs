namespace Chronicis.ResourceCompiler.Manifest.Models;

public sealed class ManifestEntity
{
    public string Name { get; init; } = string.Empty;
    public string File { get; init; } = string.Empty;
    public string PrimaryKey { get; init; } = string.Empty;
    public bool IsRoot { get; init; }
    public IReadOnlyList<ManifestChild> Children { get; init; } = Array.Empty<ManifestChild>();
    public ManifestOrderBy? OrderBy { get; init; }
}
