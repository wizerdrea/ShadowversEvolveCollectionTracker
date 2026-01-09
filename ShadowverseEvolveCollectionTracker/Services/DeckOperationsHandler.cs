using System;
using System.Linq;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.Services
{
    /// <summary>
    /// Handles operations for adding, removing, and modifying deck entries.
    /// </summary>
    public sealed class DeckOperationsHandler
    {
        private readonly DeckValidationService _validationService;

        public DeckOperationsHandler(DeckValidationService validationService)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        public bool TryAddCard(CardData card, Deck deck, out Action? onSuccess)
        {
            onSuccess = null;
            
            if (card == null || deck == null) return false;

            if (_validationService.IsLeaderCard(card))
            {
                if (!_validationService.CanAddLeader(card, deck)) return false;
                onSuccess = () => AddLeader(card, deck);
                return true;
            }

            if (_validationService.IsTokenCard(card))
            {
                // Tokens aren't added to decks
                return false;
            }

            if (_validationService.IsEvolvedCard(card))
            {
                if (!_validationService.CanAddToEvolveDeck(card, deck)) return false;
                onSuccess = () => AddToEvolveDeck(card, deck);
                return true;
            }

            if (!_validationService.CanAddToMainDeck(card, deck)) return false;
            onSuccess = () => AddToMainDeck(card, deck);
            return true;
        }

        public void IncreaseQuantity(DeckEntry entry, Deck deck, bool isEvolveDeck)
        {
            if (entry == null || deck == null) return;

            bool canIncrease = isEvolveDeck
                ? _validationService.CanIncreaseEvolveDeckQuantity(entry, deck)
                : _validationService.CanIncreaseMainDeckQuantity(entry, deck);

            if (canIncrease)
            {
                entry.Quantity++;
            }
        }

        public void DecreaseQuantity(DeckEntry entry, Deck deck, bool isEvolveDeck)
        {
            if (entry == null || deck == null) return;

            if (entry.Quantity <= 1)
            {
                if (isEvolveDeck)
                    deck.EvolveDeck.Remove(entry);
                else
                    deck.MainDeck.Remove(entry);
            }
            else
            {
                entry.Quantity--;
            }
        }

        #region Private Helpers

        private void AddLeader(CardData card, Deck deck)
        {
            if (deck.DeckType is DeckType.Standard or DeckType.Gloryfinder)
            {
                deck.Leader1 = card;
                return;
            }

            if (deck.Leader1 is null)
                deck.Leader1 = card;
            else
                deck.Leader2 = card;
        }

        private void AddToMainDeck(CardData card, Deck deck)
        {
            var existing = deck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                deck.MainDeck.Add(new DeckEntry { Card = card, Quantity = 1 });
            }
        }

        private void AddToEvolveDeck(CardData card, Deck deck)
        {
            var existing = deck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                deck.EvolveDeck.Add(new DeckEntry { Card = card, Quantity = 1 });
            }
        }

        #endregion
    }
}