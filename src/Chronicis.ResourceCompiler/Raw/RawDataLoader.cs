using Chronicis.ResourceCompiler.Raw.Models;

namespace Chronicis.ResourceCompiler.Raw;

public sealed class RawDataLoader
{
    public Task<IReadOnlyList<RawEntitySet>> LoadAsync(Manifest.Models.Manifest manifest, string inputRoot, CancellationToken cancellationToken)
    {
        _ = manifest;
        _ = inputRoot;
        _ = cancellationToken;
        throw new NotImplementedException();
    }
}
