using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Options;
using Chronicis.ResourceCompiler.Warnings;
using Xunit;

namespace Chronicis.ResourceCompiler.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    public void WarningSinkCollectsWarnings()
    {
        var sink = new WarningSink();
        var warning = new Warning(WarningCode.InvalidKey, WarningSeverity.Warning, "test warning");

        sink.Add(warning);

        Assert.Single(sink.Warnings);
        Assert.Equal(warning, sink.Warnings[0]);
    }

    [Fact]
    public void TypesCanBeInstantiated()
    {
        var options = new CompilerOptions();
        var entity = new ManifestEntity();
        var child = new ManifestChild();

        Assert.NotNull(options);
        Assert.NotNull(entity);
        Assert.NotNull(child);
    }
}
