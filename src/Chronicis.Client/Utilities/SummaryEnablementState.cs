namespace Chronicis.Client.Utilities;

/// <summary>
/// Determines whether AI summary generation should be enabled based on article body content.
/// </summary>
internal static class SummaryEnablementState
{
    /// <summary>
    /// Returns true when AI summary generation should be enabled (body has non-whitespace content).
    /// </summary>
    public static bool IsSummaryEnabled(string? body)
        => !string.IsNullOrWhiteSpace(body);
}
