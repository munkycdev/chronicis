namespace Chronicis.ResourceCompiler.Options;

public sealed class CompilerOptions
{
    public string ManifestPath { get; init; } = string.Empty;
    public string RawPath { get; init; } = string.Empty;
    public string OutputRoot { get; init; } = string.Empty;
    public int MaxDepth { get; init; } = 3;
    public bool Verbose { get; init; }

    public static bool TryParse(string[] args, out CompilerOptions options, out string? error, out bool showHelp)
    {
        options = new CompilerOptions();
        error = null;
        showHelp = false;

        if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
        {
            showHelp = true;
            return false;
        }

        var manifest = GetValue(args, "--manifest");
        var raw = GetValue(args, "--raw");
        var output = GetValue(args, "--out");

        if (string.IsNullOrWhiteSpace(manifest) || string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(output))
        {
            error = "Missing required arguments. Expected --manifest <path> --raw <path> --out <path>.";
            return false;
        }

        var maxDepthText = GetValue(args, "--maxDepth");
        var maxDepth = 3;
        if (!string.IsNullOrWhiteSpace(maxDepthText) && !int.TryParse(maxDepthText, out maxDepth))
        {
            error = "Invalid --maxDepth value. Expected an integer.";
            return false;
        }

        options = new CompilerOptions
        {
            ManifestPath = manifest,
            RawPath = raw,
            OutputRoot = output,
            MaxDepth = maxDepth,
            Verbose = args.Contains("--verbose", StringComparer.OrdinalIgnoreCase)
        };

        return true;
    }

    private static string? GetValue(string[] args, string key)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
