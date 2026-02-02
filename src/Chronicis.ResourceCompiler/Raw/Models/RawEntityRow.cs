using System.Text.Json;

namespace Chronicis.ResourceCompiler.Raw.Models;

public sealed class RawEntityRow
{
    public RawEntityRow(string entityName, JsonElement data, int sourceIndex)
    {
        EntityName = entityName;
        Data = data;
        SourceIndex = sourceIndex;
    }

    public string EntityName { get; }
    public JsonElement Data { get; }
    public int SourceIndex { get; }
}
