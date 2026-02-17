namespace Chronicis.ResourceCompiler.Warnings;

public sealed class WarningSink
{
    private readonly List<Warning> _warnings = new();

    public IReadOnlyList<Warning> Warnings => _warnings;

    public void Add(Warning warning)
    {
        _warnings.Add(warning);
    }

    public void AddRange(IEnumerable<Warning> warnings)
    {
        _warnings.AddRange(warnings);
    }
}
