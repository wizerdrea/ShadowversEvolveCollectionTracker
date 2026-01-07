using System;
using System.Globalization;
using System.Windows.Data;
using ShadowversEvolveCardTracker.Models;

namespace ShadowversEvolveCardTracker.Converters
{
    /// <summary>
    /// Determines category for a CombinedCardCount row:
    /// - "None" when total owned == 0
    /// - "Low" when total owned &lt; CopiesNeededForPlayset
    /// - "High" when total owned &gt;= CopiesNeededForPlayset
    /// This replaces the previous hard-coded threshold of 3.
    /// </summary>
    public class CombinedCountToCategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not CombinedCardCount group)
                return "None";

            try
            {
                int totalOwned = group.TotalQuantityOwned;

                // Determine playset size from first underlying card (fallback to 3)
                var first = group.AllCards is System.Collections.Generic.IEnumerable<CardData> cards
                    ? System.Linq.Enumerable.FirstOrDefault(cards)
                    : null;

                int copiesNeeded = first?.CopiesNeededForPlayset ?? 3;

                if (totalOwned <= 0) return "None";
                if (totalOwned < copiesNeeded) return "Low";
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