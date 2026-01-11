using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ShadowverseEvolveCardTracker.Converters
{
    /// <summary>
    /// Converts a class name (e.g. "Forestcraft") to a BitmapImage using the pack URI:
    /// pack://application:,,,/ShadowverseEvolveCardTracker;component/Data/ClassIcons/{lowercase-class}.png
    /// Returns null if the input is null/empty or image load fails.
    /// </summary>
    public class ClassToPackImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string className || string.IsNullOrWhiteSpace(className))
                return null;

            var fileName = className.Trim().ToLowerInvariant() + ".png";
            var uriString = $"pack://application:,,,/ShadowverseEvolveCardTracker;component/Data/ClassIcons/{fileName}";

            try
            {
                var uri = new Uri(uriString, UriKind.Absolute);
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = uri;
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}