using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

[ExcludeFromCodeCoverage]
public class MetadataDrawerServiceTests
{
    [Fact]
    public void Toggle_RaisesOnToggleEvent()
    {
        // Arrange
        var sut = new MetadataDrawerService(new DrawerCoordinator());
        var eventRaised = false;
        sut.OnToggle += () => eventRaised = true;

        // Act
        sut.Toggle();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void Toggle_WithNoSubscribers_DoesNotThrow()
    {
        // Arrange
        var sut = new MetadataDrawerService(new DrawerCoordinator());

        // Act & Assert - Should not throw
        sut.Toggle();
    }

    [Fact]
    public void Toggle_CalledMultipleTimes_RaisesEventEachTime()
    {
        // Arrange
        var sut = new MetadataDrawerService(new DrawerCoordinator());
        var eventCount = 0;
        sut.OnToggle += () => eventCount++;

        // Act
        sut.Toggle();
        sut.Toggle();
        sut.Toggle();

        // Assert
        Assert.Equal(3, eventCount);
    }
}
