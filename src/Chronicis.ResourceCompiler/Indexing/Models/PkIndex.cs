using Chronicis.ResourceCompiler.Raw.Models;

namespace Chronicis.ResourceCompiler.Indexing.Models;

public sealed class PkIndex
{
    public PkIndex(string entityName, IReadOnlyDictionary<KeyValue, RawEntityRow> rowsByKey)
    {
        EntityName = entityName;
        RowsByKey = rowsByKey;
    }

    public string EntityName { get; }
    public IReadOnlyDictionary<KeyValue, RawEntityRow> RowsByKey { get; }
}
