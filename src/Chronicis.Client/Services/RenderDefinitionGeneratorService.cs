using System.Text.Json;
using Chronicis.Client.Models;

namespace Chronicis.Client.Services;

/// <summary>
/// Generates a starter RenderDefinition from a sample JSON record.
/// Uses heuristics to group fields, detect ability scores, and choose render hints.
/// The output is a starting point for manual refinement.
/// </summary>
public static class RenderDefinitionGeneratorService
{
    public static RenderDefinition Generate(JsonElement sample)
    {
        var dataSource = UnwrapFields(sample);
        if (dataSource.ValueKind != JsonValueKind.Object)
            return CreateMinimal();

        var (titleField, hidden, remaining) = FieldClassifier.Classify(dataSource);
        var prefixGroups = PrefixGroupDetector.DetectGroups(remaining);
        var sections = SectionBuilder.Build(remaining, prefixGroups);

        return new RenderDefinition
        {
            TitleField = titleField,
            CatchAll = true,
            Hidden = hidden,
            Sections = sections
        };
    }

    private static JsonElement UnwrapFields(JsonElement sample)
    {
        if (sample.ValueKind == JsonValueKind.Object &&
            sample.TryGetProperty("fields", out var fields) &&
            fields.ValueKind == JsonValueKind.Object)
        {
            return fields;
        }
        return sample;
    }

    private static RenderDefinition CreateMinimal() => new()
    {
        CatchAll = true,
        Sections = new List<RenderSection>()
    };
}
