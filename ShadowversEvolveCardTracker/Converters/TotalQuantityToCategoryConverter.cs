using System;
using System.Globalization;
using System.Windows.Data;

namespace ShadowversEvolveCardTracker.Converters
{
    // Returns "None", "Low" (1-2) or "High" (>=3)
    public class TotalQuantityToCategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var v = System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
                if (v <= 0) return "None";
                if (v < 3) return "Low";
                return "High";
            }
            catch
            {
                return "None";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}