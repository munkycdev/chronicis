using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Output;

public sealed class TemplateRenderer
{
    public bool TryRender(string template, JsonObject payload, out string renderedPath, out Warning? warning)
    {
        return TryRenderInternal(template, payload, validatePath: true, out renderedPath, out warning);
    }

    public bool TryRenderValue(string template, JsonObject payload, out string renderedValue, out Warning? warning)
    {
        return TryRenderInternal(template, payload, validatePath: false, out renderedValue, out warning);
    }

    private static bool TryRenderInternal(
        string template,
        JsonObject payload,
        bool validatePath,
        out string renderedValue,
        out Warning? warning)
    {
        renderedValue = string.Empty;
        warning = null;

        if (string.IsNullOrWhiteSpace(template))
        {
            warning = new Warning(
                WarningCode.OutputTemplateMissingToken,
                WarningSeverity.Error,
                "Output template is empty.");
            return false;
        }

        var result = template;
        var start = result.IndexOf('{');
        while (start >= 0)
        {
            var end = result.IndexOf('}', start + 1);
            if (end < 0)
            {
                warning = new Warning(
                    WarningCode.OutputTemplateMissingToken,
                    WarningSeverity.Error,
                    $"Template token is not closed in '{template}'.");
                return false;
            }

            var token = result.Substring(start + 1, end - start - 1);
            if (string.IsNullOrWhiteSpace(token))
            {
                warning = new Warning(
                    WarningCode.OutputTemplateMissingToken,
                    WarningSeverity.Error,
                    $"Template token is empty in '{template}'.");
                return false;
            }

            if (!TryResolveToken(payload, token, out var value, out warning))
            {
                return false;
            }

            if (validatePath && IsUnsafeTokenValue(value))
            {
                warning = new Warning(
                    WarningCode.OutputTemplateTokenNotScalar,
                    WarningSeverity.Error,
                    $"Template token '{token}' contains unsafe path segments.");
                return false;
            }

            result = result.Substring(0, start) + value + result.Substring(end + 1);
            start = result.IndexOf('{', start + value.Length);
        }

        if (validatePath && !IsSafeRelativePath(result))
        {
            warning = new Warning(
                WarningCode.OutputTemplateMissingToken,
                WarningSeverity.Error,
                $"Output path '{result}' is not a safe relative path.");
            return false;
        }

        renderedValue = validatePath ? result.Replace('\\', '/') : result;
        return true;
    }

    private static bool TryResolveToken(JsonObject payload, string token, out string value, out Warning? warning)
    {
        warning = null;
        value = string.Empty;

        if (TryGetScalar(payload[token], token, out value, out var scalarWarning))
        {
            return true;
        }
        if (scalarWarning is not null)
        {
            warning = scalarWarning;
            return false;
        }

        if (payload["fields"] is JsonObject fields)
        {
            if (TryGetScalar(fields[token], token, out value, out scalarWarning))
            {
                return true;
            }

            if (scalarWarning is not null)
            {
                warning = scalarWarning;
                return false;
            }
        }

        warning = new Warning(
            WarningCode.OutputTemplateMissingToken,
            WarningSeverity.Error,
            $"Template token '{token}' could not be resolved.");
        return false;
    }

    private static bool TryGetScalar(JsonNode? node, string token, out string value, out Warning? warning)
    {
        warning = null;
        value = string.Empty;
        if (node is null)
        {
            return false;
        }

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

    private static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (Path.IsPathRooted(path))
        {
            return false;
        }

        var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.All(segment => segment != "..");
    }

    private static bool IsUnsafeTokenValue(string value)
    {
        if (value.Contains('/') || value.Contains('\\'))
        {
            return true;
        }

        var segments = value.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment == "..");
    }
}
