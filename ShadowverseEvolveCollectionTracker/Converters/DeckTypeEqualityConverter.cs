using ShadowverseEvolveCardTracker.Models;
using System.Globalization;
using System.Windows.Data;

namespace ShadowverseEvolveCardTracker.Converters
{
    public class DeckTypeEqualityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is DeckType dt && Enum.TryParse(typeof(DeckType), (string?)parameter, out var p) && dt.Equals(p);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string p &&
                Enum.TryParse<DeckType>(p, out var parsed)) return parsed;
            return Binding.DoNothing;
        }
    }
}