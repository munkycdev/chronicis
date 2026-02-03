using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Compilation.Models;
using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Output;

public sealed class OutputWriter
{
    private readonly TemplateRenderer _renderer = new();
    private readonly JsonFieldAccessor _fieldAccessor = new();

    public Task<OutputWriteResult> WriteAsync(
        string outputRoot,
        Manifest.Models.Manifest manifest,
        CompilationResult compilationResult,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            throw new ArgumentException("Output root must be provided.", nameof(outputRoot));
        }

        var warnings = new List<Warning>();
        var writtenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var documentsByEntity = GroupDocuments(compilationResult.Documents);

        foreach (var entity in manifest.Entities.Values.Where(entity => entity.IsRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entity.Output is null || string.IsNullOrWhiteSpace(entity.Output.BlobTemplate))
            {
                warnings.Add(new Warning(
                    WarningCode.OutputTemplateMissingToken,
                    WarningSeverity.Error,
                    $"Entity '{entity.Name}' does not define output.blobTemplate.",
                    entity.Name));
                continue;
            }

            documentsByEntity.TryGetValue(entity.Name, out var documents);
            documents ??= new List<CompiledDocument>();

            foreach (var document in documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (document.Payload is not JsonObject payload)
                {
                    warnings.Add(new Warning(
                        WarningCode.OutputTemplateTokenNotScalar,
                        WarningSeverity.Error,
                        $"Compiled payload for '{entity.Name}' is not a JSON object.",
                        entity.Name));
                    continue;
                }

                if (!TryCreateRenderPayload(entity, payload, entity.Output.BlobTemplate, out var renderPayload, out var renderWarning))
                {
                    warnings.Add(WithEntity(renderWarning!, entity.Name));
                    continue;
                }

                if (!_renderer.TryRender(entity.Output.BlobTemplate, renderPayload, out var relativePath, out var templateWarning))
                {
                    warnings.Add(WithEntity(templateWarning!, entity.Name));
                    continue;
                }

                if (!writtenPaths.Add(relativePath))
                {
                    warnings.Add(new Warning(
                        WarningCode.OutputBlobPathCollision,
                        WarningSeverity.Error,
                        $"Output path collision for '{relativePath}'.",
                        entity.Name,
                        relativePath));
                    continue;
                }

                var fullPath = Path.Combine(outputRoot, relativePath);
                WriteJsonAtomic(fullPath, payload, cancellationToken);
            }

            if (entity.Output.Index is not null)
            {
                var indexTemplate = entity.Output.Index.Blob;
                var indexPayload = documents.FirstOrDefault()?.Payload as JsonObject ?? new JsonObject();
                if (!TryCreateRenderPayload(entity, indexPayload, indexTemplate, out var renderPayload, out var renderWarning))
                {
                    warnings.Add(WithEntity(renderWarning!, entity.Name));
                    continue;
                }

                if (!_renderer.TryRender(indexTemplate, renderPayload, out var indexRelativePath, out var templateWarning))
                {
                    warnings.Add(WithEntity(templateWarning!, entity.Name));
                    continue;
                }

                if (!writtenPaths.Add(indexRelativePath))
                {
                    warnings.Add(new Warning(
                        WarningCode.OutputBlobPathCollision,
                        WarningSeverity.Error,
                        $"Output path collision for '{indexRelativePath}'.",
                        entity.Name,
                        indexRelativePath));
                    continue;
                }

                var indexArray = new JsonArray();
                foreach (var document in documents)
                {
                    if (document.Payload is not JsonObject docPayload)
                    {
                        warnings.Add(new Warning(
                            WarningCode.OutputTemplateTokenNotScalar,
                            WarningSeverity.Error,
                            $"Compiled payload for '{entity.Name}' is not a JSON object.",
                            entity.Name));
                        continue;
                    }

                    var projection = new JsonObject();
                    foreach (var field in entity.Output.Index.Fields)
                    {
                        if (_fieldAccessor.TryGetField(docPayload, field, out var value))
                        {
                            projection[field] = value?.DeepClone();
                        }
                        else
                        {
                            warnings.Add(new Warning(
                                WarningCode.OutputIndexFieldMissing,
                                WarningSeverity.Warning,
                                $"Index field '{field}' is missing for entity '{entity.Name}'.",
                                entity.Name,
                                $"$.{field}"));
                            projection[field] = null;
                        }
                    }

                    indexArray.Add(projection);
                }

                var indexFullPath = Path.Combine(outputRoot, indexRelativePath);
                WriteJsonAtomic(indexFullPath, indexArray, cancellationToken);
            }
        }

        return Task.FromResult(new OutputWriteResult(warnings));
    }

    private static Dictionary<string, List<CompiledDocument>> GroupDocuments(IReadOnlyList<CompiledDocument> documents)
    {
        var result = new Dictionary<string, List<CompiledDocument>>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            if (!result.TryGetValue(document.EntityName, out var list))
            {
                list = new List<CompiledDocument>();
                result[document.EntityName] = list;
            }

            list.Add(document);
        }

        return result;
    }

    private static Warning WithEntity(Warning warning, string entityName)
    {
        if (!string.IsNullOrWhiteSpace(warning.EntityName))
        {
            return warning;
        }

        return new Warning(
            warning.Code,
            warning.Severity,
            warning.Message,
            entityName,
            warning.JsonPath);
    }

    private static void WriteJsonAtomic(string path, JsonNode node, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions { WriteIndented = false };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(node, options);

        var tempPath = $"{path}.tmp";
        File.WriteAllBytes(tempPath, bytes);
        cancellationToken.ThrowIfCancellationRequested();

        File.Move(tempPath, path, true);
    }

    private bool TryCreateRenderPayload(
        ManifestEntity entity,
        JsonObject payload,
        string template,
        out JsonObject renderPayload,
        out Warning? warning)
    {
        renderPayload = payload;
        warning = null;

        if (string.IsNullOrWhiteSpace(template))
        {
            return true;
        }

        var needsSlug = RequiresToken(template, "slug");
        var needsId = RequiresToken(template, "id");
        var idTemplate = entity.Identity?.IdTemplate;
        var needsSlugForId = needsId && RequiresToken(idTemplate, "slug");
        var requiresSlugOverride = (needsSlug || needsSlugForId) && !HasToken(payload, "slug");
        var requiresIdOverride = needsId && !HasToken(payload, "id") && !string.IsNullOrWhiteSpace(idTemplate);

        if (!requiresSlugOverride && !requiresIdOverride)
        {
            return true;
        }

        renderPayload = payload.DeepClone() as JsonObject ?? new JsonObject();

        if (requiresSlugOverride)
        {
            if (!TryResolveSlug(entity, payload, out var slugValue, out warning))
            {
                return false;
            }

            renderPayload["slug"] = slugValue;
        }

        if (requiresIdOverride && !string.IsNullOrWhiteSpace(idTemplate))
        {
            if (!_renderer.TryRenderValue(idTemplate, renderPayload, out var idValue, out var idWarning))
            {
                warning = idWarning;
                return false;
            }

            renderPayload["id"] = idValue;
        }

        return true;
    }

    private bool TryResolveSlug(ManifestEntity entity, JsonObject payload, out string value, out Warning? warning)
    {
        value = string.Empty;
        warning = null;

        var slugField = entity.Identity?.SlugField;
        if (string.IsNullOrWhiteSpace(slugField))
        {
            warning = new Warning(
                WarningCode.OutputTemplateMissingToken,
                WarningSeverity.Error,
                "Template token 'slug' requires identity.slugField.");
            return false;
        }

        if (!_fieldAccessor.TryGetField(payload, slugField, out var node) || node is null)
        {
            warning = new Warning(
                WarningCode.OutputTemplateMissingToken,
                WarningSeverity.Error,
                $"Template token 'slug' could not be resolved from field '{slugField}'.");
            return false;
        }

        return TryGetScalarString(node, "slug", out value, out warning);
    }

    private static bool TryGetScalarString(JsonNode node, string token, out string value, out Warning? warning)
    {
        warning = null;
        value = string.Empty;

        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string>(out var stringValue))
            {
                value = stringValue ?? string.Empty;
                return true;
            }

            if (jsonValue.TryGetValue<bool>(out var boolValue))
            {
                value = boolValue ? "true" : "false";
                return true;
            }

            if (jsonValue.TryGetValue<long>(out var longValue))
            {
                value = longValue.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (jsonValue.TryGetValue<double>(out var doubleValue))
            {
                value = doubleValue.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (jsonValue.TryGetValue<JsonElement>(out var element))
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    value = element.GetString() ?? string.Empty;
                    return true;
                }

                if (element.ValueKind == JsonValueKind.Number)
                {
                    value = element.GetRawText();
                    return true;
                }

                if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                {
                    value = element.GetBoolean() ? "true" : "false";
                    return true;
                }
            }
        }

        warning = new Warning(
            WarningCode.OutputTemplateTokenNotScalar,
            WarningSeverity.Error,
            $"Template token '{token}' resolved to a non-scalar value.");
        return false;
    }

    private static bool RequiresToken(string? template, string token)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return false;
        }

        var marker = $"{{{token}}}";
        return template.Contains(marker, StringComparison.Ordinal);
    }

    private static bool HasToken(JsonObject payload, string token)
    {
        return payload.TryGetPropertyValue(token, out var node) && node is not null;
    }
}
