using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using ShadowverseEvolveCardTracker.Models;
using ShadowverseEvolveCardTracker.Views;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    public class DeckBuilderViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<CardData> _allCards;
        private readonly ObservableCollection<Deck> _decks = new();
        private readonly ICollectionView _standardDecks;
        private readonly ICollectionView _gloryfinderDecks;
        private readonly ICollectionView _crossCraftDecks;
        private readonly ICollectionView _validCards;

        private Deck? _currentDeck;
        private CardData? _selectedCard;
        private bool _isEditingDeck;
        private DeckEntry? _selectedMainDeckEntry;
        private DeckEntry? _selectedEvolveDeckEntry;

        public ICollectionView StandardDecks => _standardDecks;
        public ICollectionView GloryfinderDecks => _gloryfinderDecks;
        public ICollectionView CrossCraftDecks => _crossCraftDecks;
        public ICollectionView ValidCards => _validCards;

        public CardViewerViewModel CardViewer { get; } = new();

        public ObservableCollection<DeckEntry> MainDeckList { get; } = new();
        public ObservableCollection<DeckEntry> EvolveDeckList { get; } = new();

        public ICommand CreateDeckCommand { get; }
        public ICommand EditDeckCommand { get; }
        public ICommand DeleteDeckCommand { get; }
        public ICommand BackToDeckListCommand { get; }
        public ICommand AddToMainDeckCommand { get; }
        public ICommand RemoveFromMainDeckCommand { get; }
        public ICommand AddToEvolveDeckCommand { get; }
        public ICommand RemoveFromEvolveDeckCommand { get; }
        public ICommand IncreaseMainDeckQuantityCommand { get; }
        public ICommand DecreaseMainDeckQuantityCommand { get; }
        public ICommand IncreaseEvolveDeckQuantityCommand { get; }
        public ICommand DecreaseEvolveDeckQuantityCommand { get; }

        public Deck? CurrentDeck
        {
            get => _currentDeck;
            set
            {
                if (SetProperty(ref _currentDeck, value))
                {
                    RefreshDeckLists();
                    RefreshValidCards();
                    OnPropertyChanged(nameof(DeckName));
                    OnPropertyChanged(nameof(IsStandard));
                    OnPropertyChanged(nameof(IsGloryfinder));
                    OnPropertyChanged(nameof(IsCrossCraft));
                    OnPropertyChanged(nameof(MainDeckCount));
                    OnPropertyChanged(nameof(EvolveDeckCount));
                    OnPropertyChanged(nameof(Leader1Image));
                    OnPropertyChanged(nameof(Leader2Image));
                    OnPropertyChanged(nameof(ShowLeader2));
                    OnPropertyChanged(nameof(GloryCardImage));
                    OnPropertyChanged(nameof(ShowGloryCard));
                }
            }
        }

        public CardData? SelectedCard
        {
            get => _selectedCard;
            set
            {
                if (SetProperty(ref _selectedCard, value))
                {
                    CardViewer.SetCard(value);
                    ((RelayCommand?)AddToMainDeckCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand?)AddToEvolveDeckCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public DeckEntry? SelectedMainDeckEntry
        {
            get => _selectedMainDeckEntry;
            set
            {
                if (SetProperty(ref _selectedMainDeckEntry, value))
                {
                    ((RelayCommand?)IncreaseMainDeckQuantityCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand?)DecreaseMainDeckQuantityCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand?)RemoveFromMainDeckCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public DeckEntry? SelectedEvolveDeckEntry
        {
            get => _selectedEvolveDeckEntry;
            set
            {
                if (SetProperty(ref _selectedEvolveDeckEntry, value))
                {
                    ((RelayCommand?)IncreaseEvolveDeckQuantityCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand?)DecreaseEvolveDeckQuantityCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand?)RemoveFromEvolveDeckCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsEditingDeck
        {
            get => _isEditingDeck;
            set => SetProperty(ref _isEditingDeck, value);
        }

        public string DeckName
        {
            get => CurrentDeck?.Name ?? "No Deck Selected";
            set
            {
                if (CurrentDeck != null && CurrentDeck.Name != value)
                {
                    CurrentDeck.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsStandard => CurrentDeck?.DeckType == DeckType.Standard;
        public bool IsGloryfinder => CurrentDeck?.DeckType == DeckType.Gloryfinder;
        public bool IsCrossCraft => CurrentDeck?.DeckType == DeckType.CrossCraft;
        public bool ShowLeader2 => IsCrossCraft;
        public bool ShowGloryCard => IsGloryfinder;

        public int MainDeckCount => MainDeckList.Sum(e => e.Quantity);
        public int EvolveDeckCount => EvolveDeckList.Sum(e => e.Quantity);

        public string? Leader1Image => CurrentDeck?.Leader1?.ImageFile;
        public string? Leader2Image => CurrentDeck?.Leader2?.ImageFile;
        public string? GloryCardImage => CurrentDeck?.GloryCard?.ImageFile;

        public DeckBuilderViewModel(ObservableCollection<CardData> allCards, ObservableCollection<Deck> decks)
        {
            _allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
            _decks = decks ?? throw new ArgumentNullException(nameof(decks));

            // Use distinct CollectionViewSource instances so each tab gets its own view/filter.
            _standardDecks = new CollectionViewSource { Source = _decks }.View;
            _standardDecks.Filter = obj => obj is Deck d && d.DeckType == DeckType.Standard;

            _gloryfinderDecks = new CollectionViewSource { Source = _decks }.View;
            _gloryfinderDecks.Filter = obj => obj is Deck d && d.DeckType == DeckType.Gloryfinder;

            _crossCraftDecks = new CollectionViewSource { Source = _decks }.View;
            _crossCraftDecks.Filter = obj => obj is Deck d && d.DeckType == DeckType.CrossCraft;

            // Valid cards view must be independent from the AllCards default view.
            _validCards = new CollectionViewSource { Source = _allCards }.View;
            _validCards.Filter = FilterValidCards;

            CreateDeckCommand = new RelayCommand(
                execute: () => { CreateNewDeck(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => true);

            EditDeckCommand = new RelayCommand(
                execute: () => { IsEditingDeck = true; return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CurrentDeck != null);

            DeleteDeckCommand = new RelayCommand(
                execute: () => { DeleteDeck(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CurrentDeck != null);

            BackToDeckListCommand = new RelayCommand(
                execute: () => { IsEditingDeck = false; CurrentDeck = null; return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => IsEditingDeck);

            AddToMainDeckCommand = new RelayCommand(
                execute: () => { AddCardToMainDeck(SelectedCard); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedCard != null && CurrentDeck != null && CanAddToMainDeck(SelectedCard));

            RemoveFromMainDeckCommand = new RelayCommand(
                execute: () => { RemoveCardFromMainDeck(SelectedMainDeckEntry); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedMainDeckEntry != null);

            AddToEvolveDeckCommand = new RelayCommand(
                execute: () => { AddCardToEvolveDeck(SelectedCard); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedCard != null && CurrentDeck != null && CanAddToEvolveDeck(SelectedCard));

            RemoveFromEvolveDeckCommand = new RelayCommand(
                execute: () => { RemoveCardFromEvolveDeck(SelectedEvolveDeckEntry); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedEvolveDeckEntry != null);

            IncreaseMainDeckQuantityCommand = new RelayCommand(
                execute: () => { IncreaseMainDeckQuantity(SelectedMainDeckEntry); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedMainDeckEntry != null && CanIncreaseMainDeckQuantity(SelectedMainDeckEntry));

            DecreaseMainDeckQuantityCommand = new RelayCommand(
                execute: () => { DecreaseMainDeckQuantity(SelectedMainDeckEntry); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedMainDeckEntry != null && SelectedMainDeckEntry.Quantity > 1);

            IncreaseEvolveDeckQuantityCommand = new RelayCommand(
                execute: () => { IncreaseEvolveDeckQuantity(SelectedEvolveDeckEntry); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedEvolveDeckEntry != null && CanIncreaseEvolveDeckQuantity(SelectedEvolveDeckEntry));

            DecreaseEvolveDeckQuantityCommand = new RelayCommand(
                execute: () => { DecreaseEvolveDeckQuantity(SelectedEvolveDeckEntry); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedEvolveDeckEntry != null && SelectedEvolveDeckEntry.Quantity > 1);
        }

        private void CreateNewDeck()
        {
            var wizardViewModel = new CreateDeckWizardViewModel(_allCards);
            var dialog = new CreateDeckWizardDialog
            {
                DataContext = wizardViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                var newDeck = wizardViewModel.CreateDeck();
                _decks.Add(newDeck);
                CurrentDeck = newDeck;
                IsEditingDeck = true;
            }
        }

        private void DeleteDeck()
        {
            if (CurrentDeck == null) return;
            _decks.Remove(CurrentDeck);
            CurrentDeck = null;
            IsEditingDeck = false;
        }

        private bool FilterValidCards(object? obj)
        {
            if (obj is not CardData card || CurrentDeck == null) return false;

            // Exclude leaders from the valid cards list
            if (card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false)
                return false;

            return CurrentDeck.DeckType switch
            {
                DeckType.Standard => IsValidForStandard(card),
                DeckType.Gloryfinder => IsValidForGloryfinder(card),
                DeckType.CrossCraft => IsValidForCrossCraft(card),
                _ => false
            };
        }

        private bool IsValidForStandard(CardData card)
        {
            if (CurrentDeck == null) return false;
            return string.Equals(card.Class, CurrentDeck.Class1, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(card.Class, "Neutral", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsValidForGloryfinder(CardData card)
        {
            // Gloryfinder can contain any class
            return true;
        }

        private bool IsValidForCrossCraft(CardData card)
        {
            if (CurrentDeck == null) return false;
            return string.Equals(card.Class, CurrentDeck.Class1, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(card.Class, CurrentDeck.Class2, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(card.Class, "Neutral", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanAddToMainDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;

            return CurrentDeck.DeckType switch
            {
                DeckType.Standard => CanAddToMainDeckStandard(card),
                DeckType.Gloryfinder => CanAddToMainDeckGloryfinder(card),
                DeckType.CrossCraft => CanAddToMainDeckCrossCraft(card),
                _ => false
            };
        }

        private bool CanAddToMainDeckStandard(CardData card)
        {
            // Check if card is already at max copies (3) in main deck
            var existing = CurrentDeck!.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null && existing.Quantity >= 3) return false;

            // Check if adding would exceed 50 cards
            int currentCount = CurrentDeck.MainDeck.Sum(e => e.Quantity);
            return currentCount < 50;
        }

        private bool CanAddToMainDeckGloryfinder(CardData card)
        {
            // No duplicates in Gloryfinder
            var existing = CurrentDeck!.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null) return false;

            // Cannot exceed 50 cards
            int currentCount = CurrentDeck.MainDeck.Sum(e => e.Quantity);
            if (currentCount >= 50) return false;

            // Cannot be the glory card
            if (CurrentDeck.GloryCard != null && card.CardNumber == CurrentDeck.GloryCard.CardNumber)
                return false;

            return true;
        }

        private bool CanAddToMainDeckCrossCraft(CardData card)
        {
            // Same rules as Standard for CrossCraft
            return CanAddToMainDeckStandard(card);
        }

        private bool CanAddToEvolveDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;

            // Only evolved followers can go in evolve deck
            if (!card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? true)
                return false;

            return CurrentDeck.DeckType switch
            {
                DeckType.Standard => CanAddToEvolveDeckStandard(card),
                DeckType.Gloryfinder => CanAddToEvolveDeckGloryfinder(card),
                DeckType.CrossCraft => CanAddToEvolveDeckCrossCraft(card),
                _ => false
            };
        }

        private bool CanAddToEvolveDeckStandard(CardData card)
        {
            var existing = CurrentDeck!.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null && existing.Quantity >= 3) return false;

            int currentCount = CurrentDeck.EvolveDeck.Sum(e => e.Quantity);
            return currentCount < 10;
        }

        private bool CanAddToEvolveDeckGloryfinder(CardData card)
        {
            var existing = CurrentDeck!.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null) return false;

            int currentCount = CurrentDeck.EvolveDeck.Sum(e => e.Quantity);
            return currentCount < 20;
        }

        private bool CanAddToEvolveDeckCrossCraft(CardData card)
        {
            return CanAddToEvolveDeckStandard(card);
        }

        private bool CanIncreaseMainDeckQuantity(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return false;

            return CurrentDeck.DeckType switch
            {
                DeckType.Standard => entry.Quantity < 3 && MainDeckCount < 50,
                DeckType.Gloryfinder => false, // No duplicates in Gloryfinder
                DeckType.CrossCraft => entry.Quantity < 3 && MainDeckCount < 50,
                _ => false
            };
        }

        private bool CanIncreaseEvolveDeckQuantity(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return false;

            return CurrentDeck.DeckType switch
            {
                DeckType.Standard => entry.Quantity < 3 && EvolveDeckCount < 10,
                DeckType.Gloryfinder => false, // No duplicates in Gloryfinder
                DeckType.CrossCraft => entry.Quantity < 3 && EvolveDeckCount < 10,
                _ => false
            };
        }

        private void AddCardToMainDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            var existing = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                CurrentDeck.MainDeck.Add(new DeckEntry { Card = card, Quantity = 1 });
            }

            RefreshDeckLists();
            OnPropertyChanged(nameof(MainDeckCount));
        }

        private void RemoveCardFromMainDeck(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return;
            CurrentDeck.MainDeck.Remove(entry);
            RefreshDeckLists();
            OnPropertyChanged(nameof(MainDeckCount));
        }

        private void AddCardToEvolveDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                CurrentDeck.EvolveDeck.Add(new DeckEntry { Card = card, Quantity = 1 });
            }

            RefreshDeckLists();
            OnPropertyChanged(nameof(EvolveDeckCount));
        }

        private void RemoveCardFromEvolveDeck(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return;
            CurrentDeck.EvolveDeck.Remove(entry);
            RefreshDeckLists();
            OnPropertyChanged(nameof(EvolveDeckCount));
        }

        private void IncreaseMainDeckQuantity(DeckEntry? entry)
        {
            if (entry != null)
            {
                entry.Quantity++;
                OnPropertyChanged(nameof(MainDeckCount));
            }
        }

        private void DecreaseMainDeckQuantity(DeckEntry? entry)
        {
            if (entry != null && entry.Quantity > 1)
            {
                entry.Quantity--;
                OnPropertyChanged(nameof(MainDeckCount));
            }
        }

        private void IncreaseEvolveDeckQuantity(DeckEntry? entry)
        {
            if (entry != null)
            {
                entry.Quantity++;
                OnPropertyChanged(nameof(EvolveDeckCount));
            }
        }

        private void DecreaseEvolveDeckQuantity(DeckEntry? entry)
        {
            if (entry != null && entry.Quantity > 1)
            {
                entry.Quantity--;
                OnPropertyChanged(nameof(EvolveDeckCount));
            }
        }

        private void RefreshDeckLists()
        {
            MainDeckList.Clear();
            EvolveDeckList.Clear();

            if (CurrentDeck != null)
            {
                foreach (var entry in CurrentDeck.MainDeck.OrderBy(e => e.Card.Name))
                    MainDeckList.Add(entry);

                foreach (var entry in CurrentDeck.EvolveDeck.OrderBy(e => e.Card.Name))
                    EvolveDeckList.Add(entry);
            }
        }

        private void RefreshValidCards()
        {
            _validCards.Refresh();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}