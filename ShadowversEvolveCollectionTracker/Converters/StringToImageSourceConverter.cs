using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ShadowversEvolveCardTracker.Converters
{
    public class StringToImageSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                // Resolve relative paths like "./Data/..."
                string resolved = path;
                if (!Path.IsPathRooted(resolved))
                {
                    // Trim leading "./" or ".\"
                    if (resolved.StartsWith("./") || resolved.StartsWith(".\\"))
                        resolved = resolved[2..];

                    resolved = Path.Combine(AppContext.BaseDirectory, resolved);
                }

                if (!File.Exists(resolved))
                    return null;

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(resolved, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}