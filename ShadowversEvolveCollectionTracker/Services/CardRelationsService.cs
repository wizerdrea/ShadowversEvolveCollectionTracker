using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using ShadowversEvolveCardTracker.Models;

namespace ShadowversEvolveCardTracker.Services
{
    /// <summary>
    /// Service to find and manage card relationships.
    /// Two cards are related if:
    /// 1. They have the same name but different types (e.g., evolved versions)
    /// 2. Either card's name appears in the other's text
    /// </summary>
    public class CardRelationsService
    {
        /// <summary>
        /// Finds all card relations in the provided collection and updates RelatedCards property for each card.
        /// </summary>
        public void FindCardRelations(IEnumerable<CardData> allCards)
        {
            if (allCards == null) return;

            var cardsList = allCards.ToList();

            // Clear existing relations
            foreach (var card in cardsList)
            {
                card.RelatedCards.Clear();
            }

            // Find relations for each card
            for (int i = 0; i < cardsList.Count; i++)
            {
                var card1 = cardsList[i];

                for (int j = i + 1; j < cardsList.Count; j++)
                {
                    var card2 = cardsList[j];

                    if (AreCardsRelated(card1, card2))
                    {
                        // Add bidirectional relationship using RelatedCard POCO
                        card1.RelatedCards.Add(new RelatedCard(card2.Name, card2.Type));
                        card2.RelatedCards.Add(new RelatedCard(card1.Name, card1.Type));
                    }
                }
            }

            // Notify property changed for all cards
            foreach (var card in cardsList)
            {
                if (card.RelatedCards.Count > 0)
                {
                    var temp = card.RelatedCards;
                    card.RelatedCards = new HashSet<RelatedCard>(temp);
                }
            }
        }

        /// <summary>
        /// Asynchronously finds card relations and updates the cards' RelatedCards properties on the UI thread.
        /// </summary>
        public async Task FindCardRelationsAsync(IEnumerable<CardData> allCards)
        {
            if (allCards == null) return;

            var relationResults = await Task.Run(() => FindCardRelationsResult(allCards));

            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            await dispatcher.InvokeAsync(() =>
            {
                foreach (var (card, set) in relationResults)
                {
                    card.RelatedCards = new HashSet<RelatedCard>(set);
                }
            });
        }

        public Dictionary<CardData, HashSet<RelatedCard>> FindCardRelationsResult(IEnumerable<CardData> allCards)
        {
            var results = new Dictionary<CardData, HashSet<RelatedCard>>();

            if (allCards == null) return results;

            var cardsList = allCards.ToList();

            foreach (var card in cardsList)
            {
                results[card] = new HashSet<RelatedCard>();
            }

            // Find relations for each card
            for (int i = 0; i < cardsList.Count; i++)
            {
                var card1 = cardsList[i];

                for (int j = i + 1; j < cardsList.Count; j++)
                {
                    var card2 = cardsList[j];

                    if (AreCardsRelated(card1, card2))
                    {
                        // Add bidirectional relationship
                        results[card1].Add(new RelatedCard(card2.Name, card2.Type));
                        results[card2].Add(new RelatedCard(card1.Name, card1.Type));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Determines if two cards are related based on the defined criteria.
        /// </summary>
        private bool AreCardsRelated(CardData card1, CardData card2)
        {
            // Skip if either card is missing a name
            if (string.IsNullOrWhiteSpace(card1.Name) || string.IsNullOrWhiteSpace(card2.Name))
                return false;

            // Rule 1: Same name, different type (e.g., evolved versions)
            if (string.Equals(card1.Name, card2.Name, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(card1.Type, card2.Type, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Rule 2: Either card's name appears in the other's text
            var text1 = card1.Text ?? string.Empty;
            var text2 = card2.Text ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(text1) &&
                text1.Contains(card2.Name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(text2) &&
                text2.Contains(card1.Name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the best representative cards for each related card, prioritizing same set and rarity.
        /// </summary>
        public List<CardData> GetRelatedCardInstances(
            CardData sourceCard,
            IEnumerable<CardData> allCards)
        {
            if (sourceCard?.RelatedCards == null || sourceCard.RelatedCards.Count == 0)
                return new List<CardData>();

            var result = new List<CardData>();
            var cardsList = allCards.ToList();

            foreach (var related in sourceCard.RelatedCards)
            {
                var relatedName = related.CardName;
                var relatedType = related.CardType;

                // Find all cards matching this name and type
                var candidates = cardsList
                    .Where(c => string.Equals(c.Name, relatedName, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(c.Type, relatedType, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (candidates.Count == 0) continue;

                // Prioritize: same set + same rarity > same set > same rarity > any
                var bestMatch = candidates
                    .OrderByDescending(c => string.Equals(c.Set, sourceCard.Set, StringComparison.OrdinalIgnoreCase) &&
                                           string.Equals(c.Rarity, sourceCard.Rarity, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(c => string.Equals(c.Set, sourceCard.Set, StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(c => string.Equals(c.Rarity, sourceCard.Rarity, StringComparison.OrdinalIgnoreCase))
                    .First();

                result.Add(bestMatch);
            }

            return result;
        }
    }
}