namespace Chronicis.Client.Utilities;

/// <summary>
/// Determines the view state for the handwritten note section of a session note page.
/// </summary>
internal static class HandwrittenNoteViewState
{
    /// <summary>
    /// Returns true when the Tab_UI should be displayed (handwritten note exists).
    /// </summary>
    public static bool ShouldShowTabUi(Guid? handwrittenNoteImageId)
        => handwrittenNoteImageId.HasValue;

    /// <summary>
    /// Returns true when the "Add a handwritten note" button should be displayed (no handwritten note).
    /// </summary>
    public static bool ShouldShowAddButton(Guid? handwrittenNoteImageId)
        => !handwrittenNoteImageId.HasValue;
}
