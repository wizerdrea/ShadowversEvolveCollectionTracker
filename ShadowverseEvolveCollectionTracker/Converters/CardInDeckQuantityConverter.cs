using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.Converters
{
    /// <summary>
    /// MultiValue converter that takes:
    ///  - values[0] = CardData (row)
    ///  - values[1] = Deck (current deck)
    /// and returns the integer quantity of that card in the appropriate deck (main or evolve).
    /// Returns a string so it binds cleanly to TextBlock.Text.
    /// </summary>
    public class CardInDeckQuantityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 2) return "0";

                var card = values[0] as CardData;
                var deck = values[1] as Deck;
                if (card == null || deck == null) return "0";

                // Leaders/tokens aren't represented by deck quantities
                if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    return "0";
                }

                if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    var e = deck.EvolveDeck.FirstOrDefault(d => d.Card.CardNumber == card.CardNumber);
                    return (e?.Quantity ?? 0).ToString();
                }

                var m = deck.MainDeck.FirstOrDefault(d => d.Card.CardNumber == card.CardNumber);
                return (m?.Quantity ?? 0).ToString();
            }
            catch
            {
                return "0";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}