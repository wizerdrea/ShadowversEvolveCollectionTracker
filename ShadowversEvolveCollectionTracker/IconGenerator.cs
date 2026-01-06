using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShadowversEvolveCardTracker
{
    /// <summary>
    /// Utility to generate the app icon from vector graphics.
    /// Run this once to create the icon file, then remove or comment out.
    /// </summary>
    public static class IconGenerator
    {
        public static void GenerateAppIcon()
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");

            try
            {
                // Create a DrawingVisual with the card-to-smoke design
                var drawing = CreateIconDrawing(256, 256);

                // Render at highest resolution
                var renderBitmap = new RenderTargetBitmap(256, 256, 96, 96, PixelFormats.Pbgra32);
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    context.DrawDrawing(drawing);
                }
                renderBitmap.Render(visual);

                // Save as PNG first (ICO conversion would require additional library)
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                
                using (var stream = File.Create(iconPath.Replace(".ico", ".png")))
                {
                    encoder.Save(stream);
                }

                MessageBox.Show($"Icon PNG generated at: {iconPath.Replace(".ico", ".png")}\n\n" +
                               "Convert this to ICO using an online tool or image editor, then set it in the project properties.",
                               "Icon Generator", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating icon: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Drawing CreateIconDrawing(double width, double height)
        {
            var drawingGroup = new DrawingGroup();

            // Background (transparent)
            using (var dc = drawingGroup.Open())
            {
                // Card base with gradient
                var cardRect = new Rect(width * 0.2, height * 0.08, width * 0.6, height * 0.84);
                var gradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0.5, 0),
                    EndPoint = new Point(0.5, 1)
                };
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x2D, 0xD4, 0xBF), 0));      // ColorAccent
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x5E, 0xEA, 0xD4), 0.3));   // ColorAccentLight
                gradient.GradientStops.Add(new GradientStop(Color.FromArgb(0x40, 0x2D, 0xD4, 0xBF), 0.7));
                gradient.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x2D, 0xD4, 0xBF), 1));

                dc.DrawRoundedRectangle(gradient, null, cardRect, width * 0.02, height * 0.02);

                // Smoke particles at bottom
                var smokeBrush1 = new SolidColorBrush(Color.FromArgb(0x99, 0x5E, 0xEA, 0xD4));
                var smokeBrush2 = new SolidColorBrush(Color.FromArgb(0x66, 0x5E, 0xEA, 0xD4));
                var smokeBrush3 = new SolidColorBrush(Color.FromArgb(0x4D, 0x5E, 0xEA, 0xD4));

                dc.DrawEllipse(smokeBrush1, null, new Point(width * 0.28, height * 0.88), width * 0.05, height * 0.05);
                dc.DrawEllipse(smokeBrush2, null, new Point(width * 0.43, height * 0.92), width * 0.07, height * 0.07);
                dc.DrawEllipse(smokeBrush1, null, new Point(width * 0.6, height * 0.88), width * 0.05, height * 0.05);
                dc.DrawEllipse(smokeBrush3, null, new Point(width * 0.35, height * 0.95), width * 0.04, height * 0.04);
                dc.DrawEllipse(smokeBrush3, null, new Point(width * 0.63, height * 0.95), width * 0.04, height * 0.04);

                // Smoke particles at top (gold)
                var goldSmoke1 = new SolidColorBrush(Color.FromArgb(0x80, 0xD4, 0xAF, 0x37));
                var goldSmoke2 = new SolidColorBrush(Color.FromArgb(0x66, 0xD4, 0xAF, 0x37));

                dc.DrawEllipse(goldSmoke1, null, new Point(width * 0.41, height * 0.94), width * 0.04, height * 0.04);
                dc.DrawEllipse(goldSmoke2, null, new Point(width * 0.53, height * 0.97), width * 0.05, height * 0.05);
                dc.DrawEllipse(goldSmoke1, null, new Point(width * 0.72, height * 0.94), width * 0.04, height * 0.04);
            }

            return drawingGroup;
        }
    }
}