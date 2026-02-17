using System.Diagnostics.CodeAnalysis;
using Chronicis.ResourceCompiler.Raw.Models;

namespace Chronicis.ResourceCompiler.Indexing.Models;

[ExcludeFromCodeCoverage]
public sealed class FkIndex
{
    public FkIndex(
        string parentEntityName,
        string entityName,
        string fieldName,
        IReadOnlyDictionary<KeyValue, IReadOnlyList<RawEntityRow>> rowsByKey)
    {
        ParentEntityName = parentEntityName;
        EntityName = entityName;
        FieldName = fieldName;
        RowsByKey = rowsByKey;
    }

    public string ParentEntityName { get; }
    public string EntityName { get; }
    public string FieldName { get; }
    public IReadOnlyDictionary<KeyValue, IReadOnlyList<RawEntityRow>> RowsByKey { get; }
}
