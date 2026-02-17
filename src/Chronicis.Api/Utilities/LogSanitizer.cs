using System.Text.RegularExpressions;

namespace Chronicis.Api.Utilities;

/// <summary>
/// Provides aggressive sanitization of user input before logging to prevent log injection attacks.
/// </summary>
public static class LogSanitizer
{
    private const int MaxLogLength = 1000;
    private const string TruncationMarker = "...[TRUNCATED]";
    private const string SanitizedMarker = "[SANITIZED]";

    // Matches all control characters including newlines, carriage returns, tabs, etc.
    private static readonly Regex ControlCharPattern = new(@"[\x00-\x1F\x7F-\x9F]", RegexOptions.Compiled);

    // Matches potentially dangerous sequences
    private static readonly Regex DangerousPattern = new(@"(\r\n|\r|\n|\t|%0[aAdD]|%0[aA]|%0[dD])", RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes a string value for safe logging by removing control characters and truncating length.
    /// </summary>
    /// <param name="value">The value to sanitize. Can be null.</param>
    /// <returns>A sanitized version of the input safe for logging.</returns>
    public static string? Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // First pass: Remove control characters
        var sanitized = ControlCharPattern.Replace(value, "");

        // Second pass: Remove URL-encoded dangerous characters
        sanitized = DangerousPattern.Replace(sanitized, "");

        // Remove any remaining whitespace sequences longer than 1 space
        sanitized = Regex.Replace(sanitized, @"\s{2,}", " ");

        // Trim the result
        sanitized = sanitized.Trim();

        // Check if we sanitized anything
        var wasSanitized = sanitized != value;

        // Truncate if too long
        if (sanitized.Length > MaxLogLength)
        {
            sanitized = sanitized.Substring(0, MaxLogLength) + TruncationMarker;
            wasSanitized = true;
        }

        // Add marker if content was modified
        if (wasSanitized && !string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = $"{sanitized} {SanitizedMarker}";
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes an object by converting it to string and then sanitizing.
    /// Handles null values gracefully.
    /// </summary>
    /// <param name="value">The object to sanitize. Can be null.</param>
    /// <returns>A sanitized string representation safe for logging.</returns>
    public static string? SanitizeObject(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return Sanitize(value.ToString());
    }

    /// <summary>
    /// Sanitizes multiple values for logging.
    /// </summary>
    /// <param name="values">Array of values to sanitize.</param>
    /// <returns>Array of sanitized values in the same order.</returns>
    public static string?[] SanitizeMultiple(params string?[] values)
    {
        return values.Select(Sanitize).ToArray();
    }
}
