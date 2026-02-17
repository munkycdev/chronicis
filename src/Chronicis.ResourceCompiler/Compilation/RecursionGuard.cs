using Chronicis.ResourceCompiler.Indexing.Models;

namespace Chronicis.ResourceCompiler.Compilation;

public sealed class RecursionGuard
{
    private readonly HashSet<PathEntry> _currentPath = new();
    private readonly Stack<PathEntry> _stack = new();

    public bool TryEnter(string entityName, KeyValue key, int depth)
    {
        _ = depth;
        var entry = new PathEntry(entityName, key);
        if (_currentPath.Contains(entry))
        {
            return false;
        }

        _currentPath.Add(entry);
        _stack.Push(entry);
        return true;
    }

    public void Exit(string entityName, KeyValue key)
    {
        var entry = new PathEntry(entityName, key);
        if (_stack.Count == 0)
        {
            return;
        }

        var top = _stack.Pop();
        _currentPath.Remove(top);

        if (!top.Equals(entry))
        {
            _currentPath.Remove(entry);
        }
    }

    private readonly record struct PathEntry(string EntityName, KeyValue Key);
}
