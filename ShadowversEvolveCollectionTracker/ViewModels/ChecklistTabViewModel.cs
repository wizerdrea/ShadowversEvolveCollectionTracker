using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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