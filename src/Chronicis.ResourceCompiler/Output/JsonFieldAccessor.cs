using System.Text.Json.Nodes;

namespace Chronicis.ResourceCompiler.Output;

public sealed class JsonFieldAccessor
{
    public bool TryGetField(JsonObject payload, string field, out JsonNode? value)
    {
        value = null;

        if (payload.TryGetPropertyValue(field, out var node))
        {
            value = node;
            return true;
        }

        if (payload["fields"] is JsonObject fields && fields.TryGetPropertyValue(field, out node))
        {
            value = node;
            return true;
        }

        return false;
    }
}
