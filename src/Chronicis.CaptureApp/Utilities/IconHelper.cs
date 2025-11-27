using System.Drawing;
using System.Drawing.Drawing2D;

namespace Chronicis.CaptureApp.Utilities;

public static class IconHelper
{
    public static Icon CreateChronicisIcon(bool isRecording)
    {
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chronicis.ico");

        if (!File.Exists(iconPath))
        {
            // Fallback: create a simple icon if file not found
            return CreateFallbackIcon(isRecording);
        }

        if (!isRecording)
        {
            // Not recording - just use the base icon
            return new Icon(iconPath);
        }

        // Recording - add red dot overlay
        using var baseIcon = new Icon(iconPath, 32, 32);
        using var bitmap = baseIcon.ToBitmap();
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw red recording indicator dot in bottom-right
        using (var redBrush = new SolidBrush(Color.Red))
        {
            g.FillEllipse(redBrush, 20, 20, 12, 12);
        }

        // Convert bitmap back to icon
        IntPtr hIcon = bitmap.GetHicon();
        Icon icon = Icon.FromHandle(hIcon);

        return icon;
    }

    private static Icon CreateFallbackIcon(bool isRecording)
    {
        // Simple fallback if chronicis.ico not found
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Draw simple dragon head
            var dragonColor = Color.FromArgb(196, 175, 142);
            using (var brush = new SolidBrush(dragonColor))
            {
                g.FillEllipse(brush, 8, 8, 16, 16);

                var hornPoints = new Point[]
                {
                    new Point(16, 8),
                    new Point(14, 2),
                    new Point(18, 6)
                };
                g.FillPolygon(brush, hornPoints);
            }

            if (isRecording)
            {
                using (var redBrush = new SolidBrush(Color.Red))
                {
                    g.FillEllipse(redBrush, 20, 20, 10, 10);
                }
            }
        }

        IntPtr hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }
}