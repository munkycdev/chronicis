using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class QuestDrawerServiceTests : IDisposable
{
    private readonly IDrawerCoordinator _drawerCoordinator;
    private readonly QuestDrawerService _sut;

    public QuestDrawerServiceTests()
    {
        _drawerCoordinator = new DrawerCoordinator();
        _sut = new QuestDrawerService(_drawerCoordinator);
    }

    public void Dispose()
    {
        _sut?.Dispose();
        GC.SuppressFinalize(this);
    }

    // ════════════════════════════════════════════════════════════════
    //  Open/Close Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Open_WhenClosed_OpensDrawerAndRaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        _sut.OnOpen += () => eventRaised = true;

        // Act
        _sut.Open();

        // Assert
        Assert.True(_sut.IsOpen);
        Assert.True(eventRaised);
    }

    [Fact]
    public void Open_WhenAlreadyOpen_DoesNotRaiseEventAgain()
    {
        // Arrange
        var eventCount = 0;
        _sut.OnOpen += () => eventCount++;
        _sut.Open();

        // Act
        _sut.Open();

        // Assert
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void Close_WhenOpen_ClosesDrawerAndRaisesEvent()
    {
        // Arrange
        _sut.Open();
        var eventRaised = false;
        _sut.OnClose += () => eventRaised = true;

        // Act
        _sut.Close();

        // Assert
        Assert.False(_sut.IsOpen);
        Assert.True(eventRaised);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_DoesNotRaiseEvent()
    {
        // Arrange
        var eventRaised = false;
        _sut.OnClose += () => eventRaised = true;

        // Act
        _sut.Close();

        // Assert
        Assert.False(eventRaised);
    }

    // ════════════════════════════════════════════════════════════════
    //  Toggle Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Toggle_WhenClosed_OpensDrawer()
    {
        // Act
        _sut.Toggle();

        // Assert
        Assert.True(_sut.IsOpen);
    }

    [Fact]
    public void Toggle_WhenOpen_ClosesDrawer()
    {
        // Arrange
        _sut.Open();

        // Act
        _sut.Toggle();

        // Assert
        Assert.False(_sut.IsOpen);
    }

    [Fact]
    public void Toggle_CalledMultipleTimes_AlternatesState()
    {
        // Act & Assert
        _sut.Toggle();
        Assert.True(_sut.IsOpen);

        _sut.Toggle();
        Assert.False(_sut.IsOpen);

        _sut.Toggle();
        Assert.True(_sut.IsOpen);
    }

    // ════════════════════════════════════════════════════════════════
    //  Mutual Exclusivity Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void MetadataDrawerOpen_WhenQuestDrawerOpen_ClosesQuestDrawer()
    {
        // Arrange
        _sut.Open();
        var closeEventRaised = false;
        _sut.OnClose += () => closeEventRaised = true;

        // Act - Opening another drawer should close quests
        _drawerCoordinator.Open(DrawerType.Metadata);

        // Assert
        Assert.False(_sut.IsOpen);
        Assert.True(closeEventRaised);
    }

    [Fact]
    public void MetadataDrawerOpen_WhenQuestDrawerOpen_WithoutCloseSubscriber_StillCloses()
    {
        _sut.Open();

        _drawerCoordinator.Open(DrawerType.Metadata);

        Assert.False(_sut.IsOpen);
    }

    [Fact]
    public void MetadataDrawerOpen_WhenQuestDrawerClosed_DoesNothing()
    {
        // Arrange
        var closeEventRaised = false;
        _sut.OnClose += () => closeEventRaised = true;

        // Act - Opening metadata while quests are closed should not raise quest close
        _drawerCoordinator.Open(DrawerType.Metadata);

        // Assert
        Assert.False(_sut.IsOpen);
        Assert.False(closeEventRaised);
    }

    // ════════════════════════════════════════════════════════════════
    //  Disposal Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_UnsubscribesFromDrawerCoordinatorEvents()
    {
        // Arrange
        _sut.Open();

        // Act
        _sut.Dispose();

        // This test verifies that after disposal, the quest drawer doesn't respond
        // to coordinator state changes (it has unsubscribed)
        var closeEventRaised = false;
        _sut.OnClose += () => closeEventRaised = true;

        _drawerCoordinator.Open(DrawerType.Metadata);

        // Assert
        Assert.False(closeEventRaised);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _sut.Dispose();
        _sut.Dispose();
        _sut.Dispose();
    }

    [Fact]
    public void Open_AfterDispose_DoesNothing()
    {
        // Arrange
        _sut.Dispose();
        var eventRaised = false;
        _sut.OnOpen += () => eventRaised = true;

        // Act
        _sut.Open();

        // Assert
        Assert.False(_sut.IsOpen);
        Assert.False(eventRaised);
    }

    [Fact]
    public void Close_AfterDispose_DoesNothing()
    {
        // Arrange
        _sut.Open();
        _sut.Dispose();
        var eventRaised = false;
        _sut.OnClose += () => eventRaised = true;

        // Act
        _sut.Close();

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void Toggle_AfterDispose_DoesNothing()
    {
        // Arrange
        _sut.Dispose();

        // Act
        _sut.Toggle();

        // Assert
        Assert.False(_sut.IsOpen);
    }

    // ════════════════════════════════════════════════════════════════
    //  Event Lifecycle Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void IsOpen_InitiallyFalse()
    {
        // Assert
        Assert.False(_sut.IsOpen);
    }

    [Fact]
    public void EventHandlers_ClearedOnDispose()
    {
        // Arrange
        var openCount = 0;
        var closeCount = 0;
        _sut.OnOpen += () => openCount++;
        _sut.OnClose += () => closeCount++;

        // Act
        _sut.Dispose();
        _sut.Open(); // Should not raise events after dispose
        _sut.Close();

        // Assert
        Assert.Equal(0, openCount);
        Assert.Equal(0, closeCount);
    }
}
