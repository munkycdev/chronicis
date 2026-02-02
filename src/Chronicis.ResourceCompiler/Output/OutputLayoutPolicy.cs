using System.Security.Cryptography;
using System.Text;

namespace Chronicis.ResourceCompiler.Output;

public sealed class OutputLayoutPolicy
{
    public string GetEntityFolderName(string entityName)
    {
        return $"{Slugify(entityName)}-{ShortHash(entityName)}";
    }

    public string GetCompiledDocumentsPath(string outputRoot, string entityFolder)
    {
        return Path.Combine(outputRoot, entityFolder, "documents.json");
    }

    public string GetPkIndexPath(string outputRoot, string entityFolder)
    {
        return Path.Combine(outputRoot, entityFolder, "indexes", "by-pk.json");
    }

    public string GetFkIndexPath(string outputRoot, string entityFolder, string childEntityName, string fkFieldName)
    {
        var childSegment = $"{Slugify(childEntityName)}-{ShortHash(childEntityName)}";
        var fieldSegment = $"{Slugify(fkFieldName)}-{ShortHash(fkFieldName)}";
        var fileName = $"{childSegment}__{fieldSegment}.json";
        return Path.Combine(outputRoot, entityFolder, "indexes", "fk", fileName);
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "entity";
        }

        var builder = new StringBuilder(value.Length);
        var previousHyphen = false;

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                previousHyphen = false;
                continue;
            }

            if (!previousHyphen)
            {
                builder.Append('-');
                previousHyphen = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "entity" : slug;
    }

    private static string ShortHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash.AsSpan(0, 4)).ToLowerInvariant();
    }
}
