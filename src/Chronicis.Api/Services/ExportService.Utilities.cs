using System.IO.Compression;
using System.Text;

namespace Chronicis.Api.Services;

public partial class ExportService
{
    private static async Task AddFileToArchive(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(content);
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Untitled";

        // Replace invalid filename characters
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(name);
        foreach (var c in invalid)
        {
            sanitized.Replace(c, '_');
        }

        // Also replace some characters that might cause issues
        sanitized.Replace('/', '_');
        sanitized.Replace('\\', '_');
        sanitized.Replace(':', '_');

        var result = sanitized.ToString().Trim();

        // Limit length
        if (result.Length > 100)
            result = result.Substring(0, 100);

        return result;
    }

    private static string EscapeYaml(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }
}
