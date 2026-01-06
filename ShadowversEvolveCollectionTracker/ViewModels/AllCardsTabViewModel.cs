using System;
using System.Collections.ObjectModel;
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
        private readonly ICollectionView _filteredView;

        public ICollectionView FilteredCards => _filteredView;

        private CardData? _selectedCard;
        public CardData? SelectedCard
        {
            get => _selectedCard;
            set => SetProperty(ref _selectedCard, value);
        }

        // Filter properties
        private string? _nameFilter;
        public string? NameFilter
        {
            get => _nameFilter;
            set
            {
                if (SetProperty(ref _nameFilter, value))
                    _filteredView.Refresh();
            }
        }

        private string? _cardNumberFilter;
        public string? CardNumberFilter
        {
            get => _cardNumberFilter;
            set
            {
                if (SetProperty(ref _cardNumberFilter, value))
                    _filteredView.Refresh();
            }
        }

        private string? _rarityFilter;
        public string? RarityFilter
        {
            get => _rarityFilter;
            set
            {
                if (SetProperty(ref _rarityFilter, value))
                    _filteredView.Refresh();
            }
        }

        private string? _setFilter;
        public string? SetFilter
        {
            get => _setFilter;
            set
            {
                if (SetProperty(ref _setFilter, value))
                    _filteredView.Refresh();
            }
        }

        private string? _formatFilter;
        public string? FormatFilter
        {
            get => _formatFilter;
            set
            {
                if (SetProperty(ref _formatFilter, value))
                    _filteredView.Refresh();
            }
        }

        private string? _classFilter;
        public string? ClassFilter
        {
            get => _classFilter;
            set
            {
                if (SetProperty(ref _classFilter, value))
                    _filteredView.Refresh();
            }
        }

        private string? _typeFilter;
        public string? TypeFilter
        {
            get => _typeFilter;
            set
            {
                if (SetProperty(ref _typeFilter, value))
                    _filteredView.Refresh();
            }
        }

        private string? _traitsFilter;
        public string? TraitsFilter
        {
            get => _traitsFilter;
            set
            {
                if (SetProperty(ref _traitsFilter, value))
                    _filteredView.Refresh();
            }
        }

        private string? _textFilter;
        public string? TextFilter
        {
            get => _textFilter;
            set
            {
                if (SetProperty(ref _textFilter, value))
                    _filteredView.Refresh();
            }
        }

        private string _qtyOwnedFilter = "Both";
        public string QtyOwnedFilter
        {
            get => _qtyOwnedFilter;
            set
            {
                if (SetProperty(ref _qtyOwnedFilter, value))
                    _filteredView.Refresh();
            }
        }

        public AllCardsTabViewModel(ObservableCollection<CardData> allCards)
        {
            _allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
            _filteredView = CollectionViewSource.GetDefaultView(_allCards);
            _filteredView.Filter = FilterCard;
        }

        private bool FilterCard(object? obj)
        {
            if (obj is not CardData c) return false;

            static bool MatchWithRegexOrSubstring(string? value, string? pattern)
            {
                if (string.IsNullOrWhiteSpace(pattern)) return true;
                if (string.IsNullOrWhiteSpace(value)) return false;

                try
                {
                    return Regex.IsMatch(value, pattern!, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException)
                {
                    return value.Contains(pattern!, StringComparison.OrdinalIgnoreCase);
                }
            }

            if (!MatchWithRegexOrSubstring(c.Name, NameFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.CardNumber, CardNumberFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Rarity, RarityFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Set, SetFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Format, FormatFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Class, ClassFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Type, TypeFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Traits, TraitsFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Text, TextFilter)) return false;

            switch (QtyOwnedFilter)
            {
                case "Owned":
                    if (c.QuantityOwned <= 0) return false;
                    break;
                case "Unowned":
                    if (c.QuantityOwned != 0) return false;
                    break;
                default:
                    break;
            }

            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                return true;
            }
            return false;
        }
    }
}