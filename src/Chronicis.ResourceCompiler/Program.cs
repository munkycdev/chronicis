using System.Linq;
using Chronicis.ResourceCompiler.Compiler;
using Chronicis.ResourceCompiler.Options;
using Chronicis.ResourceCompiler.Warnings;

if (!CompilerOptions.TryParse(args, out var options, out var error, out var showHelp))
{
    if (showHelp)
    {
        Console.WriteLine("Usage: Chronicis.ResourceCompiler --manifest <path> --raw <path> --out <path> [--maxDepth <int>]");
        return 0;
    }

    Console.WriteLine(error ?? "Invalid arguments.");
    return 1;
}

var orchestrator = new CompilerOrchestrator();
var result = await orchestrator.RunAsync(options, CancellationToken.None);

var warningCount = result.Warnings.Count;
var errorCount = result.Warnings.Count(warning => warning.Severity == WarningSeverity.Error);
var warnCount = warningCount - errorCount;

Console.WriteLine($"Warnings: {warningCount} (Errors: {errorCount}, Warnings: {warnCount})");
return errorCount > 0 ? 1 : 0;
