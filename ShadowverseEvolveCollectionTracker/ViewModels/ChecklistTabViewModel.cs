using ShadowverseEvolveCardTracker.Collections;
using ShadowverseEvolveCardTracker.Models;
using ShadowverseEvolveCardTracker.Utilities;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    public class ChecklistTabViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<CombinedCardCount> _combinedCardCounts;
        private readonly BulkObservableCollection<CombinedCardCount> _filiteredCombinedCards = new();
        private readonly ICollectionView _checklistView;

        public ICollectionView ChecklistView => _checklistView;

        private int _uniqueCardCount;
        public int UniqueCardCount
        {
            get => _uniqueCardCount;
            private set => SetProperty(ref _uniqueCardCount, value);
        }

        private string _ownedUniqueCountString;
        public string OwnedUniqueCountString
        {
            get => _ownedUniqueCountString;
            private set => SetProperty(ref _ownedUniqueCountString, value);
        }

        private string _ownedUniqueFullSetCountString;
        public string OwnedUniqueFullSetCountString
        {
            get => _ownedUniqueFullSetCountString;
            private set => SetProperty(ref _ownedUniqueFullSetCountString, value);
        }

        public CardViewerViewModel CardViewer { get; } = new CardViewerViewModel();

        private string? _checklistNameFilter;
        public string? ChecklistNameFilter
        {
            get => _checklistNameFilter;
            set
            {
                if (SetProperty(ref _checklistNameFilter, value))
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


        private bool _useOnlyCardsFromSelectedSets;
        public bool UseOnlyCardsFromSelectedSets
        {
            get => _useOnlyCardsFromSelectedSets;
            set
            {
                if (SetProperty(ref _useOnlyCardsFromSelectedSets, value))
                    SafeRefresh();
            }
        }

        private CombinedCardCount? _selectedCombinedCard;
        public CombinedCardCount? SelectedCombinedCard
        {
            get => _selectedCombinedCard;
            set
            {
                if (SetProperty(ref _selectedCombinedCard, value))
                {
                    CardViewer.SetCombinedCard(value);
                }
            }
        }

        private bool _isCalculating;
        public bool IsCalculating
        {
            get => _isCalculating;
            set => SetProperty(ref _isCalculating, value);
        }

        private string _calculatingMessage = "Calculating combined counts...";
        public string CalculatingMessage
        {
            get => _calculatingMessage;
            set => SetProperty(ref _calculatingMessage, value);
        }


        public ObservableCollection<RarityFilterItem> SetFilters { get; } = new();
        public ObservableCollection<RarityFilterItem> OwnedFilters { get; } = new();

        public ICommand SelectAllSetFiltersCommand { get; private set; }
        public ICommand ClearAllSetFiltersCommand { get; private set; }
        public ICommand SelectAllOwnedFiltersCommand { get; private set; }
        public ICommand ClearAllOwnedFiltersCommand { get; private set; }

        public ChecklistTabViewModel(ObservableCollection<CombinedCardCount> combinedCardCounts)
        {
            _combinedCardCounts = combinedCardCounts ?? throw new ArgumentNullException(nameof(combinedCardCounts));
            _checklistView = CollectionViewSource.GetDefaultView(_filiteredCombinedCards);
            _checklistView.Filter = ChecklistFilter;

            InitializeSetFilters();
            InitializeOwnedFilters();

            SelectAllSetFiltersCommand = new RelayCommand(
                execute: () => { SelectAllSetFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SetFilters.Count > 0);

            ClearAllSetFiltersCommand = new RelayCommand(
                execute: () => { ClearAllSetFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SetFilters.Count > 0);

            SelectAllOwnedFiltersCommand = new RelayCommand(
                execute: () => { SelectAllOwnedFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            ClearAllOwnedFiltersCommand = new RelayCommand(
                execute: () => { ClearAllOwnedFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            // subscribe to collection changes and nested card property changes
            if (_combinedCardCounts is INotifyCollectionChanged incc)
                incc.CollectionChanged += CombinedGroups_CollectionChanged;

            foreach (var g in _combinedCardCounts)
                SubscribeToGroup(g);

            // initialize counts so they are immediately available for bindings
            RecalculateCounts();
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

            // Recalculate counts whenever groups are added/removed
            RecomputeFilteredCombinedCards();
            RecalculateCounts();
            InitializeSetFilters();
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

        private void Card_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // If favorites or quantity change, the checklist visibility may need refresh
            if (e.PropertyName == nameof(CardData.IsFavorite) ||
                e.PropertyName == nameof(CardData.Name) ||
                e.PropertyName == nameof(CardData.Type))
            {
                SafeRefresh();
                return;
            }

            // When quantity changes (or a full refresh signaled) update counts and view
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(CardData.QuantityOwned))
            {
                SafeRefresh();
            }
        }

        private void FilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                SafeRefresh();
            }
        }

        private void RecomputeFilteredCombinedCards()
        {

            IEnumerable<CombinedCardCount> newCombinedCards = new List<CombinedCardCount>(); 

            if (UseOnlyCardsFromSelectedSets)
            {
                newCombinedCards = GetCombinedCountsForCurrentSets();
            }
            else
            {
                newCombinedCards = _combinedCardCounts;
            }

            _filiteredCombinedCards.Clear();
            _filiteredCombinedCards.AddRange(newCombinedCards);
        }

        private bool ChecklistFilter(object? obj)
        {
            if (obj is not CombinedCardCount group) return false;
            var name = group.Name ?? string.Empty;

            if (FavoritesOnly && !group.HasFavorite)
                return false;

            if (!string.IsNullOrWhiteSpace(ChecklistNameFilter))
            {
                try
                {
                    if (!Regex.IsMatch(name, ChecklistNameFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!name.Contains(ChecklistNameFilter!, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            // set filters
            if (SetFilters.Count > 0)
            {
                var checkedCount = SetFilters.Count(f => f.IsChecked);
                if (checkedCount != SetFilters.Count)
                {
                    var cardSets = group.Sets ?? new List<string>();
                    if (!cardSets.Any()) return false;

                    bool anyMatch = cardSets.Any(cp =>
                        SetFilters.Any(f => f.IsChecked &&
                            cp.Contains(f.Name, StringComparison.OrdinalIgnoreCase)));

                    if (!anyMatch) return false;
                }
            }

            // owned filters
            if (OwnedFilters.Count > 0)
            {
                var checkedCount = OwnedFilters.Count(f => f.IsChecked);
                if (checkedCount != OwnedFilters.Count)
                {
                    var ownedCategory = group.TotalQuantityOwned > 0 ? QuantityOwnedHelper.Owned : QuantityOwnedHelper.Unowned;

                    bool anyMatch = OwnedFilters.Any(f => f.IsChecked &&
                        string.Equals(f.Name, ownedCategory, StringComparison.OrdinalIgnoreCase));

                    if (!anyMatch) return false;
                }
            }

            return true;
        }

        private void InitializeSetFilters()
        {
            try
            {
                SetFilters.Clear();

                var sets = _combinedCardCounts
                    .SelectMany(c => c.Sets)
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

        private void RecalculateCounts()
        {
            try
            {
                var filteredCards = _checklistView.Cast<CombinedCardCount>();

                UniqueCardCount = filteredCards.Count();

                int ownedUniqueCount = filteredCards.Count(g => g.TotalQuantityOwned > 0);
                int ownedUniqueFullSetCount = filteredCards.Count(g => g.TotalQuantityOwned >= g.AllCards.First().CopiesNeededForPlayset);


                OwnedUniqueCountString = GeneratePercentString(ownedUniqueCount, UniqueCardCount);
                OwnedUniqueFullSetCountString = GeneratePercentString(ownedUniqueFullSetCount, UniqueCardCount);
            }
            catch
            {
                // defensive: if something unexpected happens, fall back to zeroes
                UniqueCardCount = 0;
                OwnedUniqueCountString = "Error";
                OwnedUniqueFullSetCountString = "Error";
            }
        }

        private List<CombinedCardCount> GetCombinedCountsForCurrentSets()
        {
            var selectedSets = SetFilters.Where(f => f.IsChecked).Select(f => f.Name);

            return _combinedCardCounts.Where(c => c.Sets.Any(s => contains(selectedSets, s)))
                                      .Select(c => NewCountForSelectedSets(c, selectedSets)).ToList();
        }

        private CombinedCardCount NewCountForSelectedSets(CombinedCardCount origonal, IEnumerable<string> selectedSets)
        {
            var cardsInSelectedSets = origonal.AllCards.Where(c => contains(selectedSets, c.Set.Trim()));

            return new CombinedCardCount(cardsInSelectedSets);
        }

        private bool contains(IEnumerable<string> collection, string item)
        {
            return collection.Any(c => item.Contains(c, StringComparison.OrdinalIgnoreCase));
        }

        private string GeneratePercentString(int of, int from)
        {
            if (from == 0) return "Error";

            return $"{of}/{from} ({((double)of)/from:P0})";
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

        private void SafeRefresh()
        {
            if (_checklistView is IEditableCollectionView iecv &&
                (iecv.IsAddingNew || iecv.IsEditingItem))
                return;

            RecomputeFilteredCombinedCards();
            _checklistView.Refresh();
            RecalculateCounts();
        }
    }
}