using Chronicis.ResourceCompiler.Compiler;
using Chronicis.ResourceCompiler.Options;

var options = CompilerOptions.FromArgs(args);
var orchestrator = new CompilerOrchestrator();
var exitCode = await orchestrator.RunAsync(options, CancellationToken.None);
return exitCode;
