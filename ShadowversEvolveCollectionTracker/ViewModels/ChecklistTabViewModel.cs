using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using ShadowversEvolveCardTracker.Models;

namespace ShadowversEvolveCardTracker.ViewModels
{
    public class ChecklistTabViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<CombinedCardCount> _combinedCardCounts;
        private readonly ICollectionView _checklistView;

        public ICollectionView ChecklistView => _checklistView;

        private string? _checklistNameFilter;
        public string? ChecklistNameFilter
        {
            get => _checklistNameFilter;
            set
            {
                if (SetProperty(ref _checklistNameFilter, value))
                    _checklistView.Refresh();
            }
        }

        private string _checklistQtyFilter = "Both";
        public string ChecklistQtyFilter
        {
            get => _checklistQtyFilter;
            set
            {
                if (SetProperty(ref _checklistQtyFilter, value))
                    _checklistView.Refresh();
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
                    SelectedImageIndex = 0;
                    OnPropertyChanged(nameof(SelectedCombinedImage));
                    OnPropertyChanged(nameof(SelectedImageIndexDisplay));
                }
            }
        }

        private int _selectedImageIndex;
        public int SelectedImageIndex
        {
            get => _selectedImageIndex;
            set
            {
                if (SetProperty(ref _selectedImageIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedCombinedImage));
                    OnPropertyChanged(nameof(SelectedImageIndexDisplay));
                }
            }
        }

        public string? SelectedCombinedImage
        {
            get
            {
                if (SelectedCombinedCard == null) return null;
                var imgs = SelectedCombinedCard.Images;
                if (imgs == null || imgs.Count == 0) return null;
                if (SelectedImageIndex < 0) SelectedImageIndex = 0;
                if (SelectedImageIndex >= imgs.Count) SelectedImageIndex = imgs.Count - 1;
                return imgs.ElementAtOrDefault(SelectedImageIndex);
            }
        }

        public int SelectedImageIndexDisplay
        {
            get
            {
                var count = SelectedCombinedCard?.Images?.Count ?? 0;
                return count == 0 ? 0 : SelectedImageIndex + 1;
            }
        }

        public ICommand PrevImageCommand { get; }
        public ICommand NextImageCommand { get; }

        public ChecklistTabViewModel(ObservableCollection<CombinedCardCount> combinedCardCounts)
        {
            _combinedCardCounts = combinedCardCounts ?? throw new ArgumentNullException(nameof(combinedCardCounts));
            _checklistView = CollectionViewSource.GetDefaultView(_combinedCardCounts);
            _checklistView.Filter = ChecklistFilter;

            PrevImageCommand = new RelayCommand(() =>
            {
                if (SelectedCombinedCard == null) return Task.CompletedTask;
                if (SelectedImageIndex > 0) SelectedImageIndex--;
                return Task.CompletedTask;
            }, () => true);

            NextImageCommand = new RelayCommand(() =>
            {
                if (SelectedCombinedCard == null) return Task.CompletedTask;
                var count = SelectedCombinedCard.Images?.Count ?? 0;
                if (count > 0 && SelectedImageIndex < count - 1) SelectedImageIndex++;
                return Task.CompletedTask;
            }, () => true);
        }

        private bool ChecklistFilter(object? obj)
        {
            if (obj is not CombinedCardCount group) return false;
            var name = group.Name ?? string.Empty;

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
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
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