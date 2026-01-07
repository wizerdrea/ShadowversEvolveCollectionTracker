using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ShadowversEvolveCardTracker.Converters
{
    /// <summary>
    /// Returns a left-to-right linear brush that is solid gold from 0..percentage and transparent for the remainder.
    /// Useful to visually "fill" a cell horizontally according to a percentage (0..100).
    /// </summary>
    public class PercentageToGoldBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            double pct = 0.0;
            try
            {
                if (value is double d) pct = d;
                else if (value is float f) pct = f;
                else if (value is int i) pct = i;
                else pct = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                pct = 0.0;
            }

            // Normalize to [0,1]
            double t = Math.Clamp(pct / 100.0, 0.0, 1.0);

            // Default gold color
            Color gold = Color.FromRgb(0xD4, 0xAF, 0x37);

            // Try to obtain GoldBrush color from app resources if available
            try
            {
                var app = Application.Current;
                if (app != null)
                {
                    var res = app.TryFindResource("GoldBrush");
                    if (res is SolidColorBrush sb)
                        gold = sb.Color;
                }
            }
            catch
            {
                // ignore and use default
            }

            // Create gradient: [0 .. t] = gold, [t .. 1] = transparent
            var lg = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };

            // If t <= 0, return fully transparent brush (shows cell background)
            if (t <= 0.0001)
            {
                var empty = new SolidColorBrush(Colors.Transparent);
                empty.Freeze();
                return empty;
            }

            // If t >= 0.9999 return solid gold
            if (t >= 0.9999)
            {
                var solid = new SolidColorBrush(gold);
                solid.Freeze();
                return solid;
            }

            // Add gold from 0 to t
            lg.GradientStops.Add(new GradientStop(gold, 0.0));
            lg.GradientStops.Add(new GradientStop(gold, t));

            // Immediately transition to transparent at t
            lg.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, gold.R, gold.G, gold.B), t));
            lg.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, gold.R, gold.G, gold.B), 1.0));

            lg.Freeze();
            return lg;
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}