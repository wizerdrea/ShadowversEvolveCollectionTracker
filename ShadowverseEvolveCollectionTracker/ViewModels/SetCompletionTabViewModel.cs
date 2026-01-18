using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using ShadowverseEvolveCardTracker.Constants;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    public class SetCompletionTabViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<CardData> _allCards;
        private readonly ObservableCollection<CombinedCardCount> _combinedCardCounts;
        private readonly ObservableCollection<SetCompletionRow> _rows = new();
        private readonly ICollectionView _setsView;

        public ICollectionView SetsView => _setsView;

        public SetCompletionTabViewModel(ObservableCollection<CardData> allCards, ObservableCollection<CombinedCardCount> combinedCardCounts)
        {
            _allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
            _combinedCardCounts = combinedCardCounts ?? throw new ArgumentNullException(nameof(combinedCardCounts));
            _setsView = CollectionViewSource.GetDefaultView(_rows);
            _setsView.SortDescriptions.Add(new SortDescription(nameof(SetCompletionRow.SetName), ListSortDirection.Ascending));

            if (_allCards is INotifyCollectionChanged incc1)
                incc1.CollectionChanged += AllCards_CollectionChanged;

            if (_combinedCardCounts is INotifyCollectionChanged incc2)
                incc2.CollectionChanged += CombinedGroups_CollectionChanged;

            foreach (var c in _allCards)
                SubscribeToCard(c);

            RecalculateRows();
        }

        private void AllCards_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e?.OldItems != null)
            {
                foreach (var old in e.OldItems.OfType<CardData>())
                    UnsubscribeFromCard(old);
            }

            if (e?.NewItems != null)
            {
                foreach (var nw in e.NewItems.OfType<CardData>())
                    SubscribeToCard(nw);
            }

            RecalculateRows();
        }

        private void CombinedGroups_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e?.OldItems != null)
            {
                foreach (var old in e.OldItems.OfType<CombinedCardCount>())
                    UnsubscribeFromGroup(old);
            }

            if (e?.NewItems != null)
            {
                foreach (var nw in e.NewItems.OfType<CombinedCardCount>())
                    SubscribeToGroup(nw);
            }

            RecalculateRows();
        }

        private void SubscribeToGroup(CombinedCardCount group)
        {
            foreach (var card in group.AllCards)
            {
                if (card is INotifyPropertyChanged inpc)
                    inpc.PropertyChanged += Card_PropertyChanged;
            }
        }

        private void UnsubscribeFromGroup(CombinedCardCount group)
        {
            foreach (var card in group.AllCards)
            {
                if (card is INotifyPropertyChanged inpc)
                    inpc.PropertyChanged -= Card_PropertyChanged;
            }
        }

        private void SubscribeToCard(CardData card)
        {
            if (card is INotifyPropertyChanged inpc)
                inpc.PropertyChanged += Card_PropertyChanged;
        }

        private void UnsubscribeFromCard(CardData card)
        {
            if (card is INotifyPropertyChanged inpc)
                inpc.PropertyChanged -= Card_PropertyChanged;
        }

        private void Card_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Recalculate when properties that affect set completion change
            if (string.IsNullOrEmpty(e?.PropertyName) ||
                e.PropertyName == nameof(CardData.QuantityOwned) ||
                e.PropertyName == nameof(CardData.Set) ||
                e.PropertyName == nameof(CardData.Type) ||
                e.PropertyName == nameof(CardData.Format) ||
                e.PropertyName == nameof(CardData.CardNumber) ||
                e.PropertyName == nameof(CardData.Name))
            {
                RecalculateRows();
            }
        }

        private void RecalculateRows()
        {
            var groups = _allCards
                .GroupBy(c => (c.Set ?? string.Empty).Trim())
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var uniqueCards = _allCards.GroupBy(c => new
            {
                Name = c.Name?.Trim() ?? string.Empty,
                IsEvolved = !string.IsNullOrEmpty(c.Type) && c.Type.IndexOf("Evolved", StringComparison.OrdinalIgnoreCase) >= 0
            }).ToList();

            _rows.Clear();

            foreach (var g in groups)
            {
                if (string.IsNullOrEmpty(g.Key)) continue; // skip blank set entries

                int totalCards = g.Count();
                int ownedAtLeastOne = g.Count(c => c.QuantityOwned > 0);

                double oneCardPct = totalCards == 0 ? 100.0 : (ownedAtLeastOne * 100.0) / totalCards;
                int oneCardPercent = (int)Math.Round(oneCardPct, 0, MidpointRounding.AwayFromZero);

                // Playset calculation: limit each card's contribution to its CopiesNeededForPlayset
                int numerator = g.Sum(c => Math.Min(c.QuantityOwned, c.CopiesNeededForPlayset));
                int denominator = g.Sum(c => c.CopiesNeededForPlayset);

                double playsetPct = denominator == 0 ? 100.0 : (numerator * 100.0) / denominator;
                int playsetPercent = (int)Math.Round(playsetPct, 0, MidpointRounding.AwayFromZero);

                // Unique card calculations
                // Group by Name and Type to identify unique cards across all versions
                var uniqueCardsInSet = g.GroupBy(c => new
                {
                    Name = c.Name?.Trim() ?? string.Empty,
                    IsEvolved = !string.IsNullOrEmpty(c.Type) && c.Type.IndexOf("Evolved", StringComparison.OrdinalIgnoreCase) >= 0
                }).Where(g => (!g.First().Type?.Contains(CardTypes.Leader, StringComparison.OrdinalIgnoreCase)) ?? true)
                .Select(a => a.Key).OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();

                var uniqueCardsInSetCombined = _combinedCardCounts
                    .Where(combinedCard => uniqueCardsInSet.Any(u => u.Name == combinedCard.AllCards.First().Name && u.IsEvolved == combinedCard.IsEvolved))
                    .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                int totalUniqueCards = uniqueCardsInSet.Count;

                // Unique 1 Card: at least one copy owned of any version
                int uniqueOwnedAtLeastOne = uniqueCardsInSetCombined.Count(uc => uc.TotalQuantityOwned > 0);

                double uniqueOneCardPct = totalUniqueCards == 0 ? 100.0 : (uniqueOwnedAtLeastOne * 100.0) / totalUniqueCards;
                int uniqueOneCardPercent = (int)Math.Round(uniqueOneCardPct, 0, MidpointRounding.AwayFromZero);

                // Unique Playset: sum of all copies owned >= playset requirement (use the first card's playset requirement as they should be the same)
                int cardsOwned = uniqueCardsInSetCombined.Sum(c => Math.Min(c.TotalQuantityOwned, c.AllCards.First().CopiesNeededForPlayset));
                int cardsNeeded = uniqueCardsInSetCombined.Sum(c => c.AllCards.First().CopiesNeededForPlayset);

                int uniquePlaysetOwned = uniqueCardsInSetCombined.Count(uc => uc.TotalQuantityOwned >= uc.AllCards.First().CopiesNeededForPlayset );

                double uniquePlaysetPct = totalUniqueCards == 0 ? 100.0 : (cardsOwned * 100.0) / cardsNeeded;
                int uniquePlaysetPercent = (int)Math.Round(uniquePlaysetPct, 0, MidpointRounding.AwayFromZero);

                _rows.Add(new SetCompletionRow
                {
                    SetName = g.Key,
                    OneCardPercent = oneCardPercent,
                    PlaysetPercent = playsetPercent,
                    UniqueOneCardPercent = uniqueOneCardPercent,
                    UniquePlaysetPercent = uniquePlaysetPercent,
                    TotalCards = totalCards,
                    OwnedAtLeastOne = ownedAtLeastOne,
                    PlaysetOwned = numerator,
                    PlaysetTotal = denominator,
                    TotalUniqueCards = totalUniqueCards,
                    UniqueOwnedAtLeastOne = uniqueOwnedAtLeastOne,
                    UniquePlaysetOwned = uniquePlaysetOwned
                });
            }

            OnPropertyChanged(nameof(SetsView));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                return true;
            }
            return false;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Row model for DataGrid
        public class SetCompletionRow
        {
            public string SetName { get; set; } = string.Empty;
            public int OneCardPercent { get; set; }
            public int PlaysetPercent { get; set; }
            public int UniqueOneCardPercent { get; set; }
            public int UniquePlaysetPercent { get; set; }

            // Additional diagnostic fields kept public if useful
            public int TotalCards { get; set; }
            public int OwnedAtLeastOne { get; set; }
            public int PlaysetOwned { get; set; }
            public int PlaysetTotal { get; set; }
            public int TotalUniqueCards { get; set; }
            public int UniqueOwnedAtLeastOne { get; set; }
            public int UniquePlaysetOwned { get; set; }
        }
    }
}