namespace Chronicis.ResourceCompiler.Manifest;

public sealed class ManifestLoader
{
    public Task<Models.Manifest> LoadAsync(string manifestPath, CancellationToken cancellationToken)
    {
        _ = manifestPath;
        _ = cancellationToken;
        throw new NotImplementedException();
    }
}
