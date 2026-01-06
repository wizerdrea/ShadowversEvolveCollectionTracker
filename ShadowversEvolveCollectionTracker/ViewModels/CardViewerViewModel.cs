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
        private List<string> _images = new List<string>();
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
                    OnPropertyChanged(nameof(CurrentImage));
                    OnPropertyChanged(nameof(CurrentIndexDisplay));
                    ((RelayCommand)PrevImageCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)NextImageCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string? CurrentImage
        {
            get
            {
                if (_images == null || _images.Count == 0)
                    return null;

                if (_currentIndex < 0)
                    _currentIndex = 0;
                if (_currentIndex >= _images.Count)
                    _currentIndex = _images.Count - 1;

                return _images.ElementAtOrDefault(_currentIndex);
            }
        }

        public int ImageCount => _images?.Count ?? 0;

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
                _images = new List<string>();
                CardName = null;
            }
            else
            {
                _images = new List<string> { card.ImageFile };
                CardName = card.Name;
            }

            // Ensure UI shows 1-based index correctly even when CurrentIndex was already 0
            CurrentIndex = 0;
            OnPropertyChanged(nameof(ImageCount));
            OnPropertyChanged(nameof(ShowNavigation));
            OnPropertyChanged(nameof(CurrentImage));
            OnPropertyChanged(nameof(CurrentIndexDisplay));
            ((RelayCommand)PrevImageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NextImageCommand).RaiseCanExecuteChanged();
        }

        // Set multiple images from a CombinedCardCount
        public void SetCombinedCard(CombinedCardCount? combined)
        {
            if (combined == null)
            {
                _images = new List<string>();
                CardName = null;
            }
            else
            {
                _images = combined.Images ?? new List<string>();
                CardName = combined.Name;
            }

            // Ensure UI shows 1-based index correctly even when CurrentIndex was already 0
            CurrentIndex = 0;
            OnPropertyChanged(nameof(ImageCount));
            OnPropertyChanged(nameof(ShowNavigation));
            OnPropertyChanged(nameof(CurrentImage));
            OnPropertyChanged(nameof(CurrentIndexDisplay));
            ((RelayCommand)PrevImageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)NextImageCommand).RaiseCanExecuteChanged();
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