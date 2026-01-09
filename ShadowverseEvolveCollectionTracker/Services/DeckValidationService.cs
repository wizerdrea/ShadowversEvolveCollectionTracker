using System;
using System.Linq;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.Services
{
    /// <summary>
    /// Provides validation logic for deck building operations.
    /// </summary>
    public sealed class DeckValidationService
    {
        public bool IsValidForDeck(CardData card, Deck deck)
        {
            if (deck == null) return false;
            
            return deck.DeckType switch
            {
                DeckType.Standard => IsValidForStandard(card, deck),
                DeckType.Gloryfinder => IsValidForGloryfinder(card),
                DeckType.CrossCraft => IsValidForCrossCraft(card, deck),
                _ => false
            };
        }

        public bool CanAddLeader(CardData card, Deck deck)
        {
            if (card == null || deck == null) return false;

            return deck.DeckType switch
            {
                DeckType.Standard => deck.Leader1 is null,
                DeckType.Gloryfinder => deck.Leader1 is null,
                DeckType.CrossCraft => CanAddLeaderCrossCraft(card, deck),
                _ => false
            };
        }

        public bool CanAddToMainDeck(CardData card, Deck deck)
        {
            if (card == null || deck == null) return false;

            return deck.DeckType switch
            {
                DeckType.Standard => CanAddToMainDeckStandard(card, deck),
                DeckType.Gloryfinder => CanAddToMainDeckGloryfinder(card, deck),
                DeckType.CrossCraft => CanAddToMainDeckStandard(card, deck),
                _ => false
            };
        }

        public bool CanAddToEvolveDeck(CardData card, Deck deck)
        {
            if (card == null || deck == null) return false;

            // Only evolved followers can go in evolve deck
            if (!IsEvolvedCard(card)) return false;

            return deck.DeckType switch
            {
                DeckType.Standard => CanAddToEvolveDeckStandard(card, deck),
                DeckType.Gloryfinder => CanAddToEvolveDeckGloryfinder(card, deck),
                DeckType.CrossCraft => CanAddToEvolveDeckStandard(card, deck),
                _ => false
            };
        }

        public bool CanIncreaseMainDeckQuantity(DeckEntry entry, Deck deck)
        {
            if (entry == null || deck == null) return false;

            int currentTotal = deck.MainDeck.Sum(e => e.Quantity);

            return deck.DeckType switch
            {
                DeckType.Standard => entry.Quantity < 3 && currentTotal < 50,
                DeckType.Gloryfinder => false, // No duplicates allowed
                DeckType.CrossCraft => entry.Quantity < 3 && currentTotal < 50,
                _ => false
            };
        }

        public bool CanIncreaseEvolveDeckQuantity(DeckEntry entry, Deck deck)
        {
            if (entry == null || deck == null) return false;

            int currentTotal = deck.EvolveDeck.Sum(e => e.Quantity);

            return deck.DeckType switch
            {
                DeckType.Standard => entry.Quantity < 3 && currentTotal < 10,
                DeckType.Gloryfinder => false, // No duplicates allowed
                DeckType.CrossCraft => entry.Quantity < 3 && currentTotal < 10,
                _ => false
            };
        }

        #region Card Type Checks

        public bool IsLeaderCard(CardData card) =>
            card?.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false;

        public bool IsTokenCard(CardData card) =>
            card?.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false;

        public bool IsEvolvedCard(CardData card) =>
            card?.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false;

        public bool IsNonDeckCard(CardData card) =>
            IsLeaderCard(card) || IsTokenCard(card);

        #endregion

        #region Private Validation Helpers

        private bool IsValidForStandard(CardData card, Deck deck) =>
            string.Equals(card.Class, deck.Class1, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(card.Class, "Neutral", StringComparison.OrdinalIgnoreCase);

        private bool IsValidForGloryfinder(CardData card) => true;

        private bool IsValidForCrossCraft(CardData card, Deck deck) =>
            string.Equals(card.Class, deck.Class1, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(card.Class, deck.Class2, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(card.Class, "Neutral", StringComparison.OrdinalIgnoreCase);

        private bool CanAddLeaderCrossCraft(CardData card, Deck deck)
        {
            if (deck.Leader1 is null) return true;
            if (deck.Leader2 is null && 
                !deck.Leader1.Class.Contains(card.Class, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private bool CanAddToMainDeckStandard(CardData card, Deck deck)
        {
            var existing = deck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null && existing.Quantity >= 3) return false;

            int currentCount = deck.MainDeck.Sum(e => e.Quantity);
            return currentCount < 50;
        }

        private bool CanAddToMainDeckGloryfinder(CardData card, Deck deck)
        {
            // No duplicates allowed
            var existing = deck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null) return false;

            // Cannot exceed 50 cards
            int currentCount = deck.MainDeck.Sum(e => e.Quantity);
            if (currentCount >= 50) return false;

            // Cannot be the glory card
            if (deck.GloryCard != null && card.CardNumber == deck.GloryCard.CardNumber)
                return false;

            return true;
        }

        private bool CanAddToEvolveDeckStandard(CardData card, Deck deck)
        {
            var existing = deck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null && existing.Quantity >= 3) return false;

            int currentCount = deck.EvolveDeck.Sum(e => e.Quantity);
            return currentCount < 10;
        }

        private bool CanAddToEvolveDeckGloryfinder(CardData card, Deck deck)
        {
            var existing = deck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null) return false;

            int currentCount = deck.EvolveDeck.Sum(e => e.Quantity);
            return currentCount < 20;
        }

        #endregion
    }
}