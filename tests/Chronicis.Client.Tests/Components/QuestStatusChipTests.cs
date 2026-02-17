using Bunit;
using Chronicis.Client.Components.Quests;
using Chronicis.Shared.Enums;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the QuestStatusChip component.
/// This component displays a colored chip based on quest status.
/// </summary>
public class QuestStatusChipTests : MudBlazorTestContext
{
    [Fact]
    public void QuestStatusChip_WithActiveStatus_ShowsGreenChip()
    {
        // Act
        var cut = RenderComponent<QuestStatusChip>(parameters => parameters
            .Add(p => p.Status, QuestStatus.Active));

        // Assert
        Assert.Contains("Active", cut.Markup);
        var chip = cut.FindComponent<MudChip<string>>();
        Assert.NotNull(chip);
        Assert.Equal(Color.Success, chip.Instance.Color);
    }

    [Fact]
    public void QuestStatusChip_WithCompletedStatus_ShowsBlueChip()
    {
        // Act
        var cut = RenderComponent<QuestStatusChip>(parameters => parameters
            .Add(p => p.Status, QuestStatus.Completed));

        // Assert
        Assert.Contains("Completed", cut.Markup);
        var chip = cut.FindComponent<MudChip<string>>();
        Assert.Equal(Color.Info, chip.Instance.Color);
    }

    [Fact]
    public void QuestStatusChip_WithFailedStatus_ShowsRedChip()
    {
        // Act
        var cut = RenderComponent<QuestStatusChip>(parameters => parameters
            .Add(p => p.Status, QuestStatus.Failed));

        // Assert
        Assert.Contains("Failed", cut.Markup);
        var chip = cut.FindComponent<MudChip<string>>();
        Assert.Equal(Color.Error, chip.Instance.Color);
    }

    [Fact]
    public void QuestStatusChip_WithAbandonedStatus_ShowsDefaultChip()
    {
        // Act
        var cut = RenderComponent<QuestStatusChip>(parameters => parameters
            .Add(p => p.Status, QuestStatus.Abandoned));

        // Assert
        Assert.Contains("Abandoned", cut.Markup);
        var chip = cut.FindComponent<MudChip<string>>();
        Assert.Equal(Color.Default, chip.Instance.Color);
    }

    [Theory]
    [InlineData(QuestStatus.Active, "Active", Color.Success)]
    [InlineData(QuestStatus.Completed, "Completed", Color.Info)]
    [InlineData(QuestStatus.Failed, "Failed", Color.Error)]
    [InlineData(QuestStatus.Abandoned, "Abandoned", Color.Default)]
    public void QuestStatusChip_RendersCorrectTextAndColor(QuestStatus status, string expectedText, Color expectedColor)
    {
        // Act
        var cut = RenderComponent<QuestStatusChip>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        Assert.Contains(expectedText, cut.Markup);
        var chip = cut.FindComponent<MudChip<string>>();
        Assert.Equal(expectedColor, chip.Instance.Color);
    }

    [Fact]
    public void QuestStatusChip_UsesSmallSize()
    {
        // Act
        var cut = RenderComponent<QuestStatusChip>(parameters => parameters
            .Add(p => p.Status, QuestStatus.Active));

        // Assert
        var chip = cut.FindComponent<MudChip<string>>();
        Assert.Equal(Size.Small, chip.Instance.Size);
    }

    [Fact]
    public void QuestStatusChip_UsesTextVariant()
    {
        // Act
        var cut = RenderComponent<QuestStatusChip>(parameters => parameters
            .Add(p => p.Status, QuestStatus.Active));

        // Assert
        var chip = cut.FindComponent<MudChip<string>>();
        Assert.Equal(Variant.Text, chip.Instance.Variant);
    }
}
