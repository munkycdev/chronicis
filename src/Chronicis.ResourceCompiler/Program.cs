using System.Linq;
using Chronicis.ResourceCompiler.Compiler;
using Chronicis.ResourceCompiler.Options;
using Chronicis.ResourceCompiler.Warnings;

if (!CompilerOptions.TryParse(args, out var options, out var error, out var showHelp))
{
    if (showHelp)
    {
        Console.WriteLine("Usage: Chronicis.ResourceCompiler --manifest <path> --raw <path> --out <path> [--maxDepth <int>] [--verbose]");
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

if (options.Verbose && result.Warnings.Count > 0)
{
    foreach (var warning in result.Warnings)
    {
        var entity = string.IsNullOrWhiteSpace(warning.EntityName) ? "-" : warning.EntityName;
        var path = string.IsNullOrWhiteSpace(warning.JsonPath) ? "-" : warning.JsonPath;
        Console.WriteLine($"{warning.Severity}\t{warning.Code}\t{entity}\t{path}\t{warning.Message}");
    }
}
return errorCount > 0 ? 1 : 0;
