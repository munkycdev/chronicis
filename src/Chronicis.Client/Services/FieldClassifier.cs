using System.Text.Json;
using static Chronicis.Client.Services.RenderDefinitionHelpers;

namespace Chronicis.Client.Services;

/// <summary>
/// Extracts and classifies JSON fields into title, hidden, and remaining categories.
/// </summary>
public static class FieldClassifier
{
    public static (string titleField, List<string> hidden, List<FieldInfo> remaining) Classify(JsonElement dataSource)
    {
        var fieldInfos = new List<FieldInfo>();
        foreach (var prop in dataSource.EnumerateObject())
        {
            fieldInfos.Add(new FieldInfo
            {
                Name = prop.Name,
                Value = prop.Value,
                Kind = prop.Value.ValueKind,
                IsNull = IsNullOrEmpty(prop.Value),
                IsComplex = prop.Value.ValueKind == JsonValueKind.Object ||
                            prop.Value.ValueKind == JsonValueKind.Array
            });
        }

        var titleField = fieldInfos
            .FirstOrDefault(f => TitleCandidates.Contains(f.Name, StringComparer.OrdinalIgnoreCase))
            ?.Name ?? "name";

        var hidden = new List<string>();
        var remaining = new List<FieldInfo>();

        foreach (var fi in fieldInfos)
        {
            if (fi.Name.Equals(titleField, StringComparison.OrdinalIgnoreCase))
                continue;
            if (IsHiddenField(fi.Name))
                hidden.Add(fi.Name);
            else
                remaining.Add(fi);
        }

        return (titleField, hidden, remaining);
    }
}

/// <summary>
/// Describes a single JSON property with classification metadata.
/// </summary>
public class FieldInfo
{
    public string Name { get; set; } = "";
    public JsonElement Value { get; set; }
    public JsonValueKind Kind { get; set; }
    public bool IsNull { get; set; }
    public bool IsComplex { get; set; }
}
