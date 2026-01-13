using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using ShadowverseEvolveCardTracker.Models;
using ShadowverseEvolveCardTracker.Utilities;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    public class AddRemoveCardsViewModel : INotifyPropertyChanged
    {
        // small editable wrapper for each card row
        public class EditableCardEntry : INotifyPropertyChanged
        {
            public CardData Card { get; }

            private int _delta;
            public int Delta
            {
                get => _delta;
                set
                {
                    if (_delta != value)
                    {
                        _delta = value;
                        OnPropertyChanged();
                    }
                }
            }

            public EditableCardEntry(CardData c) => Card = c ?? throw new ArgumentNullException(nameof(c));

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private readonly ObservableCollection<CardData> _allCards;
        private readonly ObservableCollection<EditableCardEntry> _entries = new();
        private readonly ICollectionView _view;

        public ICollectionView View => _view;

        public ObservableCollection<RarityFilterItem> RarityFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> ClassFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> SetFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> OwnedFilters { get; } = new();

        // ICommand properties for header menu actions (wired from XAML)
        public ICommand SelectAllRarityFiltersCommand { get; private set; }
        public ICommand ClearAllRarityFiltersCommand { get; private set; }
        public ICommand SelectAllClassFiltersCommand { get; private set; }
        public ICommand ClearAllClassFiltersCommand { get; private set; }
        public ICommand SelectAllSetFiltersCommand { get; private set; }
        public ICommand ClearAllSetFiltersCommand { get; private set; }
        public ICommand SelectAllOwnedFiltersCommand { get; private set; }
        public ICommand ClearAllOwnedFiltersCommand { get; private set; }

        public bool UpdateWishlist
        {
            get => _updateWishlist;
            set
            {
                if (SetProperty(ref _updateWishlist, value))
                    _view.Refresh();
            }
        }
        private bool _updateWishlist;

        public string? NameFilter
        {
            get => _nameFilter;
            set
            {
                if (SetProperty(ref _nameFilter, value))
                    _view.Refresh();
            }
        }
        private string? _nameFilter;

        public string? CardNumberFilter
        {
            get => _cardNumberFilter;
            set
            {
                if (SetProperty(ref _cardNumberFilter, value))
                    _view.Refresh();
            }
        }
        private string? _cardNumberFilter;

        public AddRemoveCardsViewModel(ObservableCollection<CardData> allCards)
        {
            _allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));

            // build initial list
            foreach (var c in _allCards) _entries.Add(new EditableCardEntry(c));

            _view = CollectionViewSource.GetDefaultView(_entries);
            _view.Filter = Filter;

            InitializeRarityFilters();
            InitializeClassFilters();
            InitializeSetFilters();
            InitializeOwnedFilters();

            // Initialize ICommand properties so header menu buttons work
            SelectAllRarityFiltersCommand = new RelayCommand(
                execute: () => { SelectAllRarityFilters(); return Task.CompletedTask; },
                canExecute: () => RarityFilters.Count > 0);

            ClearAllRarityFiltersCommand = new RelayCommand(
                execute: () => { ClearAllRarityFilters(); return Task.CompletedTask; },
                canExecute: () => RarityFilters.Count > 0);

            SelectAllClassFiltersCommand = new RelayCommand(
                execute: () => { SelectAllClassFilters(); return Task.CompletedTask; },
                canExecute: () => ClassFilters.Count > 0);

            ClearAllClassFiltersCommand = new RelayCommand(
                execute: () => { ClearAllClassFilters(); return Task.CompletedTask; },
                canExecute: () => ClassFilters.Count > 0);

            SelectAllSetFiltersCommand = new RelayCommand(
                execute: () => { SelectAllSetFilters(); return Task.CompletedTask; },
                canExecute: () => SetFilters.Count > 0);

            ClearAllSetFiltersCommand = new RelayCommand(
                execute: () => { ClearAllSetFilters(); return Task.CompletedTask; },
                canExecute: () => SetFilters.Count > 0);

            SelectAllOwnedFiltersCommand = new RelayCommand(
                execute: () => { SelectAllOwnedFilters(); return Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            ClearAllOwnedFiltersCommand = new RelayCommand(
                execute: () => { ClearAllOwnedFilters(); return Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            if (_allCards is INotifyCollectionChanged incc)
                incc.CollectionChanged += AllCards_CollectionChanged;

            foreach (var e in _entries)
                SubscribeToCard(e.Card);
        }

        private void AllCards_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e?.OldItems != null)
            {
                foreach (CardData old in e.OldItems.OfType<CardData>())
                {
                    var entry = _entries.FirstOrDefault(x => x.Card.CardNumber == old.CardNumber);
                    if (entry != null) _entries.Remove(entry);
                    UnsubscribeFromCard(old);
                }
            }

            if (e?.NewItems != null)
            {
                foreach (CardData nw in e.NewItems.OfType<CardData>())
                {
                    _entries.Add(new EditableCardEntry(nw));
                    SubscribeToCard(nw);
                }
            }

            RefreshFiltersSources();
            _view.Refresh();
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
            if (string.IsNullOrEmpty(e?.PropertyName)) return;
            if (e.PropertyName == nameof(CardData.Name) ||
                e.PropertyName == nameof(CardData.CardNumber) ||
                e.PropertyName == nameof(CardData.Rarity) ||
                e.PropertyName == nameof(CardData.Set) ||
                e.PropertyName == nameof(CardData.Class) ||
                e.PropertyName == nameof(CardData.QuantityOwned))
            {
                RefreshFiltersSources();
                _view.Refresh();
            }
        }

        private bool Filter(object? obj)
        {
            if (obj is not EditableCardEntry entry) return true;
            var card = entry.Card;

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

            // Rarity filters
            if (RarityFilters.Count > 0)
            {
                var checkedCount = RarityFilters.Count(f => f.IsChecked);
                if (checkedCount != RarityFilters.Count)
                {
                    var parts = (card.Rarity ?? string.Empty).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());
                    if (!parts.Any()) return false;
                    bool anyMatch = parts.Any(p => RarityFilters.Any(f => f.IsChecked && string.Equals(f.Name, p, StringComparison.OrdinalIgnoreCase)));
                    if (!anyMatch) return false;
                }
            }

            // Class filters
            if (ClassFilters.Count > 0)
            {
                var checkedCount = ClassFilters.Count(f => f.IsChecked);
                if (checkedCount != ClassFilters.Count)
                {
                    var cls = (card.Class ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(cls)) return false;
                    bool any = ClassFilters.Any(f => f.IsChecked && string.Equals(f.Name, cls, StringComparison.OrdinalIgnoreCase));
                    if (!any) return false;
                }
            }

            // Set filters
            if (SetFilters.Count > 0)
            {
                var checkedCount = SetFilters.Count(f => f.IsChecked);
                if (checkedCount != SetFilters.Count)
                {
                    var parts = (card.Set ?? string.Empty).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());
                    if (!parts.Any()) return false;
                    bool anyMatch = parts.Any(p => SetFilters.Any(f => f.IsChecked && p.Contains(f.Name, StringComparison.OrdinalIgnoreCase)));
                    if (!anyMatch) return false;
                }
            }

            // Owned filters
            if (OwnedFilters.Count > 0)
            {
                var checkedCount = OwnedFilters.Count(f => f.IsChecked);
                if (checkedCount != OwnedFilters.Count)
                {
                    var ownedCategory = card.QuantityOwned > 0 ? QuantityOwnedHelper.Owned : QuantityOwnedHelper.Unowned;
                    bool any = OwnedFilters.Any(f => f.IsChecked && string.Equals(f.Name, ownedCategory, StringComparison.OrdinalIgnoreCase));
                    if (!any) return false;
                }
            }

            return true;
        }

        private void InitializeRarityFilters()
        {
            RarityFilters.Clear();
            foreach (var r in ShadowverseEvolveCardTracker.Constants.Rarities.AllRarities)
            {
                var item = new RarityFilterItem(r, true);
                item.PropertyChanged += FilterItem_PropertyChanged;
                RarityFilters.Add(item);
            }
        }

        private void InitializeClassFilters()
        {
            ClassFilters.Clear();
            foreach (var cls in ShadowverseEvolveCardTracker.Constants.Classes.AllClasses)
            {
                var item = new RarityFilterItem(cls, true);
                item.PropertyChanged += FilterItem_PropertyChanged;
                ClassFilters.Add(item);
            }
        }

        private void InitializeSetFilters()
        {
            SetFilters.Clear();
            var sets = _allCards.Select(c => (c.Set ?? string.Empty).Trim())
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                                .Select(s => ShadowverseEvolveCardTracker.Utilities.SetHelper.ExtractSetName(s))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

            foreach (var s in sets)
            {
                var item = new RarityFilterItem(s, true);
                item.PropertyChanged += FilterItem_PropertyChanged;
                SetFilters.Add(item);
            }
        }

        private void InitializeOwnedFilters()
        {
            OwnedFilters.Clear();
            foreach (var f in QuantityOwnedHelper.GetFilters())
            {
                f.PropertyChanged += FilterItem_PropertyChanged;
                OwnedFilters.Add(f);
            }
        }

        private void FilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
                _view.Refresh();
        }

        private void RefreshFiltersSources()
        {
            // Rebuild sets list (other filters remain static)
            InitializeSetFilters();
        }

        public void SelectAllRarityFilters() => SetAll(RarityFilters, true);
        public void ClearAllRarityFilters() => SetAll(RarityFilters, false);
        public void SelectAllClassFilters() => SetAll(ClassFilters, true);
        public void ClearAllClassFilters() => SetAll(ClassFilters, false);
        public void SelectAllSetFilters() => SetAll(SetFilters, true);
        public void ClearAllSetFilters() => SetAll(SetFilters, false);
        public void SelectAllOwnedFilters() => SetAll(OwnedFilters, true);
        public void ClearAllOwnedFilters() => SetAll(OwnedFilters, false);

        private void SetAll(IEnumerable<RarityFilterItem> items, bool isChecked)
        {
            foreach (var f in items) f.IsChecked = isChecked;
            _view.Refresh();
        }

        // Apply deltas to underlying CardData.QuantityOwned (clamped >= 0). Returns count changed
        public int ApplyChanges()
        {
            int updated = 0;
            foreach (var entry in _entries)
            {
                if (entry.Delta == 0) continue;
                var card = entry.Card;
                int newQty = Math.Max(0, card.QuantityOwned + entry.Delta);
                if (newQty != card.QuantityOwned)
                {
                    card.QuantityOwned = newQty;
                    updated++;
                }


                if (entry.Delta > 0 && card.IsWishlisted)
                {
                    card.WishlistDesiredQuantity = Math.Max(0, card.WishlistDesiredQuantity - entry.Delta);
                }

                entry.Delta = 0;
            }
            return updated;
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
    }
}