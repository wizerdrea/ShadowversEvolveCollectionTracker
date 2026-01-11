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
                if (string.IsNullOrWhiteSpace(s))
                    return string.Empty;

                // Accept straight (") and curly (“ ”) double quotes as delimiters
                char[] quoteChars = new[] { '"', '“', '”' };

                int first = s.IndexOfAny(quoteChars);
                if (first < 0)
                    return s.Trim();

                int second = s.IndexOfAny(quoteChars, first + 1);
                if (second <= first)
                    return s.Trim();

                return s.Substring(first + 1, second - first - 1).Trim();
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