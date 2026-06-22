// Feature: handwritten-session-notes, Property 2: Pressure-to-Width Mapping
using Chronicis.Client.Utilities;
using FsCheck;
using Xunit;

namespace Chronicis.Client.Tests.Utilities;

/// <summary>
/// Property 2: Pressure-to-Width Mapping
/// For any pressure value in [0.0, 1.0], the computed stroke width SHALL be in [1, 8] CSS pixels,
/// and the mapping SHALL be monotonically non-decreasing (higher pressure never produces a thinner stroke).
///
/// Validates: Requirements 2.2
/// </summary>
public class PressureToWidthPropertyTests
{
    private static Arbitrary<double> PressureInRange()
        => Arb.From(Gen.Choose(1, 10000).Select(i => i / 10000.0));

    [Fact]
    public void Width_Is_Within_Valid_Range_For_Any_Pressure()
    {
        // **Validates: Requirements 2.2**
        Prop.ForAll(PressureInRange(), pressure =>
        {
            var width = PressureToWidth.Compute(pressure);
            return (width >= 1.0 && width <= 8.0)
                .Label($"pressure={pressure}, width={width} should be in [1,8]");
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Mapping_Is_Monotonically_NonDecreasing()
    {
        // **Validates: Requirements 2.2**
        Prop.ForAll(PressureInRange(), PressureInRange(), (p1, p2) =>
        {
            var low = Math.Min(p1, p2);
            var high = Math.Max(p1, p2);

            var widthLow = PressureToWidth.Compute(low);
            var widthHigh = PressureToWidth.Compute(high);

            return (widthHigh >= widthLow)
                .Label($"p_low={low} -> w={widthLow}, p_high={high} -> w={widthHigh}: higher pressure must not produce thinner stroke");
        }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void Zero_Pressure_Returns_Default_Width()
    {
        // **Validates: Requirements 2.2**
        var width = PressureToWidth.Compute(0.0);
        Assert.Equal(2.0, width);
    }

    [Fact]
    public void Null_Pressure_Returns_Default_Width()
    {
        // **Validates: Requirements 2.2**
        var width = PressureToWidth.Compute(null);
        Assert.Equal(2.0, width);
    }
}
