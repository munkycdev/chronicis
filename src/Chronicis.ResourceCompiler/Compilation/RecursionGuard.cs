using Chronicis.ResourceCompiler.Indexing.Models;

namespace Chronicis.ResourceCompiler.Compilation;

public sealed class RecursionGuard
{
    public bool TryEnter(string entityName, KeyValue key, int depth)
    {
        _ = entityName;
        _ = key;
        _ = depth;
        throw new NotImplementedException();
    }

    public void Exit(string entityName, KeyValue key)
    {
        _ = entityName;
        _ = key;
        throw new NotImplementedException();
    }
}
