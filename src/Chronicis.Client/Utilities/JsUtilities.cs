namespace Chronicis.Client.Utilities;

/// <summary>
/// Utility methods for JavaScript interop
/// </summary>
public static class JsUtilities
{
    /// <summary>
    /// Escapes a string for safe use in JavaScript string literals.
    /// Handles single quotes, newlines, and carriage returns.
    /// </summary>
    /// <param name="text">The text to escape</param>
    /// <returns>The escaped string safe for JS interpolation</returns>
    public static string EscapeForJs(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\", "\\\\")  // Escape backslashes first
            .Replace("'", "\\'")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}
