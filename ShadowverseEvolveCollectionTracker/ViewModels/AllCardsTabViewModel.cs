using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ShadowverseEvolveCardTracker.Constants;
using ShadowverseEvolveCardTracker.Models;
using ShadowverseEvolveCardTracker.Utilities;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    public class AllCardsTabViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<CardData> _allCards;
        private readonly ICollectionView _filteredCards;

        public ICollectionView FilteredCards => _filteredCards;
        public CardViewerViewModel CardViewer { get; } = new CardViewerViewModel();

        private int _filteredCardCount;
        public int FilteredCardCount
        {
            get => _filteredCardCount;
            set
            {
                if (SetProperty(ref _filteredCardCount, value))
                    SafeRefresh();
            }
        }

        private string? _nameFilter;
        public string? NameFilter
        {
            get => _nameFilter;
            set
            {
                if (SetProperty(ref _nameFilter, value))
                    SafeRefresh();
            }
        }

        private string? _cardNumberFilter;
        public string? CardNumberFilter
        {
            get => _cardNumberFilter;
            set
            {
                if (SetProperty(ref _cardNumberFilter, value))
                    SafeRefresh();
            }
        }

        private string? _textFilter;
        public string? TextFilter
        {
            get => _textFilter;
            set
            {
                if (SetProperty(ref _textFilter, value))
                    SafeRefresh();
            }
        }

        private bool _favoritesOnly;
        public bool FavoritesOnly
        {
            get => _favoritesOnly;
            set
            {
                if (SetProperty(ref _favoritesOnly, value))
                    SafeRefresh();
            }
        }

        private bool _wishlistedOnly;
        public bool WishlistedOnly
        {
            get => _wishlistedOnly;
            set
            {
                if (SetProperty(ref _wishlistedOnly, value))
                    SafeRefresh();
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

        #region Filter collections (mimic DeckBuilder Available Cards)

        public ObservableCollection<RarityFilterItem> RarityFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> TypeFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> ClassFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> SetFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> CostFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> OwnedFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> TraitsFilters { get; } = new();

        // Commands for header context menus
        public ICommand SelectAllRarityFiltersCommand { get; private set; }
        public ICommand ClearAllRarityFiltersCommand { get; private set; }
        public ICommand SelectAllTypeFiltersCommand { get; private set; }
        public ICommand ClearAllTypeFiltersCommand { get; private set; }
        public ICommand SelectAllClassFiltersCommand { get; private set; }
        public ICommand ClearAllClassFiltersCommand { get; private set; }
        public ICommand SelectAllSetFiltersCommand { get; private set; }
        public ICommand ClearAllSetFiltersCommand { get; private set; }
        public ICommand SelectAllCostFiltersCommand { get; private set; }
        public ICommand ClearAllCostFiltersCommand { get; private set; }
        public ICommand SelectAllOwnedFiltersCommand { get; private set; }
        public ICommand ClearAllOwnedFiltersCommand { get; private set; }
        public ICommand SelectAllTraitFiltersCommand { get; private set; }
        public ICommand ClearAllTraitFiltersCommand { get; private set; }

        #endregion

        public AllCardsTabViewModel(ObservableCollection<CardData> allCards)
        {
            _allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
            _filteredCards = CollectionViewSource.GetDefaultView(_allCards);
            _filteredCards.Filter = FilterCard;

            // initialize filter sets and commands
            InitializeRarityFilters();
            InitializeTypeFilters();
            InitializeSetFilters();
            InitializeClassFilters();
            InitializeCostFilters();
            InitializeOwnedFilters();
            InitializeTraitsFilters();

            SelectAllRarityFiltersCommand = new RelayCommand(
                execute: () => { SelectAllRarityFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => RarityFilters.Count > 0);

            ClearAllRarityFiltersCommand = new RelayCommand(
                execute: () => { ClearAllRarityFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => RarityFilters.Count > 0);

            SelectAllTypeFiltersCommand = new RelayCommand(
                execute: () => { SelectAllTypeFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => TypeFilters.Count > 0);

            ClearAllTypeFiltersCommand = new RelayCommand(
                execute: () => { ClearAllTypeFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => TypeFilters.Count > 0);

            SelectAllClassFiltersCommand = new RelayCommand(
                execute: () => { SelectAllClassFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => ClassFilters.Count > 0);

            ClearAllClassFiltersCommand = new RelayCommand(
                execute: () => { ClearAllClassFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => ClassFilters.Count > 0);

            SelectAllSetFiltersCommand = new RelayCommand(
                execute: () => { SelectAllSetFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SetFilters.Count > 0);

            ClearAllSetFiltersCommand = new RelayCommand(
                execute: () => { ClearAllSetFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SetFilters.Count > 0);

            SelectAllCostFiltersCommand = new RelayCommand(
                execute: () => { SelectAllCostFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CostFilters.Count > 0);

            ClearAllCostFiltersCommand = new RelayCommand(
                execute: () => { ClearAllCostFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CostFilters.Count > 0);

            SelectAllOwnedFiltersCommand = new RelayCommand(
                execute: () => { SelectAllOwnedFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            ClearAllOwnedFiltersCommand = new RelayCommand(
                execute: () => { ClearAllOwnedFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            SelectAllTraitFiltersCommand = new RelayCommand(
                execute: () => { SelectAllTraits(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => TraitsFilters.Count > 0);

            ClearAllTraitFiltersCommand = new RelayCommand(
                execute: () => { ClearAllTraits(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => TraitsFilters.Count > 0);

            // subscribe to collection changes and card property changes so filter updates when card properties change
            if (_allCards is INotifyCollectionChanged incc)
                incc.CollectionChanged += AllCards_CollectionChanged;

            foreach (var c in _allCards)
                SubscribeToCard(c);

            SafeRefresh();
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

            // rebuild trait/cost/owned/range lists when the card set changes
            InitializeTraitsFilters();
            InitializeCostFilters();
            InitializeOwnedFilters();
            InitializeRarityFilters();
            InitializeTypeFilters();
            InitializeSetFilters();
            InitializeClassFilters();
            SafeRefresh();
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
                e.PropertyName == nameof(CardData.Cost) ||
                e.PropertyName == nameof(CardData.WishlistDesiredQuantity))
            {
                SafeRefresh();
            }
        }

        private void SafeRefresh()
        {
            // If the view is in the middle of an edit/new operation don't refresh.
            if (_filteredCards is IEditableCollectionView iecv &&
                (iecv.IsAddingNew || iecv.IsEditingItem))
                return;

            // Ensure Refresh happens on the UI thread to avoid cross-thread failures that can
            // result in the view becoming empty.
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => _filteredCards.Refresh());
            }
            else
            {
                _filteredCards.Refresh();
            }

            FilteredCardCount = _filteredCards.Cast<object>().Count();
        }

        private bool FilterCard(object? obj)
        {
            // CollectionViews sometimes pass placeholders or non-data items; don't exclude
            // those by mistake. Only filter CardData instances explicitly.
            if (obj is not CardData card) return true;

            try
            {
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

                // apply traits filters (multi-select) if defined (and not all checked)
                if (TraitsFilters.Count > 0)
                {
                    var checkedCount = TraitsFilters.Count(f => f.IsChecked);
                    if (checkedCount != TraitsFilters.Count) // only filter when some are unchecked
                    {
                        var cardParts = (card.Traits ?? string.Empty)
                            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim());
                        if (!cardParts.Any()) return false;

                        bool anyMatch = cardParts.Any(cp =>
                            TraitsFilters.Any(f => f.IsChecked &&
                                string.Equals(f.Name, cp, StringComparison.OrdinalIgnoreCase)));

                        if (!anyMatch) return false;
                    }
                }

                // apply rarity filters if defined (and not all checked)
                if (RarityFilters.Count > 0)
                {
                    var checkedCount = RarityFilters.Count(f => f.IsChecked);
                    if (checkedCount != RarityFilters.Count)
                    {
                        var cardParts = (card.Rarity ?? string.Empty)
                            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim());
                        if (!cardParts.Any()) return false;

                        bool anyMatch = cardParts.Any(cp =>
                            RarityFilters.Any(f => f.IsChecked &&
                                string.Equals(f.Name, cp, StringComparison.OrdinalIgnoreCase)));

                        if (!anyMatch) return false;
                    }
                }

                // type filters
                if (TypeFilters.Count > 0)
                {
                    var checkedCount = TypeFilters.Count(f => f.IsChecked);
                    if (checkedCount != TypeFilters.Count)
                    {
                        var cardParts = (card.Type ?? string.Empty)
                            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim());
                        if (!cardParts.Any()) return false;

                        bool anyMatch = cardParts.Any(cp =>
                            TypeFilters.Any(f => f.IsChecked &&
                                string.Equals(f.Name, cp, StringComparison.OrdinalIgnoreCase)));

                        if (!anyMatch) return false;
                    }
                }

                // class filters
                if (ClassFilters.Count > 0)
                {
                    var checkedCount = ClassFilters.Count(f => f.IsChecked);
                    if (checkedCount != ClassFilters.Count)
                    {
                        var cardClass = (card.Class ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(cardClass)) return false;

                        bool anyMatch = ClassFilters.Any(f => f.IsChecked &&
                            string.Equals(f.Name, cardClass, StringComparison.OrdinalIgnoreCase));

                        if (!anyMatch) return false;
                    }
                }

                // set filters
                if (SetFilters.Count > 0)
                {
                    var checkedCount = SetFilters.Count(f => f.IsChecked);
                    if (checkedCount != SetFilters.Count)
                    {
                        var cardParts = (card.Set ?? string.Empty)
                            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim());
                        if (!cardParts.Any()) return false;

                        bool anyMatch = cardParts.Any(cp =>
                            SetFilters.Any(f => f.IsChecked &&
                                cp.Contains(f.Name, StringComparison.OrdinalIgnoreCase)));

                        if (!anyMatch) return false;
                    }
                }

                // cost filters
                if (CostFilters.Count > 0)
                {
                    var checkedCount = CostFilters.Count(f => f.IsChecked);
                    if (checkedCount != CostFilters.Count)
                    {
                        var cardCost = (card.Cost ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(cardCost)) return false;

                        bool anyMatch = CostFilters.Any(f => f.IsChecked &&
                            string.Equals(f.Name, cardCost, StringComparison.OrdinalIgnoreCase));

                        if (!anyMatch) return false;
                    }
                }

                // owned filters
                if (OwnedFilters.Count > 0)
                {
                    var checkedCount = OwnedFilters.Count(f => f.IsChecked);
                    if (checkedCount != OwnedFilters.Count)
                    {
                        var ownedCategory = card.QuantityOwned > 0 ? QuantityOwnedHelper.Owned : QuantityOwnedHelper.Unowned;

                        bool anyMatch = OwnedFilters.Any(f => f.IsChecked &&
                            string.Equals(f.Name, ownedCategory, StringComparison.OrdinalIgnoreCase));

                        if (!anyMatch) return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Defensive: if an unexpected exception occurs while filtering, log and
                // include the item rather than filtering everything out.
                System.Diagnostics.Debug.WriteLine($"AllCardsTabViewModel.FilterCard error: {ex}");
                return true;
            }
        }

        #region Filter initialization & helpers

        private void InitializeRarityFilters()
        {
            try
            {
                RarityFilters.Clear();
                foreach (var rarity in Rarities.AllRarities)
                {
                    var item = new RarityFilterItem(rarity, isChecked: true);
                    item.PropertyChanged += FilterItem_PropertyChanged;
                    RarityFilters.Add(item);
                }
            }
            catch
            {
                // swallow
            }
        }

        private void SelectAllRarityFilters()
        {
            foreach (var f in RarityFilters) f.IsChecked = true;
            SafeRefresh();
        }

        private void ClearAllRarityFilters()
        {
            foreach (var f in RarityFilters) f.IsChecked = false;
            SafeRefresh();
        }

        private void InitializeTypeFilters()
        {
            try
            {
                TypeFilters.Clear();
                foreach (var t in CardTypes.AllCardTypes)
                {
                    var item = new RarityFilterItem(t, isChecked: true);
                    item.PropertyChanged += FilterItem_PropertyChanged;
                    TypeFilters.Add(item);
                }
            }
            catch { }
        }

        private void SelectAllTypeFilters()
        {
            foreach (var f in TypeFilters) f.IsChecked = true;
            SafeRefresh();
        }

        private void ClearAllTypeFilters()
        {
            foreach (var f in TypeFilters) f.IsChecked = false;
            SafeRefresh();
        }

        private void InitializeClassFilters()
        {
            try
            {
                ClassFilters.Clear();
                var classes = Classes.AllClasses;
                foreach (var cls in classes)
                {
                    var item = new RarityFilterItem(cls, isChecked: true);
                    item.PropertyChanged += FilterItem_PropertyChanged;
                    ClassFilters.Add(item);
                }
            }
            catch { }
        }

        private void SelectAllClassFilters()
        {
            foreach (var f in ClassFilters) f.IsChecked = true;
            SafeRefresh();
        }

        private void ClearAllClassFilters()
        {
            foreach (var f in ClassFilters) f.IsChecked = false;
            SafeRefresh();
        }

        private void InitializeSetFilters()
        {
            try
            {
                SetFilters.Clear();

                var sets = _allCards
                    .Select(c => (c.Set ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var s in sets)
                {
                    var item = new RarityFilterItem(SetHelper.ExtractSetName(s), isChecked: true);
                    item.PropertyChanged += FilterItem_PropertyChanged;
                    SetFilters.Add(item);
                }
            }
            catch { }
        }

        private void SelectAllSetFilters()
        {
            foreach (var f in SetFilters) f.IsChecked = true;
            SafeRefresh();
        }

        private void ClearAllSetFilters()
        {
            foreach (var f in SetFilters) f.IsChecked = false;
            SafeRefresh();
        }

        private void InitializeCostFilters()
        {
            try
            {
                CostFilters.Clear();

                var costs = _allCards
                    .Select(c => (c.Cost ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s =>
                    {
                        if (int.TryParse(s, out var n)) return (n, s);
                        return (int.MaxValue, s);
                    })
                    .ToList();

                foreach (var cost in costs)
                {
                    var item = new RarityFilterItem(cost, isChecked: true);
                    item.PropertyChanged += FilterItem_PropertyChanged;
                    CostFilters.Add(item);
                }
            }
            catch { }
        }

        private void SelectAllCostFilters()
        {
            foreach (var f in CostFilters) f.IsChecked = true;
            SafeRefresh();
        }

        private void ClearAllCostFilters()
        {
            foreach (var f in CostFilters) f.IsChecked = false;
            SafeRefresh();
        }

        private void InitializeOwnedFilters()
        {
            try
            {
                OwnedFilters.Clear();

                foreach (var f in QuantityOwnedHelper.GetFilters())
                {
                    f.PropertyChanged += FilterItem_PropertyChanged;
                    OwnedFilters.Add(f);
                }
            }
            catch { }
        }

        private void SelectAllOwnedFilters()
        {
            foreach (var f in OwnedFilters) f.IsChecked = true;
            SafeRefresh();
        }

        private void ClearAllOwnedFilters()
        {
            foreach (var f in OwnedFilters) f.IsChecked = false;
            SafeRefresh();
        }

        private void SelectAllTraits()
        {
            foreach (var f in TraitsFilters) f.IsChecked = true;
            SafeRefresh();
        }

        private void ClearAllTraits()
        {
            foreach (var f in TraitsFilters) f.IsChecked = false;
            SafeRefresh();
        }

        private void InitializeTraitsFilters()
        {
            try
            {
                TraitsFilters.Clear();

                var traits = _allCards
                    .SelectMany(c => (c.Traits ?? string.Empty).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var t in traits)
                {
                    var item = new RarityFilterItem(t, isChecked: true);
                    item.PropertyChanged += FilterItem_PropertyChanged;
                    TraitsFilters.Add(item);
                }
            }
            catch { }
        }

        private void FilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                SafeRefresh();
            }
        }

        #endregion

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