namespace Chronicis.Shared.DTOs;

/// <summary>
/// A contextual prompt/suggestion for the user.
/// </summary>
public class PromptDto
{
    /// <summary>
    /// Unique identifier for this prompt type (for dismissal tracking).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Short title for the prompt.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Longer descriptive message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Icon to display (emoji or Font Awesome class).
    /// </summary>
    public string Icon { get; set; } = "💡";

    /// <summary>
    /// Text for the action button. Null if no action.
    /// </summary>
    public string? ActionText { get; set; }

    /// <summary>
    /// URL to navigate to when action is clicked. Null if no action.
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Priority level (lower = more important, shown first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Category for grouping/styling.
    /// </summary>
    public PromptCategory Category { get; set; } = PromptCategory.Tip;
}

/// <summary>
/// Categories of prompts for styling and behavior.
/// </summary>
public enum PromptCategory
{
    /// <summary>
    /// Missing fundamental content - high priority.
    /// </summary>
    MissingFundamental = 1,

    /// <summary>
    /// Content needs attention (stale, incomplete).
    /// </summary>
    NeedsAttention = 2,

    /// <summary>
    /// Suggestion for engagement or feature discovery.
    /// </summary>
    Suggestion = 3,

    /// <summary>
    /// General tip or helpful information.
    /// </summary>
    Tip = 4
}
