namespace Chronicis.Client.Utilities;

/// <summary>
/// Maps stylus pressure values to stroke widths for the drawing canvas.
/// Mirrors the JavaScript pressureToWidth function in drawingCanvas.js.
/// </summary>
internal static class PressureToWidth
{
    private const double MinWidth = 1.0;
    private const double MaxWidth = 8.0;
    private const double DefaultWidth = 2.0;

    /// <summary>
    /// Computes the stroke width from a pressure value.
    /// For pressure in (0.0, 1.0], returns clamp(pressure * 8, 1, 8).
    /// For pressure == 0 or absent (null), returns 2px default.
    /// </summary>
    public static double Compute(double? pressure)
    {
        if (pressure is null or 0.0)
            return DefaultWidth;

        return Math.Clamp(pressure.Value * 8.0, MinWidth, MaxWidth);
    }
}
