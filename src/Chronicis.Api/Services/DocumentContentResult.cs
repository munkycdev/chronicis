using System.IO;

namespace Chronicis.Api.Services;

public sealed class DocumentContentResult
{
    public DocumentContentResult(Stream content, string fileName, string contentType, long? contentLength)
    {
        Content = content;
        FileName = fileName;
        ContentType = contentType;
        ContentLength = contentLength;
    }

    public Stream Content { get; }
    public string FileName { get; }
    public string ContentType { get; }
    public long? ContentLength { get; }
}
