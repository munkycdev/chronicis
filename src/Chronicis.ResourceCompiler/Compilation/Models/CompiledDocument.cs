using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Indexing.Models;

namespace Chronicis.ResourceCompiler.Compilation.Models;

[ExcludeFromCodeCoverage]
public sealed class CompiledDocument
{
    public CompiledDocument(string entityName, KeyValue primaryKey, JsonNode payload)
    {
        EntityName = entityName;
        PrimaryKey = primaryKey;
        Payload = payload;
    }

    public string EntityName { get; }
    public KeyValue PrimaryKey { get; }
    public JsonNode Payload { get; }
}
