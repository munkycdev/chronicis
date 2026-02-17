using System.Text.Json;

namespace Chronicis.ResourceCompiler.Raw.Models;

public sealed class RawEntitySet : IDisposable
{
    public RawEntitySet(string entityName, JsonDocument document, IReadOnlyList<RawEntityRow> rows)
    {
        EntityName = entityName;
        Document = document;
        Rows = rows;
    }

    public string EntityName { get; }
    public JsonDocument Document { get; }
    public IReadOnlyList<RawEntityRow> Rows { get; }

    public void Dispose()
    {
        Document.Dispose();
    }
}
