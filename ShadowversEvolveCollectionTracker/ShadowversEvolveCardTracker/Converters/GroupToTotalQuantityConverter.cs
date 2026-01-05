using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ShadowversEvolveCardTracker.Converters
{
    // Converts a CollectionViewGroup (received when binding to CollectionViewSource.View.Groups)
    // into the total of the QuantityOwned property for the group's items.
    public class GroupToTotalQuantityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Defensive: if it's not a group, return 0
            var group = value as System.Windows.Data.CollectionViewGroup;
            if (group == null)
            {
                return 0;
            }

            int total = 0;
            foreach (var item in group.Items)
            {
                if (item == null) continue;

                // Try to read QuantityOwned via reflection to avoid compile-time dependency on CardData type.
                var prop = item.GetType().GetProperty("QuantityOwned");
                if (prop == null) continue;

                try
                {
                    var raw = prop.GetValue(item);
                    if (raw == null) continue;

                    // Handle numeric types and convertible values
                    if (raw is int i) total += i;
                    else if (raw is long l) total += (int)l;
                    else
                    {
                        // Attempt convert
                        total += System.Convert.ToInt32(raw, CultureInfo.InvariantCulture);
                    }
                }
                catch
                {
                    // ignore individual conversion errors and continue summing
                }
            }

            return total;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}