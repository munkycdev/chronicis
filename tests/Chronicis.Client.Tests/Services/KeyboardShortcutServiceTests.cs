using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class KeyboardShortcutServiceTests
{
    [Fact]
    public void RequestSave_RaisesOnSaveRequestedEvent()
    {
        // Arrange
        var sut = new KeyboardShortcutService();
        var eventRaised = false;
        sut.OnSaveRequested += () => eventRaised = true;

        // Act
        sut.RequestSave();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void RequestSave_WithNoSubscribers_DoesNotThrow()
    {
        // Arrange
        var sut = new KeyboardShortcutService();

        // Act & Assert - Should not throw
        sut.RequestSave();
    }

    [Fact]
    public void RequestSave_CalledMultipleTimes_RaisesEventEachTime()
    {
        // Arrange
        var sut = new KeyboardShortcutService();
        var eventCount = 0;
        sut.OnSaveRequested += () => eventCount++;

        // Act
        sut.RequestSave();
        sut.RequestSave();
        sut.RequestSave();

        // Assert
        Assert.Equal(3, eventCount);
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var sut = new KeyboardShortcutService();
        var subscriber1Raised = false;
        var subscriber2Raised = false;
        var subscriber3Raised = false;

        sut.OnSaveRequested += () => subscriber1Raised = true;
        sut.OnSaveRequested += () => subscriber2Raised = true;
        sut.OnSaveRequested += () => subscriber3Raised = true;

        // Act
        sut.RequestSave();

        // Assert
        Assert.True(subscriber1Raised);
        Assert.True(subscriber2Raised);
        Assert.True(subscriber3Raised);
    }
}
