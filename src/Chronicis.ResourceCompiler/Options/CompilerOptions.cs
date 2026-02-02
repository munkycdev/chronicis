namespace Chronicis.ResourceCompiler.Options;

public sealed class CompilerOptions
{
    public string ManifestPath { get; init; } = string.Empty;
    public string OutputRoot { get; init; } = string.Empty;

    public static CompilerOptions FromArgs(string[] args)
    {
        _ = args;
        return new CompilerOptions();
    }
}
