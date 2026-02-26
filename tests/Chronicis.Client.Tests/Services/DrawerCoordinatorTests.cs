using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class DrawerCoordinatorTests
{
    [Fact]
    public void IsForcedOpen_SetSameValue_DoesNotRaiseChanged()
    {
        var sut = new DrawerCoordinator();
        var changed = 0;
        sut.OnChanged += () => changed++;

        sut.IsForcedOpen = false;

        Assert.Equal(0, changed);
    }

    [Fact]
    public void IsForcedOpen_WhenEnabled_ForcesTutorialAndRaisesChanged()
    {
        var sut = new DrawerCoordinator();
        var changed = 0;
        sut.Open(DrawerType.Metadata);
        sut.OnChanged += () => changed++;

        sut.IsForcedOpen = true;

        Assert.True(sut.IsForcedOpen);
        Assert.Equal(DrawerType.Tutorial, sut.Current);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void IsForcedOpen_WhenDisabledAfterEnabled_RaisesChangedWithoutChangingCurrentDrawer()
    {
        var sut = new DrawerCoordinator();
        sut.Open(DrawerType.Tutorial);
        sut.IsForcedOpen = true;
        var changed = 0;
        sut.OnChanged += () => changed++;

        sut.IsForcedOpen = false;

        Assert.False(sut.IsForcedOpen);
        Assert.Equal(DrawerType.Tutorial, sut.Current);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void Open_None_DelegatesToClose()
    {
        var sut = new DrawerCoordinator();
        sut.Open(DrawerType.Metadata);

        sut.Open(DrawerType.None);

        Assert.Equal(DrawerType.None, sut.Current);
    }

    [Fact]
    public void Open_WhenForcedAndNonTutorial_OpensTutorial()
    {
        var sut = new DrawerCoordinator { IsForcedOpen = true };

        sut.Open(DrawerType.Metadata);

        Assert.Equal(DrawerType.Tutorial, sut.Current);
    }

    [Fact]
    public void Open_WhenSameDrawer_DoesNotRaiseChanged()
    {
        var sut = new DrawerCoordinator();
        var changed = 0;
        sut.Open(DrawerType.Metadata);
        sut.OnChanged += () => changed++;

        sut.Open(DrawerType.Metadata);

        Assert.Equal(0, changed);
    }

    [Fact]
    public void Close_WhenForcedTutorial_DoesNothing()
    {
        var sut = new DrawerCoordinator();
        sut.Open(DrawerType.Tutorial);
        sut.IsForcedOpen = true;

        sut.Close();

        Assert.Equal(DrawerType.Tutorial, sut.Current);
    }

    [Fact]
    public void Close_WhenAlreadyNone_DoesNothing()
    {
        var sut = new DrawerCoordinator();
        var changed = 0;
        sut.OnChanged += () => changed++;

        sut.Close();

        Assert.Equal(DrawerType.None, sut.Current);
        Assert.Equal(0, changed);
    }

    [Fact]
    public void Toggle_None_Closes()
    {
        var sut = new DrawerCoordinator();
        sut.Open(DrawerType.Quests);

        sut.Toggle(DrawerType.None);

        Assert.Equal(DrawerType.None, sut.Current);
    }

    [Fact]
    public void Toggle_WhenForcedAndNonTutorial_OpensTutorial()
    {
        var sut = new DrawerCoordinator { IsForcedOpen = true };

        sut.Toggle(DrawerType.Metadata);

        Assert.Equal(DrawerType.Tutorial, sut.Current);
    }

    [Fact]
    public void Toggle_SameDrawer_WhenForcedTutorial_DoesNotClose()
    {
        var sut = new DrawerCoordinator();
        sut.Open(DrawerType.Tutorial);
        sut.IsForcedOpen = true;

        sut.Toggle(DrawerType.Tutorial);

        Assert.Equal(DrawerType.Tutorial, sut.Current);
    }

    [Fact]
    public void Toggle_SameDrawer_WhenNotForced_Closes()
    {
        var sut = new DrawerCoordinator();
        sut.Open(DrawerType.Metadata);

        sut.Toggle(DrawerType.Metadata);

        Assert.Equal(DrawerType.None, sut.Current);
    }

    [Fact]
    public void Toggle_DifferentDrawer_OpensRequestedDrawer()
    {
        var sut = new DrawerCoordinator();

        sut.Toggle(DrawerType.Quests);

        Assert.Equal(DrawerType.Quests, sut.Current);
    }
}
