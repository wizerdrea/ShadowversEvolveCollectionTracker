using System;

namespace ShadowverseEvolveCardTracker.Models
{
    /// <summary>
    /// Serializable representation of a related card entry.
    /// Uses case-insensitive equality so HashSet treats same name/type as the same relation.
    /// </summary>
    public sealed class RelatedCard : IEquatable<RelatedCard>
    {
        public string CardName { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;

        public RelatedCard() { }

        public RelatedCard(string cardName, string cardType)
        {
            CardName = cardName ?? string.Empty;
            CardType = cardType ?? string.Empty;
        }

        public bool Equals(RelatedCard? other)
        {
            if (other is null) return false;
            return string.Equals(CardName, other.CardName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(CardType, other.CardType, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as RelatedCard);

        public override int GetHashCode()
        {
            // Use case-insensitive combined key
            return HashCode.Combine(CardName?.ToUpperInvariant(), CardType?.ToUpperInvariant());
        }
    }
}