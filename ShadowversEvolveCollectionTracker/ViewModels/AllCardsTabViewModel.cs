using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Data;
using ShadowversEvolveCardTracker.Models;

namespace ShadowversEvolveCardTracker.ViewModels
{
    public class AllCardsTabViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<CardData> _allCards;
        private readonly ICollectionView _filteredCards;

        public ICollectionView FilteredCards => _filteredCards;
        public CardViewerViewModel CardViewer { get; } = new CardViewerViewModel();

        private string? _nameFilter;
        public string? NameFilter
        {
            get => _nameFilter;
            set
            {
                if (SetProperty(ref _nameFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private string? _cardNumberFilter;
        public string? CardNumberFilter
        {
            get => _cardNumberFilter;
            set
            {
                if (SetProperty(ref _cardNumberFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private string _qtyOwnedFilter = "Both";
        public string QtyOwnedFilter
        {
            get => _qtyOwnedFilter;
            set
            {
                if (SetProperty(ref _qtyOwnedFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private string? _rarityFilter;
        public string? RarityFilter
        {
            get => _rarityFilter;
            set
            {
                if (SetProperty(ref _rarityFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private string? _setFilter;
        public string? SetFilter
        {
            get => _setFilter;
            set
            {
                if (SetProperty(ref _setFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private string? _formatFilter;
        public string? FormatFilter
        {
            get => _formatFilter;
            set
            {
                if (SetProperty(ref _formatFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private string? _classFilter;
        public string? ClassFilter
        {
            get => _classFilter;
            set
            {
                if (SetProperty(ref _classFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private string? _typeFilter;
        public string? TypeFilter
        {
            get => _typeFilter;
            set
            {
                if (SetProperty(ref _typeFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private string? _traitsFilter;
        public string? TraitsFilter
        {
            get => _traitsFilter;
            set
            {
                if (SetProperty(ref _traitsFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private string? _textFilter;
        public string? TextFilter
        {
            get => _textFilter;
            set
            {
                if (SetProperty(ref _textFilter, value))
                    _filteredCards.Refresh();
            }
        }

        private bool _favoritesOnly;
        public bool FavoritesOnly
        {
            get => _favoritesOnly;
            set
            {
                if (SetProperty(ref _favoritesOnly, value))
                    _filteredCards.Refresh();
            }
        }

        private bool _wishlistedOnly;
        public bool WishlistedOnly
        {
            get => _wishlistedOnly;
            set
            {
                if (SetProperty(ref _wishlistedOnly, value))
                    _filteredCards.Refresh();
            }
        }

        private CardData? _selectedCard;
        public CardData? SelectedCard
        {
            get => _selectedCard;
            set
            {
                if (SetProperty(ref _selectedCard, value))
                {
                    CardViewer.SetCard(value);
                }
            }
        }

        public AllCardsTabViewModel(ObservableCollection<CardData> allCards)
        {
            _allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
            _filteredCards = CollectionViewSource.GetDefaultView(_allCards);
            _filteredCards.Filter = FilterCard;

            // subscribe to collection changes and card property changes so filter updates when card properties change
            if (_allCards is INotifyCollectionChanged incc)
                incc.CollectionChanged += AllCards_CollectionChanged;

            foreach (var c in _allCards)
                SubscribeToCard(c);
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
            // changes that may affect filtering should refresh the view
            if (e.PropertyName == nameof(CardData.QuantityOwned) ||
                e.PropertyName == nameof(CardData.Name) ||
                e.PropertyName == nameof(CardData.Rarity) ||
                e.PropertyName == nameof(CardData.Set) ||
                e.PropertyName == nameof(CardData.Format) ||
                e.PropertyName == nameof(CardData.Class) ||
                e.PropertyName == nameof(CardData.Type) ||
                e.PropertyName == nameof(CardData.Traits) ||
                e.PropertyName == nameof(CardData.Text) ||
                e.PropertyName == nameof(CardData.CardNumber) ||
                e.PropertyName == nameof(CardData.IsFavorite) ||
                e.PropertyName == nameof(CardData.IsWishlisted) ||
                e.PropertyName == nameof(CardData.WishlistDesiredQuantity))
            {
                _filteredCards.Refresh();
            }
        }

        private bool FilterCard(object? obj)
        {
            if (obj is not CardData card) return false;

            if (FavoritesOnly && !card.IsFavorite)
                return false;

            if (WishlistedOnly && !card.IsWishlisted)
                return false;

            if (!string.IsNullOrWhiteSpace(NameFilter))
            {
                try
                {
                    if (!Regex.IsMatch(card.Name ?? string.Empty, NameFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!card.Name?.Contains(NameFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(CardNumberFilter))
            {
                try
                {
                    if (!Regex.IsMatch(card.CardNumber ?? string.Empty, CardNumberFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!card.CardNumber?.Contains(CardNumberFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                        return false;
                }
            }

            switch (QtyOwnedFilter)
            {
                case "Owned":
                    if (card.QuantityOwned <= 0) return false;
                    break;
                case "Unowned":
                    if (card.QuantityOwned > 0) return false;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(RarityFilter))
            {
                try
                {
                    if (!Regex.IsMatch(card.Rarity ?? string.Empty, RarityFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!card.Rarity?.Contains(RarityFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(SetFilter))
            {
                try
                {
                    if (!Regex.IsMatch(card.Set ?? string.Empty, SetFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!card.Set?.Contains(SetFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(FormatFilter))
            {
                try
                {
                    if (!Regex.IsMatch(card.Format ?? string.Empty, FormatFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!card.Format?.Contains(FormatFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(ClassFilter))
            {
                try
                {
                    if (!Regex.IsMatch(card.Class ?? string.Empty, ClassFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!card.Class?.Contains(ClassFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(TypeFilter))
            {
                try
                {
                    if (!Regex.IsMatch(card.Type ?? string.Empty, TypeFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!card.Type?.Contains(TypeFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(TraitsFilter))
            {
                try
                {
                    if (!Regex.IsMatch(card.Traits ?? string.Empty, TraitsFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!card.Traits?.Contains(TraitsFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(TextFilter))
            {
                try
                {
                    if (!Regex.IsMatch(card.Text ?? string.Empty, TextFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!card.Text?.Contains(TextFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                        return false;
                }
            }

            return true;
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
    }
}