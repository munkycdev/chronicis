using System.Text.Json;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Serialization;

public sealed class KeyCanonicalizer
{
    public bool TryCanonicalize(JsonElement element, out KeyValue? key, out Warning? warning)
    {
        _ = element;
        key = null;
        warning = null;
        throw new NotImplementedException();
    }
}
