using System.Text.Json;
using Chronicis.ResourceCompiler.Raw.Models;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Raw;

public sealed class RawDataLoader
{
    public async Task<RawLoadResult> LoadAsync(Manifest.Models.Manifest manifest, string inputRoot, CancellationToken cancellationToken)
    {
        var entitySets = new List<RawEntitySet>();
        var warnings = new List<Warning>();

        foreach (var pair in manifest.Entities)
        {
            var entityName = pair.Key;
            var entity = pair.Value;

            if (string.IsNullOrWhiteSpace(entity.File))
            {
                continue;
            }

            var filePath = Path.Combine(inputRoot, entity.File);
            if (!File.Exists(filePath))
            {
                warnings.Add(new Warning(
                    WarningCode.RawFileNotFound,
                    WarningSeverity.Error,
                    $"Raw data file not found: {filePath}",
                    entityName));
                continue;
            }

            JsonDocument document;
            try
            {
                await using var stream = File.OpenRead(filePath);
                document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
            catch (JsonException ex)
            {
                warnings.Add(new Warning(
                    WarningCode.RawJsonParseError,
                    WarningSeverity.Error,
                    $"Failed to parse JSON for entity '{entityName}': {ex.Message}",
                    entityName));
                continue;
            }
            catch (Exception ex)
            {
                warnings.Add(new Warning(
                    WarningCode.RawFileUnreadable,
                    WarningSeverity.Error,
                    $"Failed to read raw data file for entity '{entityName}': {ex.Message}",
                    entityName));
                continue;
            }

            var rows = new List<RawEntityRow>();
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                warnings.Add(new Warning(
                    WarningCode.RawRootNotArray,
                    WarningSeverity.Error,
                    $"Raw data root is not an array for entity '{entityName}'.",
                    entityName));
                entitySets.Add(new RawEntitySet(entityName, document, rows));
                continue;
            }

            var index = 0;
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    warnings.Add(new Warning(
                        WarningCode.RawRowNotObject,
                        WarningSeverity.Error,
                        $"Row {index} is not an object for entity '{entityName}'.",
                        entityName,
                        $"$[{index}]"));
                    index++;
                    continue;
                }

                var pkField = entity.PrimaryKey;
                JsonElement? pkElement = null;

                if (string.IsNullOrWhiteSpace(pkField))
                {
                    warnings.Add(new Warning(
                        WarningCode.MissingPk,
                        WarningSeverity.Error,
                        $"Entity '{entityName}' has no primary key configured.",
                        entityName,
                        $"$[{index}]"));
                }
                else if (element.TryGetProperty(pkField, out var pkValue))
                {
                    pkElement = pkValue;
                    if (pkValue.ValueKind is JsonValueKind.Null or JsonValueKind.Object or JsonValueKind.Array)
                    {
                        warnings.Add(new Warning(
                            WarningCode.InvalidPkType,
                            WarningSeverity.Error,
                            $"Entity '{entityName}' has an invalid PK type at row {index}.",
                            entityName,
                            $"$[{index}].{pkField}"));
                    }
                }
                else
                {
                    warnings.Add(new Warning(
                        WarningCode.MissingPk,
                        WarningSeverity.Error,
                        $"Entity '{entityName}' is missing PK '{pkField}' at row {index}.",
                        entityName,
                        $"$[{index}].{pkField}"));
                }

                rows.Add(new RawEntityRow(entityName, index, element, pkElement));
                index++;
            }

            entitySets.Add(new RawEntitySet(entityName, document, rows));
        }

        return new RawLoadResult(entitySets, warnings);
    }
}
