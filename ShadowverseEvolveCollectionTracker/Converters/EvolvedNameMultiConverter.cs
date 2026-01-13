using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ShadowverseEvolveCardTracker.Converters
{
    /// <summary>
    /// Produces a display name that appends " (Evolved)" when the card Type contains "Evolved".
    /// Used as a MultiBinding converter receiving Name and Type.
    /// </summary>
    public sealed class EvolvedNameMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            var name = values?.ElementAtOrDefault(0)?.ToString() ?? string.Empty;
            var type = values?.ElementAtOrDefault(1)?.ToString() ?? string.Empty;

            if (!string.IsNullOrEmpty(type) && type.IndexOf("Evolved", StringComparison.OrdinalIgnoreCase) >= 0)
                return $"{name} (Evolved)";

            return name;
        }

        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}