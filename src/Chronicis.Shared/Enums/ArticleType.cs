namespace Chronicis.Shared.Enums;

/// <summary>
/// Defines the type of article, which determines behavior and where it appears in the tree.
/// </summary>
public enum ArticleType
{
    // ===== World-Scoped Content =====

    /// <summary>
    /// General wiki content: locations, NPCs, items, lore, etc.
    /// Can be nested infinitely under other WikiArticles.
    /// </summary>
    WikiArticle = 1,

    /// <summary>
    /// A player or NPC character (top-level in Characters group).
    /// </summary>
    Character = 2,

    /// <summary>
    /// Notes nested under a Character (backstory, development, etc.).
    /// </summary>
    CharacterNote = 3,

    // ===== Campaign/Arc-Scoped Content =====

    /// <summary>
    /// A game session article containing the DM's canonical notes.
    /// Must belong to an Arc.
    /// </summary>
    Session = 10,

    /// <summary>
    /// Player or additional notes nested under a Session.
    /// </summary>
    SessionNote = 11,

    // ===== Migration/Legacy =====

    /// <summary>
    /// Articles migrated from old schema that haven't been categorized yet.
    /// Users can change these to appropriate types via the UI.
    /// </summary>
    Legacy = 99
}
