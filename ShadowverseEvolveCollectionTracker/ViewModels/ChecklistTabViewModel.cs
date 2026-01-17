using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Data;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    public class ChecklistTabViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<CombinedCardCount> _combinedCardCounts;
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

        private string _checklistQtyFilter = "Both";
        public string ChecklistQtyFilter
        {
            get => _checklistQtyFilter;
            set
            {
                if (SetProperty(ref _checklistQtyFilter, value))
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

        public ChecklistTabViewModel(ObservableCollection<CombinedCardCount> combinedCardCounts)
        {
            _combinedCardCounts = combinedCardCounts ?? throw new ArgumentNullException(nameof(combinedCardCounts));
            _checklistView = CollectionViewSource.GetDefaultView(_combinedCardCounts);
            _checklistView.Filter = ChecklistFilter;

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
            RecalculateCounts();
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
                RecalculateCounts();
                SafeRefresh();
            }
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

            switch (ChecklistQtyFilter)
            {
                case "Owned":
                    return group.TotalQuantityOwned > 0;
                case "Unowned":
                    return group.TotalQuantityOwned == 0;
                default:
                    return true;
            }
        }

        private void RecalculateCounts()
        {
            try
            {
                UniqueCardCount = _combinedCardCounts.Count;

                int ownedUniqueCount = _combinedCardCounts.Count(g => g.TotalQuantityOwned > 0);
                int ownedUniqueFullSetCount = _combinedCardCounts.Count(g => g.TotalQuantityOwned >= g.AllCards.First().CopiesNeededForPlayset);


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
            _checklistView.Refresh();
        }
    }
}