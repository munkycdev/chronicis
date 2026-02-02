namespace Chronicis.ResourceCompiler.Manifest.Models;

public sealed class Manifest
{
    public IReadOnlyDictionary<string, ManifestEntity> Entities { get; init; } =
        new Dictionary<string, ManifestEntity>();

    public int? MaxDepth { get; init; }
}
