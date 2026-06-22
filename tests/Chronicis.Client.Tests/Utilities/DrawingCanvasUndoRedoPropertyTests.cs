// Feature: handwritten-session-notes, Property 3: Undo/Redo Round-Trip
using Chronicis.Client.Utilities;
using FsCheck;
using Xunit;

namespace Chronicis.Client.Tests.Utilities;

/// <summary>
/// Property 3: Undo/Redo Round-Trip
/// For any non-empty sequence of strokes on the canvas, performing an undo followed by a redo
/// SHALL return the stroke list to the same state as before the undo was performed.
///
/// Validates: Requirements 2.6
/// </summary>
public class DrawingCanvasUndoRedoPropertyTests
{
    [Fact]
    public void Undo_Then_Redo_Returns_Stroke_List_To_Same_State()
    {
        // **Validates: Requirements 2.6**
        Prop.ForAll(Arb.From<NonEmptyArray<int>>(), strokes =>
        {
            var strokeList = new List<int>();
            var redoStack = new Stack<int>();
            foreach (var stroke in strokes.Get)
            {
                DrawingCanvasUndoRedo.AddStroke(strokeList, redoStack, stroke);
            }

            var beforeUndo = strokeList.ToList();

            DrawingCanvasUndoRedo.Undo(strokeList, redoStack);
            DrawingCanvasUndoRedo.Redo(strokeList, redoStack);

            return beforeUndo.SequenceEqual(strokeList);
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Multiple_Undo_Redo_Pairs_Preserve_State()
    {
        // **Validates: Requirements 2.6**
        var strokeListGen = Gen.ListOf(Gen.Choose(1, 1000))
            .Where(l => l.Count() >= 2)
            .Select(l => l.ToArray());

        Prop.ForAll(Arb.From(strokeListGen), Gen.Choose(1, 5).ToArbitrary(), (strokes, undoCount) =>
        {
            var strokeList = new List<int>();
            var redoStack = new Stack<int>();
            foreach (var stroke in strokes)
            {
                DrawingCanvasUndoRedo.AddStroke(strokeList, redoStack, stroke);
            }

            var actualUndos = Math.Min(undoCount, strokes.Length);
            var beforeUndo = strokeList.ToList();

            for (int i = 0; i < actualUndos; i++)
                DrawingCanvasUndoRedo.Undo(strokeList, redoStack);
            for (int i = 0; i < actualUndos; i++)
                DrawingCanvasUndoRedo.Redo(strokeList, redoStack);

            return beforeUndo.SequenceEqual(strokeList);
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Undo_On_Empty_Is_Noop()
    {
        // **Validates: Requirements 2.6**
        var strokeList = new List<int>();
        var redoStack = new Stack<int>();
        DrawingCanvasUndoRedo.Undo(strokeList, redoStack);
        Assert.Empty(strokeList);
        Assert.Empty(redoStack);
    }

    [Fact]
    public void Redo_On_Empty_Is_Noop()
    {
        // **Validates: Requirements 2.6**
        var strokeList = new List<int>();
        var redoStack = new Stack<int>();
        DrawingCanvasUndoRedo.Redo(strokeList, redoStack);
        Assert.Empty(strokeList);
    }

    [Fact]
    public void AddStroke_Clears_RedoStack()
    {
        // **Validates: Requirements 2.6**
        Prop.ForAll(Arb.From<NonEmptyArray<int>>(), Arb.From<int>(), (strokes, newStroke) =>
        {
            var strokeList = new List<int>();
            var redoStack = new Stack<int>();
            foreach (var stroke in strokes.Get)
            {
                DrawingCanvasUndoRedo.AddStroke(strokeList, redoStack, stroke);
            }

            DrawingCanvasUndoRedo.Undo(strokeList, redoStack);
            var redoCountBefore = redoStack.Count;

            DrawingCanvasUndoRedo.AddStroke(strokeList, redoStack, newStroke);

            return redoCountBefore > 0 && redoStack.Count == 0;
        }).QuickCheckThrowOnFailure();
    }
}
