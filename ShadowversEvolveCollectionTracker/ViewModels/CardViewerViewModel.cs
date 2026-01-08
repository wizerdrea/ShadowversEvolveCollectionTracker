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
        private CardData? _subscribedCard;

        // history of previously-viewed card lists + index
        private readonly Stack<HistoryEntry> _history = new();
        private bool _suppressHistoryPush;

        public ICommand PrevImageCommand { get; }
        public ICommand NextImageCommand { get; }
        public ICommand IncreaseWishlistCommand { get; }
        public ICommand DecreaseWishlistCommand { get; }
        public ICommand ViewRelatedCardsCommand { get; }
        public ICommand BackCommand { get; }

        // Delegate to request related cards from parent (AllCards collection)
        public Action<CardData>? RequestRelatedCards { get; set; }

        private record HistoryEntry(List<CardData> Cards, int Index);

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

            IncreaseWishlistCommand = new RelayCommand(
                execute: () =>
                {
                    if (CurrentCard != null)
                        CurrentCard.WishlistDesiredQuantity++;
                    return Task.CompletedTask;
                },
                canExecute: () => CurrentCard != null);

            DecreaseWishlistCommand = new RelayCommand(
                execute: () =>
                {
                    if (CurrentCard != null)
                        CurrentCard.WishlistDesiredQuantity = Math.Max(0, CurrentCard.WishlistDesiredQuantity - 1);
                    return Task.CompletedTask;
                },
                canExecute: () => CurrentCard != null && (CurrentCard?.WishlistDesiredQuantity ?? 0) > 0);

            ViewRelatedCardsCommand = new RelayCommand(
                execute: () =>
                {
                    if (CurrentCard != null)
                        RequestRelatedCards?.Invoke(CurrentCard);
                    return Task.CompletedTask;
                },
                canExecute: () => CurrentCard != null && (CurrentCard?.RelatedCards?.Count ?? 0) > 0);

            BackCommand = new RelayCommand(
                execute: () =>
                {
                    RestoreHistory();
                    return Task.CompletedTask;
                },
                canExecute: () => CanGoBack);
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
                    UpdateCurrentCardSubscription(CurrentCard);
                    ((RelayCommand)IncreaseWishlistCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DecreaseWishlistCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ViewRelatedCardsCommand).RaiseCanExecuteChanged();
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

        // Wishlist view helpers
        public int WishlistQuantity => CurrentCard?.WishlistDesiredQuantity ?? 0;
        public bool IsWishlisted => WishlistQuantity > 0;

        // Related cards helper
        public bool HasRelatedCards => (CurrentCard?.RelatedCards?.Count ?? 0) > 0;
        public int RelatedCardsCount => CurrentCard?.RelatedCards?.Count ?? 0;

        // Back navigation helper
        public bool CanGoBack => _history.Count > 0;

        // Set a single card
        public void SetCard(CardData? card)
        {
            PushHistoryIfNeeded();

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
            UpdateCurrentCardSubscription(CurrentCard);
            NotifyHistoryChanged();
        }

        // Set multiple cards from an enumerable of CardData
        public void SetCards(IEnumerable<CardData>? cards)
        {
            PushHistoryIfNeeded();

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
            UpdateCurrentCardSubscription(CurrentCard);
            NotifyHistoryChanged();
        }

        // Convenience: keep existing name used by other code
        public void SetCombinedCard(CombinedCardCount? combined)
        {
            SetCards(combined?.Cards);
            if (combined != null)
                CardName = combined.Name;
        }

        private void PushHistoryIfNeeded()
        {
            if (_suppressHistoryPush) return;
            if (_cards == null || _cards.Count == 0) return;

            // push a shallow copy of the current list and index
            var copy = new List<CardData>(_cards);
            _history.Push(new HistoryEntry(copy, CurrentIndex));

            // keep history reasonable (optional cap)
            const int MaxHistory = 100;
            if (_history.Count > MaxHistory)
            {
                // remove oldest entry by rebuilding stack without the oldest
                var temp = new Stack<HistoryEntry>();
                while (_history.Count > 1)
                    temp.Push(_history.Pop());
                _history.Pop(); // remove the oldest
                while (temp.Count > 0)
                    _history.Push(temp.Pop());
            }

            NotifyHistoryChanged();
        }

        private void RestoreHistory()
        {
            if (!CanGoBack) return;

            var entry = _history.Pop();
            try
            {
                _suppressHistoryPush = true;
                SetCards(entry.Cards);
                CurrentIndex = Math.Clamp(entry.Index, 0, _cards.Count - 1);
            }
            finally
            {
                _suppressHistoryPush = false;
            }

            NotifyHistoryChanged();
        }

        private void NotifyHistoryChanged()
        {
            OnPropertyChanged(nameof(CanGoBack));
            ((RelayCommand?)BackCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateCurrentCardSubscription(CardData? newCard)
        {
            if (_subscribedCard == newCard) return;

            if (_subscribedCard != null)
                _subscribedCard.PropertyChanged -= SubscribedCard_PropertyChanged;

            _subscribedCard = newCard;

            if (_subscribedCard != null)
                _subscribedCard.PropertyChanged += SubscribedCard_PropertyChanged;

            OnPropertyChanged(nameof(WishlistQuantity));
            OnPropertyChanged(nameof(IsWishlisted));
            OnPropertyChanged(nameof(HasRelatedCards));
            OnPropertyChanged(nameof(RelatedCardsCount));
            ((RelayCommand)IncreaseWishlistCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DecreaseWishlistCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ViewRelatedCardsCommand).RaiseCanExecuteChanged();
        }

        private void SubscribedCard_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Bubble wishlist changes to the view
            if (e.PropertyName == nameof(CardData.WishlistDesiredQuantity) || string.IsNullOrEmpty(e.PropertyName))
            {
                OnPropertyChanged(nameof(WishlistQuantity));
                OnPropertyChanged(nameof(IsWishlisted));
                ((RelayCommand)DecreaseWishlistCommand).RaiseCanExecuteChanged();
            }

            // Bubble related cards changes to the view
            if (e.PropertyName == nameof(CardData.RelatedCards) || string.IsNullOrEmpty(e.PropertyName))
            {
                OnPropertyChanged(nameof(HasRelatedCards));
                OnPropertyChanged(nameof(RelatedCardsCount));
                ((RelayCommand)ViewRelatedCardsCommand).RaiseCanExecuteChanged();
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