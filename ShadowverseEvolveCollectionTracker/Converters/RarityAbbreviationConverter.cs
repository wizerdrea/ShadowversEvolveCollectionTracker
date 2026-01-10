using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ShadowverseEvolveCardTracker.Converters
{
    /// <summary>
    /// Converts full rarity strings (possibly containing multiple rarities separated by '/')
    /// into their abbreviated forms according to the project's mapping.
    /// Examples:
    ///   "Gold" -> "G"
    ///   "Gold / Premium" -> "G/P"
    ///   "Super Legendary" -> "SL"
    /// </summary>
    public class RarityAbbreviationConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Bronze", "B" },
            { "Silver", "S" },
            { "Gold", "G" },
            { "Legendary", "L" },
            { "Super Legendary", "SL" },
            { "Ultimate", "U" },
            { "Special", "SP" },
            { "Premium", "P" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var s = value as string;
                if (string.IsNullOrWhiteSpace(s))
                    return string.Empty;

                // Split by '/' and map each trimmed part to abbreviation
                var parts = s.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(p => p.Trim())
                             .Where(p => !string.IsNullOrEmpty(p))
                             .Select(p =>
                             {
                                 if (_map.TryGetValue(p, out var abbr))
                                     return abbr;

                                 // Try to match keys that may differ slightly (e.g., extra spaces/casing)
                                 var match = _map.FirstOrDefault(kvp => string.Equals(kvp.Key, p, StringComparison.OrdinalIgnoreCase));
                                 if (!string.IsNullOrEmpty(match.Key))
                                     return match.Value;

                                 // Fallback: use uppercase initials (for unknown words)
                                 var tokens = p.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                 if (tokens.Length == 1)
                                     return tokens[0].Substring(0, Math.Min(1, tokens[0].Length)).ToUpperInvariant();
                                 // For multi-word unknowns, take first letters (e.g., "Super Rare" -> "SR")
                                 return string.Concat(tokens.Select(t => t[0])).ToUpperInvariant();
                             })
                             .Where(x => !string.IsNullOrEmpty(x));

                return string.Join("/", parts);
            }
            catch
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}