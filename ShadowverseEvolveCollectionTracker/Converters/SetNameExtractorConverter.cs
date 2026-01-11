using ShadowverseEvolveCardTracker.Utilities;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ShadowverseEvolveCardTracker.Converters
{
    /// <summary>
    /// Extracts the first substring found between double-quotes (straight or curly) in the input string.
    /// If no quotes are present the original trimmed string is returned.
    /// Designed for showing the short set identifier contained in CardData.Set.
    /// </summary>
    public class SetNameExtractorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                var s = value as string;
                return SetHelper.ExtractSetName(s);
            }
            catch
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}