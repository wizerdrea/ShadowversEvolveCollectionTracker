using System;
using System.Globalization;
using System.Windows.Data;

namespace ShadowverseEvolveCardTracker.Converters
{
    public class ClassEqualityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string selectedClass && parameter is string className)
            {
                return string.Equals(selectedClass, className, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string className)
            {
                return className;
            }
            return Binding.DoNothing;
        }
    }
}