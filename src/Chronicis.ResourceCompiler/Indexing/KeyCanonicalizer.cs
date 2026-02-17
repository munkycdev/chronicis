using System.Globalization;
using System.Text.Json;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Indexing;

public sealed class KeyCanonicalizer
{
    public bool TryCanonicalize(JsonElement element, out KeyValue key, out Warning? warning)
    {
        warning = null;
        key = default;

        switch (element.ValueKind)
        {
            case JsonValueKind.String:
            {
                var value = element.GetString() ?? string.Empty;
                key = new KeyValue(KeyKind.String, value);
                return true;
            }
            case JsonValueKind.True:
            case JsonValueKind.False:
            {
                var value = element.GetBoolean() ? "true" : "false";
                key = new KeyValue(KeyKind.Boolean, value);
                return true;
            }
            case JsonValueKind.Number:
            {
                var rawText = element.GetRawText();
                key = new KeyValue(KeyKind.Number, CanonicalizeNumber(rawText));
                return true;
            }
            default:
                warning = new Warning(
                    WarningCode.InvalidKey,
                    WarningSeverity.Error,
                    $"Invalid key type: {element.ValueKind}.");
                return false;
        }
    }

    private static string CanonicalizeNumber(string rawText)
    {
        var trimmed = rawText.Trim();
        if (decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue))
        {
            if (decimalValue == 0m)
            {
                return "0";
            }

            var formatted = decimalValue.ToString("0.############################", CultureInfo.InvariantCulture);
            return formatted == "-0" ? "0" : formatted;
        }

        if (IsNegativeZeroToken(trimmed))
        {
            return "0";
        }

        return trimmed;
    }

    private static bool IsNegativeZeroToken(string rawText)
    {
        if (!rawText.StartsWith("-", StringComparison.Ordinal))
        {
            return false;
        }

        var unsigned = rawText[1..].Trim();
        if (string.IsNullOrEmpty(unsigned))
        {
            return false;
        }

        var exponentIndex = unsigned.IndexOfAny(new[] { 'e', 'E' });
        var basePart = exponentIndex >= 0 ? unsigned[..exponentIndex] : unsigned;

        if (!IsZeroDecimal(basePart))
        {
            return false;
        }

        if (exponentIndex < 0)
        {
            return true;
        }

        var exponentPart = unsigned[(exponentIndex + 1)..];
        return IsValidExponent(exponentPart);
    }

    private static bool IsZeroDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var span = value.AsSpan().Trim();
        var hasDigit = false;

        foreach (var ch in span)
        {
            if (ch == '.')
            {
                continue;
            }

            if (ch is < '0' or > '9')
            {
                return false;
            }

            if (ch != '0')
            {
                return false;
            }

            hasDigit = true;
        }

        return hasDigit;
    }

    private static bool IsValidExponent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var span = value.AsSpan().Trim();
        if (span.Length == 0)
        {
            return false;
        }

        var start = (span[0] == '+' || span[0] == '-') ? 1 : 0;
        if (start == span.Length)
        {
            return false;
        }

        for (var i = start; i < span.Length; i++)
        {
            if (span[i] is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}
