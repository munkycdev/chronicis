// Feature: handwritten-session-notes, Property 1: View State Determination
using Chronicis.Client.Utilities;
using FsCheck;
using Xunit;

namespace Chronicis.Client.Tests.Utilities;

/// <summary>
/// Property 1: View State Determination
/// For any session note article, the page view state is determined solely by the presence of
/// HandwrittenNoteImageId: when non-null the Tab_UI is displayed, when null the
/// "Add a handwritten note" button is displayed. These two states are mutually exclusive.
///
/// Validates: Requirements 1.1, 1.3, 5.1
/// </summary>
public class HandwrittenNoteViewStatePropertyTests
{
    [Fact]
    public void NonNull_HandwrittenNoteImageId_Shows_TabUi()
    {
        // **Validates: Requirements 1.1, 1.3, 5.1**
        Prop.ForAll<Guid>(guid =>
        {
            Guid? id = guid;
            return HandwrittenNoteViewState.ShouldShowTabUi(id);
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Null_HandwrittenNoteImageId_Shows_AddButton()
    {
        // **Validates: Requirements 1.1, 1.3, 5.1**
        var result = HandwrittenNoteViewState.ShouldShowAddButton(null);
        Assert.True(result);
    }

    [Fact]
    public void NonNull_HandwrittenNoteImageId_Hides_AddButton()
    {
        // **Validates: Requirements 1.1, 1.3, 5.1**
        Prop.ForAll<Guid>(guid =>
        {
            Guid? id = guid;
            return !HandwrittenNoteViewState.ShouldShowAddButton(id);
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Null_HandwrittenNoteImageId_Hides_TabUi()
    {
        // **Validates: Requirements 1.1, 1.3, 5.1**
        var result = HandwrittenNoteViewState.ShouldShowTabUi(null);
        Assert.False(result);
    }

    [Fact]
    public void ViewStates_Are_Mutually_Exclusive_For_Any_Guid()
    {
        // **Validates: Requirements 1.1, 1.3, 5.1**
        Prop.ForAll(Arb.From<Guid?>(), id =>
        {
            var showTab = HandwrittenNoteViewState.ShouldShowTabUi(id);
            var showButton = HandwrittenNoteViewState.ShouldShowAddButton(id);

            // Exactly one must be true — mutually exclusive and exhaustive
            return showTab != showButton;
        }).QuickCheckThrowOnFailure();
    }
}
