using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Chronicis.Client.Components.World;
using Xunit;

namespace Chronicis.Client.Tests.Components.World;

[ExcludeFromCodeCoverage]
public class WorldDocumentUploadDialogTests
{
    [Theory]
    [InlineData(512L, "512 B")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    public void FormatFileSize_ReturnsExpectedUnits(long bytes, string expected)
    {
        var method = typeof(WorldDocumentUploadDialog)
            .GetMethod("FormatFileSize", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var result = (string?)method!.Invoke(null, [bytes]);
        Assert.Equal(expected, result);
    }
}
