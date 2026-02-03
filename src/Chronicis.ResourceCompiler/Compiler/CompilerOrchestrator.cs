using System.Linq;
using Chronicis.ResourceCompiler.Compilation;
using Chronicis.ResourceCompiler.Indexing;
using Chronicis.ResourceCompiler.Manifest;
using Chronicis.ResourceCompiler.Options;
using Chronicis.ResourceCompiler.Output;
using Chronicis.ResourceCompiler.Raw;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Compiler;

public sealed class CompilerOrchestrator
{
    public async Task<CompilerRunResult> RunAsync(CompilerOptions options, CancellationToken cancellationToken)
    {
        var warnings = new List<Warning>();

        var manifestLoader = new ManifestLoader();
        var manifestValidator = new ManifestValidator();
        var manifestResult = await manifestLoader.LoadAsync(options.ManifestPath, cancellationToken);
        warnings.AddRange(manifestResult.Warnings);

        if (manifestResult.Manifest is null || manifestResult.HasErrors)
        {
            return new CompilerRunResult(warnings);
        }

        var validationWarnings = manifestValidator.Validate(manifestResult.Manifest);
        warnings.AddRange(validationWarnings);
        if (validationWarnings.Any(warning => warning.Severity == WarningSeverity.Error))
        {
            return new CompilerRunResult(warnings);
        }

        var rawLoader = new RawDataLoader();
        var rawResult = await rawLoader.LoadAsync(manifestResult.Manifest, options.RawPath, cancellationToken);
        warnings.AddRange(rawResult.Warnings);
        if (rawResult.HasErrors)
        {
            return new CompilerRunResult(warnings);
        }

        var indexBuilder = new IndexBuilder();
        var indexResult = indexBuilder.BuildIndexes(manifestResult.Manifest, rawResult);
        warnings.AddRange(indexResult.Warnings);
        if (indexResult.HasErrors)
        {
            return new CompilerRunResult(warnings);
        }

        var assembler = new DocumentAssembler();
        var compilationResult = await assembler.AssembleAsync(
            manifestResult.Manifest,
            rawResult,
            indexResult,
            options,
            cancellationToken);
        warnings.AddRange(compilationResult.Warnings);
        if (compilationResult.HasErrors)
        {
            return new CompilerRunResult(warnings);
        }

        if (warnings.Any(warning => warning.Severity == WarningSeverity.Error))
        {
            return new CompilerRunResult(warnings);
        }

        try
        {
            var outputWriter = new OutputWriter();
            var outputWarnings = await WriteOutputsAsync(
                outputWriter,
                options.OutputRoot,
                manifestResult.Manifest,
                compilationResult,
                cancellationToken);
            warnings.AddRange(outputWarnings);
        }
        catch (Exception ex)
        {
            warnings.Add(new Warning(
                WarningCode.OutputWriteFailed,
                WarningSeverity.Error,
                $"Failed to write output: {ex.Message}"));
        }

        return new CompilerRunResult(warnings);
    }

    private static async Task<IReadOnlyList<Warning>> WriteOutputsAsync(
        OutputWriter writer,
        string outputRoot,
        Manifest.Models.Manifest manifest,
        Compilation.Models.CompilationResult compilationResult,
        CancellationToken cancellationToken)
    {
        var tempRoot = $"{outputRoot}.tmp";
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, true);
        }

        try
        {
            var result = await writer.WriteAsync(tempRoot, manifest, compilationResult, cancellationToken);

            if (result.HasErrors)
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }

                return result.Warnings;
            }

            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, true);
            }

            Directory.Move(tempRoot, outputRoot);
            return result.Warnings;
        }
        catch
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }

            throw;
        }
    }
}
