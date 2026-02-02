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

        try
        {
            var outputWriter = new OutputWriter();
            var layoutPolicy = new OutputLayoutPolicy();
            await WriteOutputsAsync(outputWriter, layoutPolicy, options.OutputRoot, compilationResult, indexResult, cancellationToken);
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

    private static async Task WriteOutputsAsync(
        OutputWriter writer,
        OutputLayoutPolicy layoutPolicy,
        string outputRoot,
        Compilation.Models.CompilationResult compilationResult,
        IndexBuildResult indexResult,
        CancellationToken cancellationToken)
    {
        var tempRoot = $"{outputRoot}.tmp";
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, true);
        }

        await writer.WriteAsync(tempRoot, compilationResult, indexResult, layoutPolicy, cancellationToken);

        if (Directory.Exists(outputRoot))
        {
            Directory.Delete(outputRoot, true);
        }

        Directory.Move(tempRoot, outputRoot);
    }
}
