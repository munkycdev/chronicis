using Chronicis.Api.Services;
using Xunit;

namespace Chronicis.Api.Tests;

public class DocumentContentResultTests
{
    [Fact]
    public void Constructor_AssignsAllProperties()
    {
        var result = new DocumentContentResult(
            "https://docs.example.test/download",
            "session-notes.pdf",
            "application/pdf",
            12345);

        Assert.Equal("https://docs.example.test/download", result.DownloadUrl);
        Assert.Equal("session-notes.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(12345, result.FileSizeBytes);
    }
}
