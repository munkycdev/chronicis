namespace Chronicis.Client.Utilities;

/// <summary>
/// Models the undo/redo stroke stack behavior of the drawing canvas.
/// Strokes are stored in a list; redo items in a separate stack.
/// Undo: pop last stroke from strokes → push to redoStack.
/// Redo: pop from redoStack → push to strokes.
/// </summary>
public static class DrawingCanvasUndoRedo
{
    /// <summary>
    /// Performs an undo operation: removes the last stroke and returns it as a redo candidate.
    /// Returns null if strokes is empty.
    /// </summary>
    public static T? Undo<T>(List<T> strokes, Stack<T> redoStack)
    {
        if (strokes.Count == 0) return default;
        var stroke = strokes[^1];
        strokes.RemoveAt(strokes.Count - 1);
        redoStack.Push(stroke);
        return stroke;
    }

    /// <summary>
    /// Performs a redo operation: pops from redo stack and appends to strokes.
    /// Returns null if redoStack is empty.
    /// </summary>
    public static T? Redo<T>(List<T> strokes, Stack<T> redoStack)
    {
        if (redoStack.Count == 0) return default;
        var stroke = redoStack.Pop();
        strokes.Add(stroke);
        return stroke;
    }

    /// <summary>
    /// Adds a stroke and clears the redo stack (new input invalidates redo history).
    /// </summary>
    public static void AddStroke<T>(List<T> strokes, Stack<T> redoStack, T stroke)
    {
        strokes.Add(stroke);
        redoStack.Clear();
    }
}
