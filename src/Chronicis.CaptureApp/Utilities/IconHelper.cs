using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Chronicis.CaptureApp.Utilities;

public static class IconHelper
{
    public static Icon CreateDragonIcon(bool isRecording)
    {
        // Create a 32x32 bitmap for the icon
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Draw dragon silhouette (simplified)
            var dragonColor = Color.FromArgb(196, 175, 142); // Beige-Gold
            using (var brush = new SolidBrush(dragonColor))
            {
                // Dragon head (simplified)
                g.FillEllipse(brush, 8, 8, 16, 16);

                // Dragon horn
                var hornPoints = new Point[]
                {
                    new Point(16, 8),
                    new Point(14, 2),
                    new Point(18, 6)
                };
                g.FillPolygon(brush, hornPoints);
            }

            // Add recording indicator (red dot)
            if (isRecording)
            {
                using (var redBrush = new SolidBrush(Color.Red))
                {
                    g.FillEllipse(redBrush, 20, 20, 10, 10);
                }
            }
        }

        // Convert bitmap to icon
        IntPtr hIcon = bitmap.GetHicon();
        Icon icon = Icon.FromHandle(hIcon);

        return icon;
    }
}