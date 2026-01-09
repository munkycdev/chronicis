namespace Chronicis.Client.Services;

public sealed record DocumentDownloadResult(byte[] Content, string FileName, string ContentType);
