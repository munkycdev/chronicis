using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Chronicis.ResourceCompiler.Raw.Models;

[ExcludeFromCodeCoverage]
public sealed class RawEntityRow
{
    public RawEntityRow(string entityName, int rowIndex, JsonElement data, JsonElement? extractedPkElement)
    {
        EntityName = entityName;
        RowIndex = rowIndex;
        Data = data;
        ExtractedPkElement = extractedPkElement;
    }

    public string EntityName { get; }
    public int RowIndex { get; }
    public JsonElement Data { get; }
    public JsonElement? ExtractedPkElement { get; }
}
