using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    public class SetCompletionTabViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<CardData> _allCards;
        private readonly ObservableCollection<SetCompletionRow> _rows = new();
        private readonly ICollectionView _setsView;

        public ICollectionView SetsView => _setsView;

        public SetCompletionTabViewModel(ObservableCollection<CardData> allCards)
        {
            _allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
            _setsView = CollectionViewSource.GetDefaultView(_rows);
            _setsView.SortDescriptions.Add(new SortDescription(nameof(SetCompletionRow.SetName), ListSortDirection.Ascending));

            if (_allCards is INotifyCollectionChanged incc)
                incc.CollectionChanged += AllCards_CollectionChanged;

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

                _rows.Add(new SetCompletionRow
                {
                    SetName = g.Key,
                    OneCardPercent = oneCardPercent,
                    PlaysetPercent = playsetPercent,
                    TotalCards = totalCards,
                    OwnedAtLeastOne = ownedAtLeastOne,
                    PlaysetOwned = numerator,
                    PlaysetTotal = denominator
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

            // Additional diagnostic fields kept public if useful
            public int TotalCards { get; set; }
            public int OwnedAtLeastOne { get; set; }
            public int PlaysetOwned { get; set; }
            public int PlaysetTotal { get; set; }
        }
    }
}