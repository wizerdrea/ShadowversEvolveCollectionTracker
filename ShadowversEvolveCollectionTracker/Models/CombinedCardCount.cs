using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowverseEvolveCardTracker.Models
{
    public class CombinedCardCount
    {
        private List<CardData> _cards;

        public CombinedCardCount(IEnumerable<CardData> cards)
        {
            _cards = cards.ToList();
        }

        // Base name, append " (Evolved)" when appropriate
        public string Name
        {
            get
            {
                var baseName = _cards.FirstOrDefault()?.Name ?? string.Empty;
                return IsEvolved ? $"{baseName} (Evolved)" : baseName;
            }
        }

        // True when any card in the group has "Evolved" in its Type (case-insensitive)
        public bool IsEvolved =>
            _cards.Any(c => !string.IsNullOrEmpty(c.Type) && c.Type.IndexOf("Evolved", StringComparison.OrdinalIgnoreCase) >= 0);

        public List<string> Images => _cards
            .Where(c => c.QuantityOwned > 0)
            .Select(c => c.ImageFile)
            .ToList();

        public List<CardData> Cards =>
            TotalQuantityOwned > 0
                ? _cards.Where(c => c.QuantityOwned > 0).ToList()
                : _cards.FirstOrDefault() is not null
                    ? new List<CardData> { _cards.First() }
                    : new List<CardData>();

        public int TotalQuantityOwned => _cards.Sum(c => c.QuantityOwned);

        // Expose underlying cards so viewmodels can subscribe to property changes
        public IEnumerable<CardData> AllCards => _cards;

        // Convenience helper for favorites filtering
        public bool HasFavorite => _cards.Any(c => c.IsFavorite);
    }
}
