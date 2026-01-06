using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ShadowversEvolveCardTracker.Models;

namespace ShadowversEvolveCardTracker.ViewModels
{
    public class CardViewerViewModel : INotifyPropertyChanged
    {
        private List<CardData> _cards = new();
        private int _currentIndex;
        private string? _cardName;

        public ICommand PrevImageCommand { get; }
        public ICommand NextImageCommand { get; }

        public CardViewerViewModel()
        {
            PrevImageCommand = new RelayCommand(
                execute: () =>
                {
                    if (CurrentIndex > 0)
                        CurrentIndex--;
                    return Task.CompletedTask;
                },
                canExecute: () => CurrentIndex > 0 && ImageCount > 1);

            NextImageCommand = new RelayCommand(
                execute: () =>
                {
                    if (CurrentIndex < ImageCount - 1)
                        CurrentIndex++;
                    return Task.CompletedTask;
                },
                canExecute: () => CurrentIndex < ImageCount - 1 && ImageCount > 1);
        }

        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                if (SetProperty(ref _currentIndex, value))
                {
                    OnPropertyChanged(nameof(CurrentCard));
                    OnPropertyChanged(nameof(CurrentImage));
                    OnPropertyChanged(nameof(CurrentIndexDisplay));
                    ((RelayCommand)PrevImageCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)NextImageCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public CardData? CurrentCard
        {
            get
            {
                if (_cards == null || _cards.Count == 0) return null;
                if (_currentIndex < 0) _currentIndex = 0;
                if (_currentIndex >= _cards.Count) _currentIndex = _cards.Count - 1;
                return _cards.ElementAtOrDefault(_currentIndex);
            }
        }

        public string? CurrentImage => CurrentCard?.ImageFile;

        public int ImageCount => _cards?.Count ?? 0;

        public int CurrentIndexDisplay => ImageCount == 0 ? 0 : CurrentIndex + 1;

        public bool ShowNavigation => ImageCount > 1;

        public string? CardName
        {
            get => _cardName;
            private set => SetProperty(ref _cardName, value);
        }

        // Set a single card
        public void SetCard(CardData? card)
        {
            if (card == null)
            {
                _cards = new List<CardData>();
                CardName = null;
            }
            else
            {
                _cards = new List<CardData> { card };
                CardName = card.Name;
            }

            CurrentIndex = 0;
            OnPropertyChanged(nameof(ImageCount));
            OnPropertyChanged(nameof(ShowNavigation));
            OnPropertyChanged(nameof(CurrentCard));
            OnPropertyChanged(nameof(CurrentImage));
            OnPropertyChanged(nameof(CurrentIndexDisplay));
            ((RelayCommand)PrevImageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NextImageCommand).RaiseCanExecuteChanged();
        }

        // Set multiple cards from an enumerable of CardData
        public void SetCards(IEnumerable<CardData>? cards)
        {
            if (cards == null)
            {
                _cards = new List<CardData>();
                CardName = null;
            }
            else
            {
                _cards = cards.ToList();
                // show name of the currently displayed card if available, otherwise first card name
                CardName = CurrentCard?.Name ?? _cards.FirstOrDefault()?.Name;
            }

            CurrentIndex = 0;
            OnPropertyChanged(nameof(ImageCount));
            OnPropertyChanged(nameof(ShowNavigation));
            OnPropertyChanged(nameof(CurrentCard));
            OnPropertyChanged(nameof(CurrentImage));
            OnPropertyChanged(nameof(CurrentIndexDisplay));
            ((RelayCommand)PrevImageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NextImageCommand).RaiseCanExecuteChanged();
        }

        // Convenience: keep existing name used by other code
        public void SetCombinedCard(CombinedCardCount? combined)
        {
            SetCards(combined?.Cards);
            if (combined != null)
                CardName = combined.Name;
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