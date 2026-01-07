using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Data;
using ShadowversEvolveCardTracker.Models;

namespace ShadowversEvolveCardTracker.ViewModels
{
    public class ChecklistTabViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<CombinedCardCount> _combinedCardCounts;
        private readonly ICollectionView _checklistView;

        public ICollectionView ChecklistView => _checklistView;
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
                e.PropertyName == nameof(CardData.QuantityOwned) ||
                e.PropertyName == nameof(CardData.Name) ||
                e.PropertyName == nameof(CardData.Type))
            {
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