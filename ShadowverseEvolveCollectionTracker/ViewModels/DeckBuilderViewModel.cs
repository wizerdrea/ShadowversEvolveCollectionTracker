using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using ShadowverseEvolveCardTracker.Models;
using ShadowverseEvolveCardTracker.Services;
using ShadowverseEvolveCardTracker.Views;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    public class DeckBuilderViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly ObservableCollection<CardData> _allCards;
        private readonly ObservableCollection<Deck> _decks;
        private readonly ICollectionView _standardDecks;
        private readonly ICollectionView _gloryfinderDecks;
        private readonly ICollectionView _crossCraftDecks;
        private readonly ICollectionView _validCards;
        
        private readonly DeckValidationService _validationService;
        private readonly DeckOperationsHandler _operationsHandler;

        private Deck? _currentDeck;
        private CardData? _selectedCard;
        private bool _isEditingDeck;
        private DeckEntry? _selectedMainDeckEntry;
        private DeckEntry? _selectedEvolveDeckEntry;
        private CardData? _selectedLeader;
        private CardData? _selectedGloryCard;
        private Deck? _subscribedDeck;
        private int _deckChangeTick;
        private bool _deckIsValid;
        private string _deckValidityText = "Invalid";
        private string _deckValidationTooltip = "No deck selected.";

        #endregion

        #region Properties - Collections

        public ICollectionView StandardDecks => _standardDecks;
        public ICollectionView GloryfinderDecks => _gloryfinderDecks;
        public ICollectionView CrossCraftDecks => _crossCraftDecks;
        public ICollectionView ValidCards => _validCards;

        public CardViewerViewModel CardViewer { get; } = new();

        public ObservableCollection<DeckEntry> MainDeckList { get; } = new();
        public ObservableCollection<DeckEntry> EvolveDeckList { get; } = new();
        public ObservableCollection<CardData> LeadersList { get; } = new();
        public ObservableCollection<CardData> GloryCardList { get; } = new();

        #endregion

        #region Properties - Current State

        public Deck? CurrentDeck
        {
            get => _currentDeck;
            set
            {
                if (SetProperty(ref _currentDeck, value))
                {
                    UnsubscribeFromDeck();
                    _subscribedDeck = _currentDeck;
                    SubscribeToDeck();

                    RefreshAll();
                    ClearSelections();
                    RaiseAllCommandStates();
                    EvaluateDeckValidity();
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
                    if (value != null)
                        CardViewer.SetCard(value);
                    RaiseAddCommandStates();
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
                    if (value?.Card != null)
                        CardViewer.SetCard(value.Card);
                    RaiseMainDeckQuantityCommandStates();
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
                    if (value?.Card != null)
                        CardViewer.SetCard(value.Card);
                    RaiseEvolveDeckQuantityCommandStates();
                }
            }
        }

        public CardData? SelectedLeader
        {
            get => _selectedLeader;
            set
            {
                if (SetProperty(ref _selectedLeader, value) && value != null)
                    CardViewer.SetCard(value);
            }
        }

        public CardData? SelectedGloryCard
        {
            get => _selectedGloryCard;
            set
            {
                if (SetProperty(ref _selectedGloryCard, value) && value != null)
                    CardViewer.SetCard(value);
            }
        }

        public bool IsEditingDeck
        {
            get => _isEditingDeck;
            set
            {
                if (SetProperty(ref _isEditingDeck, value))
                {
                    RaiseCommand(BackToDeckListCommand);
                    RaiseCommand(CreateDeckCommand);
                    RaiseCommand(EditDeckCommand);
                    RaiseCommand(DeleteDeckCommand);
                }
            }
        }

        public int DeckChangeTick => _deckChangeTick;

        // Exposed properties for validity display and tooltip
        public bool DeckIsValid
        {
            get => _deckIsValid;
            private set => SetProperty(ref _deckIsValid, value);
        }

        public string DeckValidityText
        {
            get => _deckValidityText;
            private set => SetProperty(ref _deckValidityText, value);
        }

        public string DeckValidationTooltip
        {
            get => _deckValidationTooltip;
            private set => SetProperty(ref _deckValidationTooltip, value);
        }

        #endregion

        #region Properties - Deck Info

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
        public string Leader1Name => CurrentDeck?.Leader1?.Name ?? "None";
        public string Leader2Name => CurrentDeck?.Leader2?.Name ?? "None";

        public int CurrentInDeckQuantity
        {
            get
            {
                var card = CardViewer.CurrentCard;
                if (card == null || CurrentDeck == null) return 0;

                if (_validationService.IsNonDeckCard(card)) return 0;

                var deck = _validationService.IsEvolvedCard(card) 
                    ? CurrentDeck.EvolveDeck 
                    : CurrentDeck.MainDeck;

                return deck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber)?.Quantity ?? 0;
            }
        }

        #endregion

        #region Commands

        public ICommand CreateDeckCommand { get; }
        public ICommand EditDeckCommand { get; }
        public ICommand DeleteDeckCommand { get; }
        public ICommand BackToDeckListCommand { get; }

        public ICommand AddToDeckCommand { get; }
        public ICommand AddToMainDeckCommand { get; }
        public ICommand RemoveFromMainDeckCommand { get; }
        public ICommand AddToEvolveDeckCommand { get; }
        public ICommand RemoveFromEvolveDeckCommand { get; }
        
        public ICommand IncreaseMainDeckQuantityCommand { get; }
        public ICommand DecreaseMainDeckQuantityCommand { get; }
        public ICommand IncreaseEvolveDeckQuantityCommand { get; }
        public ICommand DecreaseEvolveDeckQuantityCommand { get; }

        public ICommand IncreaseCurrentCardQuantityCommand { get; }
        public ICommand DecreaseCurrentCardQuantityCommand { get; }

        public ICommand IncreaseAvailableCardCommand { get; }
        public ICommand DecreaseAvailableCardCommand { get; }

        #endregion

        #region Constructor

        public DeckBuilderViewModel(ObservableCollection<CardData> allCards, ObservableCollection<Deck> decks)
        {
            _allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
            _decks = decks ?? throw new ArgumentNullException(nameof(decks));

            _validationService = new DeckValidationService();
            _operationsHandler = new DeckOperationsHandler(_validationService);

            // Initialize collection views directly in constructor
            _standardDecks = new CollectionViewSource { Source = _decks }.View;
            _standardDecks.Filter = obj => obj is Deck d && d.DeckType == DeckType.Standard;

            _gloryfinderDecks = new CollectionViewSource { Source = _decks }.View;
            _gloryfinderDecks.Filter = obj => obj is Deck d && d.DeckType == DeckType.Gloryfinder;

            _crossCraftDecks = new CollectionViewSource { Source = _decks }.View;
            _crossCraftDecks.Filter = obj => obj is Deck d && d.DeckType == DeckType.CrossCraft;

            _validCards = new CollectionViewSource { Source = _allCards }.View;
            _validCards.Filter = obj => obj is CardData card && CurrentDeck != null && _validationService.IsValidForDeck(card, CurrentDeck);

            // Initialize commands directly in constructor
            CreateDeckCommand = new RelayCommand(
                execute: async () => await System.Threading.Tasks.Task.Run(CreateNewDeck),
                canExecute: () => true);

            EditDeckCommand = new RelayCommand<Deck?>(
                execute: deck =>
                {
                    if (deck != null)
                    {
                        CurrentDeck = deck;
                        IsEditingDeck = true;
                    }
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: deck => deck != null);

            DeleteDeckCommand = new RelayCommand<Deck?>(
                execute: deck =>
                {
                    if (deck != null)
                        DeleteDeck(deck);
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: deck => deck != null);

            BackToDeckListCommand = new RelayCommand(
                execute: () =>
                {
                    IsEditingDeck = false;
                    CurrentDeck = null;
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: () => IsEditingDeck);

            AddToDeckCommand = new RelayCommand(
                execute: () => { ExecuteAddCard(CardViewer.CurrentCard); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CanAddCardToDeck(CardViewer.CurrentCard));

            AddToMainDeckCommand = new RelayCommand(
                execute: () => { ExecuteAddToMainDeck(CardViewer.CurrentCard); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CardViewer.CurrentCard != null && CurrentDeck != null && 
                    _validationService.CanAddToMainDeck(CardViewer.CurrentCard, CurrentDeck));

            RemoveFromMainDeckCommand = new RelayCommand(
                execute: () => { ExecuteRemoveFromMainDeck(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedMainDeckEntry != null);

            AddToEvolveDeckCommand = new RelayCommand(
                execute: () => { ExecuteAddToEvolveDeck(CardViewer.CurrentCard); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CardViewer.CurrentCard != null && CurrentDeck != null && 
                    _validationService.CanAddToEvolveDeck(CardViewer.CurrentCard, CurrentDeck));

            RemoveFromEvolveDeckCommand = new RelayCommand(
                execute: () => { ExecuteRemoveFromEvolveDeck(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedEvolveDeckEntry != null);

            IncreaseMainDeckQuantityCommand = new RelayCommand<DeckEntry?>(
                execute: entry => { ExecuteIncreaseQuantity(entry, false); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: entry => entry != null && CurrentDeck != null && 
                    _validationService.CanIncreaseMainDeckQuantity(entry, CurrentDeck));

            DecreaseMainDeckQuantityCommand = new RelayCommand<DeckEntry?>(
                execute: entry => { ExecuteDecreaseQuantity(entry, false); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: entry => entry != null);

            IncreaseEvolveDeckQuantityCommand = new RelayCommand<DeckEntry?>(
                execute: entry => { ExecuteIncreaseQuantity(entry, true); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: entry => entry != null && CurrentDeck != null && 
                    _validationService.CanIncreaseEvolveDeckQuantity(entry, CurrentDeck));

            DecreaseEvolveDeckQuantityCommand = new RelayCommand<DeckEntry?>(
                execute: entry => { ExecuteDecreaseQuantity(entry, true); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: entry => entry != null);

            IncreaseCurrentCardQuantityCommand = new RelayCommand(
                execute: () => { ExecuteIncreaseViewedCardQuantity(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: CanIncreaseViewedCardQuantity);

            DecreaseCurrentCardQuantityCommand = new RelayCommand(
                execute: () => { ExecuteDecreaseViewedCardQuantity(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: CanDecreaseViewedCardQuantity);

            IncreaseAvailableCardCommand = new RelayCommand<CardData?>(
                execute: card => { ExecuteIncreaseAvailableCard(card); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: CanAddCardToDeck);

            DecreaseAvailableCardCommand = new RelayCommand<CardData?>(
                execute: card => { ExecuteDecreaseAvailableCard(card); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: CanRemoveCardFromDeck);

            CardViewer.PropertyChanged += CardViewer_PropertyChanged;
        }

        #endregion

        #region Command Execution

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

        private void DeleteDeck(Deck deck)
        {
            var dlg = new ConfirmDialog
            {
                Owner = System.Windows.Application.Current.MainWindow,
                Message = $"Are you sure you want to delete the deck '{deck.Name}'?",
                Title = "Confirm Delete"
            };

            if (dlg.ShowDialog() != true) return;

            if (CurrentDeck == deck)
            {
                CurrentDeck = null;
                IsEditingDeck = false;
            }
            _decks.Remove(deck);
        }

        private void ExecuteAddCard(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;
            
            if (_operationsHandler.TryAddCard(card, CurrentDeck, out var onSuccess))
            {
                onSuccess?.Invoke();
                NotifyDeckChanged();
            }
        }

        private void ExecuteAddToMainDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            var existing = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null)
                existing.Quantity++;
            else
                CurrentDeck.MainDeck.Add(new DeckEntry { Card = card, Quantity = 1 });

            NotifyDeckChanged();
        }

        private void ExecuteRemoveFromMainDeck()
        {
            if (SelectedMainDeckEntry == null || CurrentDeck == null) return;
            CurrentDeck.MainDeck.Remove(SelectedMainDeckEntry);
            NotifyDeckChanged();
        }

        private void ExecuteAddToEvolveDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null)
                existing.Quantity++;
            else
                CurrentDeck.EvolveDeck.Add(new DeckEntry { Card = card, Quantity = 1 });

            NotifyDeckChanged();
        }

        private void ExecuteRemoveFromEvolveDeck()
        {
            if (SelectedEvolveDeckEntry == null || CurrentDeck == null) return;
            CurrentDeck.EvolveDeck.Remove(SelectedEvolveDeckEntry);
            NotifyDeckChanged();
        }

        private void ExecuteIncreaseQuantity(DeckEntry? entry, bool isEvolveDeck)
        {
            if (entry == null || CurrentDeck == null) return;
            
            _operationsHandler.IncreaseQuantity(entry, CurrentDeck, isEvolveDeck);
            NotifyDeckChanged();
        }

        private void ExecuteDecreaseQuantity(DeckEntry? entry, bool isEvolveDeck)
        {
            if (entry == null || CurrentDeck == null) return;
            
            _operationsHandler.DecreaseQuantity(entry, CurrentDeck, isEvolveDeck);
            RefreshDeckLists();
            NotifyDeckChanged();
        }

        private void ExecuteIncreaseViewedCardQuantity()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return;

            if (_validationService.IsEvolvedCard(card))
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                    _operationsHandler.IncreaseQuantity(existing, CurrentDeck, true);
                else if (_validationService.CanAddToEvolveDeck(card, CurrentDeck))
                    ExecuteAddToEvolveDeck(card);
            }
            else if (!_validationService.IsNonDeckCard(card))
            {
                var existing = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                    _operationsHandler.IncreaseQuantity(existing, CurrentDeck, false);
                else if (_validationService.CanAddToMainDeck(card, CurrentDeck))
                    ExecuteAddToMainDeck(card);
            }

            NotifyDeckChanged();
        }

        private void ExecuteDecreaseViewedCardQuantity()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return;

            if (_validationService.IsEvolvedCard(card))
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                {
                    _operationsHandler.DecreaseQuantity(existing, CurrentDeck, true);
                    RefreshDeckLists();
                }
            }
            else if (!_validationService.IsNonDeckCard(card))
            {
                var existing = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                {
                    _operationsHandler.DecreaseQuantity(existing, CurrentDeck, false);
                    RefreshDeckLists();
                }
            }

            NotifyDeckChanged();
        }

        private void ExecuteIncreaseAvailableCard(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            if (_validationService.IsEvolvedCard(card))
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                    _operationsHandler.IncreaseQuantity(existing, CurrentDeck, true);
                else if (_validationService.CanAddToEvolveDeck(card, CurrentDeck))
                    ExecuteAddToEvolveDeck(card);
            }
            else if (!_validationService.IsNonDeckCard(card))
            {
                var existing = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                    _operationsHandler.IncreaseQuantity(existing, CurrentDeck, false);
                else if (_validationService.CanAddToMainDeck(card, CurrentDeck))
                    ExecuteAddToMainDeck(card);
            }

            NotifyDeckChanged();
        }

        private void ExecuteDecreaseAvailableCard(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            if (_validationService.IsEvolvedCard(card))
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                {
                    _operationsHandler.DecreaseQuantity(existing, CurrentDeck, true);
                    RefreshDeckLists();
                }
            }
            else if (!_validationService.IsNonDeckCard(card))
            {
                var existing = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                {
                    _operationsHandler.DecreaseQuantity(existing, CurrentDeck, false);
                    RefreshDeckLists();
                }
            }

            NotifyDeckChanged();
        }

        #endregion

        #region Can Execute Helpers

        private bool CanAddCardToDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;

            if (!_validationService.IsValidForDeck(card, CurrentDeck)) return false;

            if (_validationService.IsLeaderCard(card))
                return _validationService.CanAddLeader(card, CurrentDeck);
            
            if (_validationService.IsTokenCard(card))
                return false;
            
            if (_validationService.IsEvolvedCard(card))
                return _validationService.CanAddToEvolveDeck(card, CurrentDeck);
            
            return _validationService.CanAddToMainDeck(card, CurrentDeck);
        }

        private bool CanRemoveCardFromDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;

            if (_validationService.IsNonDeckCard(card)) return false;

            if (_validationService.IsEvolvedCard(card))
                return CurrentDeck.EvolveDeck.Any(e => e.Card.CardNumber == card.CardNumber);

            return CurrentDeck.MainDeck.Any(e => e.Card.CardNumber == card.CardNumber);
        }

        private bool CanIncreaseViewedCardQuantity()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return false;

            if (_validationService.IsNonDeckCard(card)) return false;

            if (_validationService.IsEvolvedCard(card))
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                return existing != null 
                    ? _validationService.CanIncreaseEvolveDeckQuantity(existing, CurrentDeck)
                    : _validationService.CanAddToEvolveDeck(card, CurrentDeck);
            }

            var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            return existingMain != null
                ? _validationService.CanIncreaseMainDeckQuantity(existingMain, CurrentDeck)
                : _validationService.CanAddToMainDeck(card, CurrentDeck);
        }

        private bool CanDecreaseViewedCardQuantity()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return false;

            if (_validationService.IsNonDeckCard(card)) return false;

            if (_validationService.IsEvolvedCard(card))
                return CurrentDeck.EvolveDeck.Any(e => e.Card.CardNumber == card.CardNumber && e.Quantity > 0);

            return CurrentDeck.MainDeck.Any(e => e.Card.CardNumber == card.CardNumber && e.Quantity > 0);
        }

        #endregion

        #region Event Handlers

        private void CardViewer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(CardViewer.CurrentCard))
            {
                RaiseAddCommandStates();
                OnPropertyChanged(nameof(CurrentInDeckQuantity));
                RaiseQuantityCommandStates();
            }
        }

        private void Deck_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var prop = e?.PropertyName ?? string.Empty;

            if (string.IsNullOrEmpty(prop) || 
                 prop == nameof(Deck.Leader1) || prop == nameof(Deck.Leader2) || 
                 prop == nameof(Deck.GloryCard) || prop == nameof(Deck.Class1) || 
                 prop == nameof(Deck.Class2) || prop == nameof(Deck.Name))
             {
                OnPropertyChanged(nameof(Leader1Image));
                OnPropertyChanged(nameof(Leader2Image));
                OnPropertyChanged(nameof(Leader1Name));
                OnPropertyChanged(nameof(Leader2Name));
                OnPropertyChanged(nameof(GloryCardImage));
                OnPropertyChanged(nameof(ShowLeader2));
                OnPropertyChanged(nameof(ShowGloryCard));
                OnPropertyChanged(nameof(DeckName));

                RefreshLeadersAndGloryCard();
                RaiseAddCommandStates();
                EvaluateDeckValidity();
             }
 
             if (string.IsNullOrEmpty(prop) || prop == nameof(Deck.MainDeck) || prop == nameof(Deck.EvolveDeck))
             {
                 RefreshDeckLists();
                 OnPropertyChanged(nameof(MainDeckCount));
                 OnPropertyChanged(nameof(EvolveDeckCount));
                 OnPropertyChanged(nameof(CurrentInDeckQuantity));
                 RaiseQuantityCommandStates();
                 EvaluateDeckValidity();
             }
         }

        #endregion

        #region Helper Methods

        private void NotifyDeckChanged()
        {
            RefreshDeckLists();
            OnPropertyChanged(nameof(MainDeckCount));
            OnPropertyChanged(nameof(EvolveDeckCount));
            OnPropertyChanged(nameof(CurrentInDeckQuantity));
            RaiseQuantityCommandStates();
            EvaluateDeckValidity();
            BumpDeckChangeTick();
        }

        private void BumpDeckChangeTick()
        {
            _deckChangeTick++;
            OnPropertyChanged(nameof(DeckChangeTick));
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

        private void RefreshValidCards() => _validCards.Refresh();

        private void RefreshLeadersAndGloryCard()
        {
            LeadersList.Clear();
            GloryCardList.Clear();

            if (CurrentDeck != null)
            {
                if (CurrentDeck.Leader1 != null) LeadersList.Add(CurrentDeck.Leader1);
                if (CurrentDeck.Leader2 != null) LeadersList.Add(CurrentDeck.Leader2);
                if (CurrentDeck.GloryCard != null) GloryCardList.Add(CurrentDeck.GloryCard);
            }
        }

        private void RefreshAll()
        {
            RefreshDeckLists();
            RefreshValidCards();
            RefreshLeadersAndGloryCard();
            
            OnPropertyChanged(nameof(DeckName));
            OnPropertyChanged(nameof(IsStandard));
            OnPropertyChanged(nameof(IsGloryfinder));
            OnPropertyChanged(nameof(IsCrossCraft));
            OnPropertyChanged(nameof(MainDeckCount));
            OnPropertyChanged(nameof(EvolveDeckCount));
            OnPropertyChanged(nameof(Leader1Image));
            OnPropertyChanged(nameof(Leader2Image));
            OnPropertyChanged(nameof(Leader1Name));
            OnPropertyChanged(nameof(Leader2Name));
            OnPropertyChanged(nameof(ShowLeader2));
            OnPropertyChanged(nameof(GloryCardImage));
            OnPropertyChanged(nameof(ShowGloryCard));
            EvaluateDeckValidity();
        }

        private void ClearSelections()
        {
            SelectedCard = null;
            SelectedMainDeckEntry = null;
            SelectedEvolveDeckEntry = null;
            SelectedLeader = null;
            SelectedGloryCard = null;
        }

        private void SubscribeToDeck()
        {
            if (_subscribedDeck != null)
                _subscribedDeck.PropertyChanged += Deck_PropertyChanged;
        }

        private void UnsubscribeFromDeck()
        {
            if (_subscribedDeck != null)
                _subscribedDeck.PropertyChanged -= Deck_PropertyChanged;
        }

        #endregion

        #region Command State Management

        private void RaiseCommand(ICommand? command)
        {
            if (command is IRelayCommand rc) rc.RaiseCanExecuteChanged();
        }

        private void RaiseAddCommandStates()
        {
            RaiseCommand(AddToDeckCommand);
            RaiseCommand(AddToMainDeckCommand);
            RaiseCommand(AddToEvolveDeckCommand);
        }

        private void RaiseMainDeckQuantityCommandStates()
        {
            RaiseCommand(IncreaseMainDeckQuantityCommand);
            RaiseCommand(DecreaseMainDeckQuantityCommand);
            RaiseCommand(RemoveFromMainDeckCommand);
        }

        private void RaiseEvolveDeckQuantityCommandStates()
        {
            RaiseCommand(IncreaseEvolveDeckQuantityCommand);
            RaiseCommand(DecreaseEvolveDeckQuantityCommand);
            RaiseCommand(RemoveFromEvolveDeckCommand);
        }

        private void RaiseQuantityCommandStates()
        {
            RaiseMainDeckQuantityCommandStates();
            RaiseEvolveDeckQuantityCommandStates();
            RaiseCommand(IncreaseCurrentCardQuantityCommand);
            RaiseCommand(DecreaseCurrentCardQuantityCommand);
            RaiseCommand(IncreaseAvailableCardCommand);
            RaiseCommand(DecreaseAvailableCardCommand);
        }

        private void RaiseAllCommandStates()
        {
            RaiseCommand(EditDeckCommand);
            RaiseCommand(DeleteDeckCommand);
            RaiseCommand(BackToDeckListCommand);
            RaiseAddCommandStates();
            RaiseQuantityCommandStates();
            OnPropertyChanged(nameof(CurrentInDeckQuantity));
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Deck Validity Evaluation

        private void EvaluateDeckValidity()
        {
            if (CurrentDeck == null)
            {
                DeckIsValid = false;
                DeckValidityText = "No Deck";
                DeckValidationTooltip = "No deck selected.";
                return;
            }

            var errors = _validationService.ValidateDeck(CurrentDeck);
            var valid = errors.Count == 0;
            DeckIsValid = valid;
            DeckValidityText = valid ? "Valid" : $"Invalid ({errors.Count})";
            if (valid)
            {
                DeckValidationTooltip = "Deck is valid.";
            }
            else
            {
                // Join with newline for tooltip listing
                DeckValidationTooltip = string.Join(Environment.NewLine, errors);
            }
        }

        #endregion
    }
}