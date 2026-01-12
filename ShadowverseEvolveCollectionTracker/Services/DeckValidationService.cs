using System;
using System.Linq;
using ShadowverseEvolveCardTracker.Models;
using System.Collections.Generic;
using ShadowverseEvolveCardTracker.Constants;

namespace ShadowverseEvolveCardTracker.Services
{
    /// <summary>
    /// Provides validation logic for deck building operations.
    /// </summary>
    public sealed class DeckValidationService
    {
        private sealed class NameTypeEqualityComparer : IEqualityComparer<(string Name, string Type)>
        {
            public bool Equals((string Name, string Type) x, (string Name, string Type) y)
            {
                return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode((string Name, string Type) obj)
            {
                return (obj.Name?.ToLowerInvariant().GetHashCode() ?? 0) ^ (obj.Type?.ToLowerInvariant().GetHashCode() ?? 0);
            }
        }

        public bool IsValidForDeck(CardData card, Deck deck)
        {
            if (deck == null) return false;
            
            return deck.DeckType switch
            {
                DeckType.Standard => IsValidForStandard(card, deck),
                DeckType.Gloryfinder => IsValidForGloryfinder(card, deck),
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
                // Check total copies across all entries that match by name+type
                DeckType.Standard => CountMatching(deck.MainDeck, entry.Card) < 3 && currentTotal < 50,
                DeckType.Gloryfinder => false, // No duplicates allowed
                DeckType.CrossCraft => CountMatching(deck.MainDeck, entry.Card) < 3 && currentTotal < 50,
                _ => false
            };
        }

        public bool CanIncreaseEvolveDeckQuantity(DeckEntry entry, Deck deck)
        {
            if (entry == null || deck == null) return false;

            int currentTotal = deck.EvolveDeck.Sum(e => e.Quantity);

            return deck.DeckType switch
            {
                // Check total copies across all evolve entries that match by name+type
                DeckType.Standard => CountMatching(deck.EvolveDeck, entry.Card) < 3 && currentTotal < 10,
                DeckType.Gloryfinder => false, // No duplicates allowed
                DeckType.CrossCraft => CountMatching(deck.EvolveDeck, entry.Card) < 3 && currentTotal < 10,
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

        // UPDATED: tokens are now deck entries (not "non-deck" like leaders)
        public bool IsNonDeckCard(CardData card) =>
            IsLeaderCard(card);

        #endregion

        #region Private Validation Helpers

        private bool IsValidFormat(CardData card, Deck deck)
        {
            if (card == null || deck == null) return false;
            return string.IsNullOrWhiteSpace(card.Format) ||
                   string.Equals(card.Format.Trim(), "Any", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(card.Format.Trim(), deck.DeckType.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private bool IsValidForStandard(CardData card, Deck deck) =>
            IsValidFormat(card, deck) &&
            (string.Equals(card.Class, deck.Class1, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(card.Class, "Neutral", StringComparison.OrdinalIgnoreCase));

        private bool IsValidForGloryfinder(CardData card, Deck deck) =>
            IsValidFormat(card, deck) &&
            (!card.Type.Contains(CardTypes.Leader, StringComparison.OrdinalIgnoreCase) || 
             card.Class.Contains(deck.Class1, StringComparison.OrdinalIgnoreCase));

        private bool IsValidForCrossCraft(CardData card, Deck deck) =>
            IsValidFormat(card, deck) &&
            (string.Equals(card.Class, deck.Class1, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(card.Class, deck.Class2, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(card.Class, "Neutral", StringComparison.OrdinalIgnoreCase));

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
            // Sum quantity across any entries that match by name and type
            int matching = CountMatching(deck.MainDeck, card);
            if (matching >= 3) return false;

            int currentCount = deck.MainDeck.Sum(e => e.Quantity);
            return currentCount < 50;
        }

        private bool CanAddToMainDeckGloryfinder(CardData card, Deck deck)
        {
            // No duplicates allowed (by name+type)
            int matching = CountMatching(deck.MainDeck, card);
            if (matching > 0) return false;

            // Cannot exceed 50 cards
            int currentCount = deck.MainDeck.Sum(e => e.Quantity);
            if (currentCount >= 50) return false;

            // Cannot be the glory card (compare by CardNumber as before)
            if (deck.GloryCard != null && card.CardNumber == deck.GloryCard.CardNumber)
                return false;

            return true;
        }

        private bool CanAddToEvolveDeckStandard(CardData card, Deck deck)
        {
            int matching = CountMatching(deck.EvolveDeck, card);
            if (matching >= 3) return false;

            int currentCount = deck.EvolveDeck.Sum(e => e.Quantity);
            return currentCount < 10;
        }

        private bool CanAddToEvolveDeckGloryfinder(CardData card, Deck deck)
        {
            int matching = CountMatching(deck.EvolveDeck, card);
            if (matching > 0) return false;

            int currentCount = deck.EvolveDeck.Sum(e => e.Quantity);
            return currentCount < 20;
        }

        private static bool IsSameNameAndType(CardData a, CardData b)
        {
            if (a == null || b == null) return false;
            return string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(a.Type, b.Type, StringComparison.OrdinalIgnoreCase);
        }

        private static int CountMatching(IEnumerable<DeckEntry> entries, CardData card)
        {
            if (card == null) return 0;
            return entries.Where(e => IsSameNameAndType(e.Card, card))
                          .Sum(e => e.Quantity);
        }

        #endregion

        #region Deck-level Validation

        /// <summary>
        /// Returns a list of failing validation messages for the deck.
        /// If the returned list is empty the deck is valid.
        /// </summary>
        public List<string> ValidateDeck(Deck deck)
        {
            var errors = new List<string>();
            if (deck == null)
            {
                errors.Add("No deck selected.");
                return errors;
            }

            // Helper to count copies across both main and evolve lists by name+type
            static int CountAcross(Deck d, CardData card)
            {
                if (card == null) return 0;
                return d.MainDeck.Where(e => IsSameNameAndType(e.Card, card)).Sum(e => e.Quantity)
                     + d.EvolveDeck.Where(e => IsSameNameAndType(e.Card, card)).Sum(e => e.Quantity);
            }

            IEnumerable<DeckEntry> allEntries() => deck.MainDeck.Concat(deck.EvolveDeck);

            switch (deck.DeckType)
            {
                case DeckType.Standard:
                    if (deck.Leader1 == null)
                        errors.Add("Standard decks must have 1 leader.");

                    var mainCount = deck.MainDeck.Sum(e => e.Quantity);
                    if (mainCount < 40)
                        errors.Add($"Main deck must contain at least 40 cards (current {mainCount}).");
                    if (mainCount > 50)
                        errors.Add($"Main deck cannot exceed 50 cards (current {mainCount}).");

                    var evolveCount = deck.EvolveDeck.Sum(e => e.Quantity);
                    if (evolveCount > 10)
                        errors.Add($"Evolve deck cannot exceed 10 cards (current {evolveCount}).");

                    // duplicates and class checks
                    foreach (var group in allEntries().GroupBy(
                        e => new ( e.Card.Name, e.Card.Type ), 
                        new NameTypeEqualityComparer()))
                    {
                        var sample = group.First().Card;
                        int copies = CountAcross(deck, sample);
                        if (copies > 3)
                            errors.Add($"No more than three copies allowed of \"{sample.Name}\" ({sample.Type}). Found {copies}.");
                    }

                    foreach (var entry in allEntries())
                    {
                        var c = entry.Card;
                        if (!(string.Equals(c.Class, deck.Class1, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(c.Class, "Neutral", StringComparison.OrdinalIgnoreCase)))
                        {
                            errors.Add($"Card \"{c.Name}\" is class {c.Class} but must be {deck.Class1} or Neutral.");
                        }
                    }
                    break;

                case DeckType.Gloryfinder:
                    if (deck.Leader1 == null)
                        errors.Add("Gloryfinder decks must have 1 leader.");

                    if (deck.GloryCard == null)
                        errors.Add("Gloryfinder decks must have 1 glory card.");
                    else
                    {
                        var glory = deck.GloryCard;
                        if (IsLeaderCard(glory) || IsTokenCard(glory) || IsEvolvedCard(glory))
                            errors.Add("Glory card must not be a leader, token, or evolved card.");
                        if (!string.Equals(glory.Class, deck.Class1, StringComparison.OrdinalIgnoreCase))
                            errors.Add($"Glory card must be of the deck's class ({deck.Class1}).");
                        if (deck.MainDeck.Any(e => e.Card.CardNumber == glory.CardNumber) ||
                            deck.EvolveDeck.Any(e => e.Card.CardNumber == glory.CardNumber))
                            errors.Add("Glory card must not be present in main or evolve decks.");
                    }

                    var mainCountG = deck.MainDeck.Sum(e => e.Quantity);
                    if (mainCountG != 50)
                        errors.Add($"Main deck must contain exactly 50 cards (current {mainCountG}).");

                    var evolveCountG = deck.EvolveDeck.Sum(e => e.Quantity);
                    if (evolveCountG > 20)
                        errors.Add($"Evolve deck cannot exceed 20 cards (current {evolveCountG}).");

                    foreach (var group in allEntries().GroupBy(
                        e => new(e.Card.Name, e.Card.Type),
                        new NameTypeEqualityComparer()))
                    {
                        var sample = group.First().Card;
                        int copies = CountAcross(deck, sample);
                        if (copies > 1)
                            errors.Add($"No more than one copy allowed of \"{sample.Name}\" ({sample.Type}). Found {copies}.");
                    }

                    break;

                case DeckType.CrossCraft:
                    if (deck.Leader1 == null || deck.Leader2 == null)
                        errors.Add("Cross Craft decks must have 2 leaders.");
                    else if (string.Equals(deck.Leader1.Class, deck.Leader2.Class, StringComparison.OrdinalIgnoreCase))
                        errors.Add("Cross Craft leaders must be from different classes.");

                    var mainCountC = deck.MainDeck.Sum(e => e.Quantity);
                    if (mainCountC < 40)
                        errors.Add($"Main deck must contain at least 40 cards (current {mainCountC}).");
                    if (mainCountC > 50)
                        errors.Add($"Main deck cannot exceed 50 cards (current {mainCountC}).");

                    bool hasClass1 = allEntries().Any(e => string.Equals(e.Card.Class, deck.Class1, StringComparison.OrdinalIgnoreCase));
                    bool hasClass2 = allEntries().Any(e => string.Equals(e.Card.Class, deck.Class2, StringComparison.OrdinalIgnoreCase));
                    if (!hasClass1)
                        errors.Add($"Deck must contain at least one card of class {deck.Class1}.");
                    if (!hasClass2)
                        errors.Add($"Deck must contain at least one card of class {deck.Class2}.");

                    var evolveCountC = deck.EvolveDeck.Sum(e => e.Quantity);
                    if (evolveCountC > 10)
                        errors.Add($"Evolve deck cannot exceed 10 cards (current {evolveCountC}).");

                    foreach (var group in allEntries().GroupBy(
                        e => new(e.Card.Name, e.Card.Type),
                        new NameTypeEqualityComparer()))
                    {
                        var sample = group.First().Card;
                        int copies = CountAcross(deck, sample);
                        if (copies > 3)
                            errors.Add($"No more than three copies allowed of \"{sample.Name}\" ({sample.Type}). Found {copies}.");
                    }

                    foreach (var entry in allEntries())
                    {
                        var c = entry.Card;
                        if (!(string.Equals(c.Class, deck.Class1, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(c.Class, deck.Class2, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(c.Class, "Neutral", StringComparison.OrdinalIgnoreCase)))
                        {
                            errors.Add($"Card \"{c.Name}\" is class {c.Class} but must be {deck.Class1}, {deck.Class2}, or Neutral.");
                        }
                    }
                    break;

                default:
                    errors.Add("Unknown deck type.");
                    break;
            }

            // Use distinct messages to avoid duplicates
            return errors.Distinct().ToList();
        }

        /// <summary>
        /// Backwards-compatible boolean check using ValidateDeck.
        /// </summary>
        public bool IsDeckValid(Deck deck)
        {
            var errors = ValidateDeck(deck);
            return errors.Count == 0;
        }

        #endregion
    }
}