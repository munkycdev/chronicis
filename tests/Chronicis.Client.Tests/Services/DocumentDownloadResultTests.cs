using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class DocumentDownloadResultTests
{
    [Fact]
    public void Ctor_SetsProperties()
    {
        var sut = new DocumentDownloadResult("url", "file.pdf", "application/pdf", 42);

        Assert.Equal("url", sut.DownloadUrl);
        Assert.Equal("file.pdf", sut.FileName);
        Assert.Equal("application/pdf", sut.ContentType);
        Assert.Equal(42, sut.FileSizeBytes);
    }
}

