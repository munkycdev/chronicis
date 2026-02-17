namespace Chronicis.ResourceCompiler.Manifest.Models;

public sealed class ManifestOutput
{
    public string BlobTemplate { get; init; } = string.Empty;
    public ManifestOutputIndex? Index { get; init; }
}
