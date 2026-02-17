using System.Diagnostics.CodeAnalysis;
using Chronicis.ResourceCompiler.Compiler;
using Chronicis.ResourceCompiler.Options;
using Chronicis.ResourceCompiler.Warnings;
using Microsoft.Extensions.Logging;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

        var logger = loggerFactory.CreateLogger<Program>();

        if (!CompilerOptions.TryParse(args, out var options, out var error, out var showHelp))
        {
            if (showHelp)
            {
                return 0;
            }

            logger.LogWarning("{error}", error ?? "Invalid arguments.");
            return 1;
        }

        var orchestrator = new CompilerOrchestrator();
        var result = await orchestrator.RunAsync(options, CancellationToken.None);

        var warningCount = result.Warnings.Count;
        var errorCount = result.Warnings.Count(warning => warning.Severity == WarningSeverity.Error);
        var warnCount = warningCount - errorCount;

        logger.LogWarning("Warnings: {WarningCount} (Errors: {ErrorCount}, Warnings: {WarnCount})",
            warningCount, errorCount, warnCount);

        if (options.Verbose && result.Warnings.Count > 0)
        {
            foreach (var warning in result.Warnings)
            {
                var entity = string.IsNullOrWhiteSpace(warning.EntityName) ? "-" : warning.EntityName;
                var path = string.IsNullOrWhiteSpace(warning.JsonPath) ? "-" : warning.JsonPath;
                logger.LogWarning("{Severity}\t{Code}\t{Entity}\t{Path}\t{Message}",
                    warning.Severity, warning.Code, entity, path, warning.Message);
            }
        }
        return errorCount > 0 ? 1 : 0;
    }
}
