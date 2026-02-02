using Chronicis.ResourceCompiler.Raw.Models;

namespace Chronicis.ResourceCompiler.Indexing.Models;

public sealed class FkIndex
{
    public FkIndex(string entityName, string fieldName, IReadOnlyDictionary<KeyValue, IReadOnlyList<RawEntityRow>> rowsByKey)
    {
        EntityName = entityName;
        FieldName = fieldName;
        RowsByKey = rowsByKey;
    }

    public string EntityName { get; }
    public string FieldName { get; }
    public IReadOnlyDictionary<KeyValue, IReadOnlyList<RawEntityRow>> RowsByKey { get; }
}
