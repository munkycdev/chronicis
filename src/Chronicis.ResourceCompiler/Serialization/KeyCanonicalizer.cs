using System.Text.Json;
using Chronicis.ResourceCompiler.Indexing;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Serialization;

public sealed class KeyCanonicalizer
{
    private readonly Indexing.KeyCanonicalizer _canonicalizer = new();

    public bool TryCanonicalize(JsonElement element, out KeyValue? key, out Warning? warning)
    {
        if (_canonicalizer.TryCanonicalize(element, out var canonicalKey, out warning))
        {
            key = canonicalKey;
            return true;
        }

        key = null;
        return false;
    }
}
