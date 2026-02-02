using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Raw.Models;

namespace Chronicis.ResourceCompiler.Compilation;

public sealed class OrderingService
{
    public IReadOnlyList<RawEntityRow> ApplyOrder(IReadOnlyList<RawEntityRow> rows, ManifestOrderBy? orderBy)
    {
        _ = rows;
        _ = orderBy;
        throw new NotImplementedException();
    }
}
