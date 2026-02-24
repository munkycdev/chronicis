using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Models;

/// <summary>
/// Represents a game session within an Arc.
/// Sessions are first-class entities (not Articles) that contain PublicNotes and PrivateNotes
/// authored by the GM, and act as containers for SessionNote articles contributed by players.
/// </summary>
[ExcludeFromCodeCoverage]
public class Session
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The arc this session belongs to.
    /// </summary>
    public Guid ArcId { get; set; }

    /// <summary>
    /// Display name of the session (e.g., "Session 1 — The Dark Forest").
    /// Max 500 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Real-world date when the session was played. Optional.
    /// </summary>
    public DateTime? SessionDate { get; set; }

    /// <summary>
    /// GM-authored notes that are safe to expose publicly (HTML from TipTap editor).
    /// Included in AI summary generation.
    /// </summary>
    public string? PublicNotes { get; set; }

    /// <summary>
    /// GM-authored notes that must never be exposed to players or included in AI summaries (HTML from TipTap editor).
    /// Excluded from all public endpoints and AI summary generation. Always.
    /// </summary>
    public string? PrivateNotes { get; set; }

    /// <summary>
    /// AI-generated summary of the session derived exclusively from public sources.
    /// Built from PublicNotes and Public-visibility SessionNote articles only.
    /// </summary>
    public string? AiSummary { get; set; }

    /// <summary>
    /// When the AI summary was last generated.
    /// </summary>
    public DateTime? AiSummaryGeneratedAt { get; set; }

    /// <summary>
    /// User who triggered the AI summary generation. Optional.
    /// </summary>
    public Guid? AiSummaryGeneratedByUserId { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the session was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who created this session.
    /// </summary>
    public Guid CreatedBy { get; set; }

    // ===== Navigation Properties =====

    /// <summary>
    /// The arc this session belongs to.
    /// </summary>
    public Arc Arc { get; set; } = null!;

    /// <summary>
    /// User who created this session.
    /// </summary>
    public User Creator { get; set; } = null!;

    /// <summary>
    /// User who triggered AI summary generation (if recorded).
    /// </summary>
    public User? AiSummaryGeneratedBy { get; set; }

    /// <summary>
    /// SessionNote articles attached to this session via Article.SessionId.
    /// </summary>
    public ICollection<Article> SessionNotes { get; set; } = new List<Article>();

    /// <summary>
    /// Quest updates that reference this session entity.
    /// </summary>
    public ICollection<QuestUpdate> QuestUpdates { get; set; } = new List<QuestUpdate>();
}
