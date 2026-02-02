using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Manifest;

public sealed class ManifestValidator
{
    public IReadOnlyList<Warning> Validate(Models.Manifest manifest)
    {
        _ = manifest;
        throw new NotImplementedException();
    }
}
