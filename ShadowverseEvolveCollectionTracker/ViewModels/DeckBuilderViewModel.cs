using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
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
        private CardData? _selectedLeader;
        private CardData? _selectedGloryCard;

        // Keep track of the deck we've subscribed to so we can unsubscribe cleanly.
        private Deck? _subscribedDeck;

        // Tick value used to force re-evaluation of bindings that depend on the deck contents.
        private int _deckChangeTick;

        public ICollectionView StandardDecks => _standardDecks;
        public ICollectionView GloryfinderDecks => _gloryfinderDecks;
        public ICollectionView CrossCraftDecks => _crossCraftDecks;
        public ICollectionView ValidCards => _validCards;

        public CardViewerViewModel CardViewer { get; } = new();

        public ObservableCollection<DeckEntry> MainDeckList { get; } = new();
        public ObservableCollection<DeckEntry> EvolveDeckList { get; } = new();
        public ObservableCollection<CardData> LeadersList { get; } = new();
        public ObservableCollection<CardData> GloryCardList { get; } = new();

        public ICommand CreateDeckCommand { get; }
        public ICommand EditDeckCommand { get; }
        public ICommand DeleteDeckCommand { get; }
        public ICommand BackToDeckListCommand { get; }

        public ICommand AddToDeckCommand { get; }
        public ICommand AddToMainDeckCommand { get; }
        public ICommand RemoveFromMainDeckCommand { get; }
        public ICommand AddToEvolveDeckCommand { get; }
        public ICommand RemoveFromEvolveDeckCommand { get; }
        public ICommand IncreaseMainDeckQuantityCommand { get; private set; }
        public ICommand DecreaseMainDeckQuantityCommand { get; private set; }
        public ICommand IncreaseEvolveDeckQuantityCommand { get; private set; }
        public ICommand DecreaseEvolveDeckQuantityCommand { get; private set; }

        // New: controls for modifying the viewed card's quantity in the current deck
        public ICommand IncreaseCurrentCardQuantityCommand { get; }
        public ICommand DecreaseCurrentCardQuantityCommand { get; }

        // New: parameterized commands to add/remove a card from the deck directly from the AvailableCards grid
        public ICommand IncreaseAvailableCardCommand { get; }
        public ICommand DecreaseAvailableCardCommand { get; }

        // Expose the tick so XAML can bind to it to cause re-evaluation when deck contents change.
        public int DeckChangeTick => _deckChangeTick;

        public Deck? CurrentDeck
        {
            get => _currentDeck;
            set
            {
                if (SetProperty(ref _currentDeck, value))
                {
                    // Unsubscribe from previous deck notifications
                    if (_subscribedDeck != null)
                        _subscribedDeck.PropertyChanged -= Deck_PropertyChanged;

                    _subscribedDeck = _currentDeck;

                    // Subscribe to the new deck so changes to Leader/GloryCard/etc. propagate to the UI
                    if (_subscribedDeck != null)
                        _subscribedDeck.PropertyChanged += Deck_PropertyChanged;

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
                    OnPropertyChanged(nameof(Leader1Name));
                    OnPropertyChanged(nameof(Leader2Name));
                    OnPropertyChanged(nameof(ShowLeader2));
                    OnPropertyChanged(nameof(GloryCardImage));
                    OnPropertyChanged(nameof(ShowGloryCard));

                    // Ensure leader/glory lists reflect the newly selected deck immediately
                    RefreshLeadersAndGloryCard();

                    // Clear any selections (keeps viewer state consistent when deck changes)
                    SelectedCard = null;
                    SelectedMainDeckEntry = null;
                    SelectedEvolveDeckEntry = null;
                    SelectedLeader = null;
                    SelectedGloryCard = null;

                    // Update commands whose CanExecute depends on CurrentDeck/collections
                    if (EditDeckCommand is IRelayCommand editRc) editRc.RaiseCanExecuteChanged();
                    if (DeleteDeckCommand is IRelayCommand delRc) delRc.RaiseCanExecuteChanged();
                    if (AddToDeckCommand is IRelayCommand addDeckRc) addDeckRc.RaiseCanExecuteChanged();
                    if (AddToMainDeckCommand is IRelayCommand addMainRc) addMainRc.RaiseCanExecuteChanged();
                    if (AddToEvolveDeckCommand is IRelayCommand addEvolveRc) addEvolveRc.RaiseCanExecuteChanged();
                    if (RemoveFromMainDeckCommand is IRelayCommand removeMainRc) removeMainRc.RaiseCanExecuteChanged();
                    if (RemoveFromEvolveDeckCommand is IRelayCommand removeEvolveRc) removeEvolveRc.RaiseCanExecuteChanged();
                    if (IncreaseMainDeckQuantityCommand is IRelayCommand incMainRc) incMainRc.RaiseCanExecuteChanged();
                    if (DecreaseMainDeckQuantityCommand is IRelayCommand decMainRc) decMainRc.RaiseCanExecuteChanged();
                    if (IncreaseEvolveDeckQuantityCommand is IRelayCommand incEvolveRc) incEvolveRc.RaiseCanExecuteChanged();
                    if (DecreaseEvolveDeckQuantityCommand is IRelayCommand decEvolveRc) decEvolveRc.RaiseCanExecuteChanged();

                    // Also update new current-card quantity commands/properties
                    OnPropertyChanged(nameof(CurrentInDeckQuantity));
                    if (IncreaseCurrentCardQuantityCommand is IRelayCommand incCurRc) incCurRc.RaiseCanExecuteChanged();
                    if (DecreaseCurrentCardQuantityCommand is IRelayCommand decCurRc) decCurRc.RaiseCanExecuteChanged();

                    // Also update the new available-card commands
                    if (IncreaseAvailableCardCommand is IRelayCommand incAvail) incAvail.RaiseCanExecuteChanged();
                    if (DecreaseAvailableCardCommand is IRelayCommand decAvail) decAvail.RaiseCanExecuteChanged();

                    // BackToDeckList state also may change when CurrentDeck changes
                    if (BackToDeckListCommand is IRelayCommand backRc) backRc.RaiseCanExecuteChanged();
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
                    if (AddToDeckCommand is IRelayCommand addDeckRc) addDeckRc.RaiseCanExecuteChanged();
                    if (AddToMainDeckCommand is IRelayCommand addMainRc) addMainRc.RaiseCanExecuteChanged();
                    if (AddToEvolveDeckCommand is IRelayCommand addEvolveRc) addEvolveRc.RaiseCanExecuteChanged();
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
                    // Show the selected card in the viewer
                    if (value?.Card != null)
                    {
                        CardViewer.SetCard(value.Card);
                    }

                    if (IncreaseMainDeckQuantityCommand is IRelayCommand incMainRc) incMainRc.RaiseCanExecuteChanged();
                    if (DecreaseMainDeckQuantityCommand is IRelayCommand decMainRc) decMainRc.RaiseCanExecuteChanged();
                    if (RemoveFromMainDeckCommand is IRelayCommand removeMainRc) removeMainRc.RaiseCanExecuteChanged();
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
                    // Show the selected card in the viewer
                    if (value?.Card != null)
                    {
                        CardViewer.SetCard(value.Card);
                    }

                    if (IncreaseEvolveDeckQuantityCommand is IRelayCommand incEvolveRc) incEvolveRc.RaiseCanExecuteChanged();
                    if (DecreaseEvolveDeckQuantityCommand is IRelayCommand decEvolveRc) decEvolveRc.RaiseCanExecuteChanged();
                    if (RemoveFromEvolveDeckCommand is IRelayCommand removeEvolveRc) removeEvolveRc.RaiseCanExecuteChanged();
                }
            }
        }

        public CardData? SelectedLeader
        {
            get => _selectedLeader;
            set
            {
                if (SetProperty(ref _selectedLeader, value))
                {
                    // Show the selected leader in the viewer
                    if (value != null)
                    {
                        CardViewer.SetCard(value);
                    }
                }
            }
        }

        public CardData? SelectedGloryCard
        {
            get => _selectedGloryCard;
            set
            {
                if (SetProperty(ref _selectedGloryCard, value))
                {
                    // Show the selected glory card in the viewer
                    if (value != null)
                    {
                        CardViewer.SetCard(value);
                    }
                }
            }
        }

        public bool IsEditingDeck
        {
            get => _isEditingDeck;
            set
            {
                if (SetProperty(ref _isEditingDeck, value))
                {
                    // BackToDeckListCommand's CanExecute depends on IsEditingDeck; ensure the UI updates
                    if (BackToDeckListCommand is IRelayCommand backRc) backRc.RaiseCanExecuteChanged();

                    // Some commands may enable/disable based on editing state - refresh them as well.
                    if (CreateDeckCommand is IRelayCommand createRc) createRc.RaiseCanExecuteChanged();
                    if (EditDeckCommand is IRelayCommand editRc) editRc.RaiseCanExecuteChanged();
                    if (DeleteDeckCommand is IRelayCommand delRc) delRc.RaiseCanExecuteChanged();
                }
            }
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

        // New: expose leader names for the UI
        public string Leader1Name => CurrentDeck?.Leader1?.Name ?? "None";
        public string Leader2Name => CurrentDeck?.Leader2?.Name ?? "None";

        // New: how many of the currently-viewed card are present in the appropriate deck (main OR evolve)
        public int CurrentInDeckQuantity
        {
            get
            {
                var card = CardViewer.CurrentCard;
                if (card == null || CurrentDeck == null) return 0;

                // Evolve cards go to EvolveDeck; otherwise MainDeck
                if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    var ee = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                    return ee?.Quantity ?? 0;
                }

                // Leaders/tokens aren't represented by deck quantities
                if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    return 0;
                }

                var me = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                return me?.Quantity ?? 0;
            }
        }

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

            // Use parameter-capable commands for editing/deleting so row buttons can pass the deck item.
            EditDeckCommand = new RelayCommand<Deck?>(execute: deck =>
                {
                    if (deck == null) return System.Threading.Tasks.Task.CompletedTask;
                    CurrentDeck = deck;
                    IsEditingDeck = true;
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: deck => deck != null);

            DeleteDeckCommand = new RelayCommand<Deck?>(execute: deck =>
                {
                    if (deck == null) return System.Threading.Tasks.Task.CompletedTask;

                    // Use themed confirmation dialog
                    var dlg = new ConfirmDialog
                    {
                        Owner = System.Windows.Application.Current.MainWindow,
                        Message = $"Are you sure you want to delete the deck '{deck.Name}'?",
                        Title = "Confirm Delete"
                    };

                    if (dlg.ShowDialog() != true)
                        return System.Threading.Tasks.Task.CompletedTask;

                    // If deleting the currently edited deck, clear editing state.
                    if (CurrentDeck == deck)
                    {
                        CurrentDeck = null;
                        IsEditingDeck = false;
                    }
                    _decks.Remove(deck);
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: deck => deck != null);

            BackToDeckListCommand = new RelayCommand(
                execute: () => { IsEditingDeck = false; CurrentDeck = null; return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => IsEditingDeck);

            // Use the currently viewed card in the CardViewer (not the SelectedCard from the ValidCards list).
            AddToDeckCommand = new RelayCommand(
                execute: () => { AddCardToDeck(CardViewer.CurrentCard); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CardViewer.CurrentCard != null && CurrentDeck != null && CannAddToDeck(CardViewer.CurrentCard));

            // Use the currently viewed card in the CardViewer (not the SelectedCard from the ValidCards list).
            AddToMainDeckCommand = new RelayCommand(
                execute: () => { AddCardToMainDeck(CardViewer.CurrentCard); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CardViewer.CurrentCard != null && CurrentDeck != null && CanAddToMainDeck(CardViewer.CurrentCard));

            RemoveFromMainDeckCommand = new RelayCommand(
                execute: () => { RemoveCardFromMainDeck(SelectedMainDeckEntry); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedMainDeckEntry != null);

            AddToEvolveDeckCommand = new RelayCommand(
                execute: () => { AddCardToEvolveDeck(CardViewer.CurrentCard); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CardViewer.CurrentCard != null && CurrentDeck != null && CanAddToEvolveDeck(CardViewer.CurrentCard));

            RemoveFromEvolveDeckCommand = new RelayCommand(
                execute: () => { RemoveCardFromEvolveDeck(SelectedEvolveDeckEntry); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SelectedEvolveDeckEntry != null);

            // Parameterized +/- commands so per-row buttons operate on the passed DeckEntry.
            IncreaseMainDeckQuantityCommand = new RelayCommand<DeckEntry?>(execute: entry =>
                {
                    IncreaseMainDeckQuantity(entry);
                    RaiseQuantityCommandStates();
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: entry => CanIncreaseMainDeckQuantity(entry));

            DecreaseMainDeckQuantityCommand = new RelayCommand<DeckEntry?>(execute: entry =>
                {
                    DecreaseMainDeckQuantity(entry);
                    RaiseQuantityCommandStates();
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: entry => entry != null);

            IncreaseEvolveDeckQuantityCommand = new RelayCommand<DeckEntry?>(execute: entry =>
                {
                    IncreaseEvolveDeckQuantity(entry);
                    RaiseQuantityCommandStates();
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: entry => CanIncreaseEvolveDeckQuantity(entry));

            DecreaseEvolveDeckQuantityCommand = new RelayCommand<DeckEntry?>(execute: entry =>
                {
                    DecreaseEvolveDeckQuantity(entry);
                    RaiseQuantityCommandStates();
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: entry => entry != null);

            // New: commands to bump the quantity for the currently-viewed card
            IncreaseCurrentCardQuantityCommand = new RelayCommand(
                execute: () =>
                {
                    IncreaseQuantityForViewedCard();
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: () => CanIncreaseQuantityForViewedCard());

            DecreaseCurrentCardQuantityCommand = new RelayCommand(
                execute: () =>
                {
                    DecreaseQuantityForViewedCard();
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: () => CanDecreaseQuantityForViewedCard());

            // New: parameterized commands for available-cards +/- buttons
            IncreaseAvailableCardCommand = new RelayCommand<CardData?>(execute: card =>
                {
                    if (card == null || CurrentDeck == null) return System.Threading.Tasks.Task.CompletedTask;

                    if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
                    {
                        var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                        if (existing != null)
                            IncreaseEvolveDeckQuantity(existing);
                        else if (CanAddToEvolveDeck(card))
                            AddCardToEvolveDeck(card);
                    }
                    else if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        // no-op
                    }
                    else
                    {
                        var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                        if (existingMain != null)
                            IncreaseMainDeckQuantity(existingMain);
                        else if (CanAddToMainDeck(card))
                            AddCardToMainDeck(card);
                    }

                    OnPropertyChanged(nameof(CurrentInDeckQuantity));
                    OnPropertyChanged(nameof(MainDeckCount));
                    OnPropertyChanged(nameof(EvolveDeckCount));
                    RefreshDeckLists();
                    RaiseQuantityCommandStates();
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: card => card != null && CurrentDeck != null && CannAddToDeck(card));

            DecreaseAvailableCardCommand = new RelayCommand<CardData?>(execute: card =>
                {
                    if (card == null || CurrentDeck == null) return System.Threading.Tasks.Task.CompletedTask;

                    if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
                    {
                        var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                        if (existing != null)
                            DecreaseEvolveDeckQuantity(existing);
                    }
                    else if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                             (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        // no-op
                    }
                    else
                    {
                        var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                        if (existingMain != null)
                            DecreaseMainDeckQuantity(existingMain);
                    }

                    OnPropertyChanged(nameof(CurrentInDeckQuantity));
                    OnPropertyChanged(nameof(MainDeckCount));
                    OnPropertyChanged(nameof(EvolveDeckCount));
                    RefreshDeckLists();
                    RaiseQuantityCommandStates();
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                canExecute: CanDecreaseQuantity
                );

            // Subscribe to CardViewer changes so Add commands update when the viewed card changes
            CardViewer.PropertyChanged += CardViewer_PropertyChanged;
        }

        private void BumpDeckChangeTick()
        {
            _deckChangeTick++;
            OnPropertyChanged(nameof(DeckChangeTick));
        }

        private void RaiseQuantityCommandStates()
        {
            if (IncreaseMainDeckQuantityCommand is IRelayCommand incMainRc) incMainRc.RaiseCanExecuteChanged();
            if (DecreaseMainDeckQuantityCommand is IRelayCommand decMainRc) decMainRc.RaiseCanExecuteChanged();
            if (IncreaseEvolveDeckQuantityCommand is IRelayCommand incEvolveRc) incEvolveRc.RaiseCanExecuteChanged();
            if (DecreaseEvolveDeckQuantityCommand is IRelayCommand decEvolveRc) decEvolveRc.RaiseCanExecuteChanged();

            if (IncreaseCurrentCardQuantityCommand is IRelayCommand incCurRc) incCurRc.RaiseCanExecuteChanged();
            if (DecreaseCurrentCardQuantityCommand is IRelayCommand decCurRc) decCurRc.RaiseCanExecuteChanged();

            // Also update available-card commands
            if (IncreaseAvailableCardCommand is IRelayCommand incAvail) incAvail.RaiseCanExecuteChanged();
            if (DecreaseAvailableCardCommand is IRelayCommand decAvail) decAvail.RaiseCanExecuteChanged();
        }

        private void CardViewer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // CurrentCard can change when navigating versions/related cards; refresh add-button states.
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(CardViewer.CurrentCard))
            {
                if (AddToDeckCommand is IRelayCommand addDeckRc) addDeckRc.RaiseCanExecuteChanged();
                if (AddToMainDeckCommand is IRelayCommand addMainRc) addMainRc.RaiseCanExecuteChanged();
                if (AddToEvolveDeckCommand is IRelayCommand addEvolveRc) addEvolveRc.RaiseCanExecuteChanged();

                // Also update current-card quantity UI/commands
                OnPropertyChanged(nameof(CurrentInDeckQuantity));
                RaiseQuantityCommandStates();
            }
        }

        // Listen to property changes on the active Deck so UI updates when Leader/GloryCard/etc. change
        private void Deck_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // If deck raised change, refresh dependent viewmodel properties and lists
            var prop = e?.PropertyName ?? string.Empty;

            if (string.IsNullOrEmpty(prop) || prop == nameof(Deck.Leader1) || prop == nameof(Deck.Leader2) || prop == nameof(Deck.GloryCard) ||
                prop == nameof(Deck.Class1) || prop == nameof(Deck.Class2) || prop == nameof(Deck.Name))
            {
                OnPropertyChanged(nameof(Leader1Image));
                OnPropertyChanged(nameof(Leader2Image));
                OnPropertyChanged(nameof(Leader1Name));
                OnPropertyChanged(nameof(Leader2Name));
                OnPropertyChanged(nameof(GloryCardImage));
                OnPropertyChanged(nameof(ShowLeader2));
                OnPropertyChanged(nameof(ShowGloryCard));
                OnPropertyChanged(nameof(DeckName));

                // Refresh leaders and glory card lists
                RefreshLeadersAndGloryCard();

                // Commands that depend on leaders or classes may need refreshing
                if (AddToDeckCommand is IRelayCommand addDeckRc) addDeckRc.RaiseCanExecuteChanged();
                if (AddToMainDeckCommand is IRelayCommand addMainRc) addMainRc.RaiseCanExecuteChanged();
                if (AddToEvolveDeckCommand is IRelayCommand addEvolveRc) addEvolveRc.RaiseCanExecuteChanged();
            }

            // If main/evolve deck lists changed notify counts and refresh the list views.
            if (string.IsNullOrEmpty(prop) || prop == nameof(Deck.MainDeck) || prop == nameof(Deck.EvolveDeck))
            {
                RefreshDeckLists();
                OnPropertyChanged(nameof(MainDeckCount));
                OnPropertyChanged(nameof(EvolveDeckCount));
                OnPropertyChanged(nameof(CurrentInDeckQuantity)); // update viewer count when deck contents change
                RaiseQuantityCommandStates();
            }
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

            var dlg = new ConfirmDialog
            {
                Owner = System.Windows.Application.Current.MainWindow,
                Message = $"Are you sure you want to delete the deck '{CurrentDeck.Name}'?",
                Title = "Confirm Delete"
            };

            if (dlg.ShowDialog() != true)
                return;

            _decks.Remove(CurrentDeck);
            CurrentDeck = null;
            IsEditingDeck = false;
        }

        private bool FilterValidCards(object? obj)
        {
            if (obj is not CardData card || CurrentDeck == null) return false;

            return IsValidForDeck(card);
        }

        private bool IsValidForDeck(CardData card)
        {
            if (CurrentDeck == null) return false;
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

        private bool CannAddToDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;

            if (!IsValidForDeck(card))
                return false;

            if (card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false)
                return CanAddLeader(card);
            else if (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false)
                return CanAddToExtraDeck(card);
            else if (card.Type?.Contains("Evolve", StringComparison.OrdinalIgnoreCase) ?? false)
                return CanAddToEvolveDeck(card);
            else
                return CanAddToMainDeck(card);
        }

        private bool CanAddLeader(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;

            return CurrentDeck.DeckType switch
            {
                DeckType.Standard => CanAddLeaderStandard(card),
                DeckType.Gloryfinder => CanAddLeaderGloryfinder(card),
                DeckType.CrossCraft => CanAddLeaderCrossCraft(card),
                _ => false
            };
        }

        private bool CanAddLeaderStandard(CardData card)
        {
            return CurrentDeck?.Leader1 is null;
        }

        private bool CanAddLeaderGloryfinder(CardData card)
        {
            return CurrentDeck?.Leader1 is null;
        }

        private bool CanAddLeaderCrossCraft(CardData card)
        {
            if (CurrentDeck?.Leader1 is null) return true;
            else if (CurrentDeck?.Leader2 is null && !CurrentDeck.Leader1.Class.Contains(card.Class, StringComparison.OrdinalIgnoreCase))
                return true;
            else
                return false;
        }

        private bool CanAddToExtraDeck(CardData card)
        {
            return true;
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

        // New: helpers to determine if the viewed card can be increased/decreased
        private bool CanIncreaseQuantityForViewedCard()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return false;

            if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                return existing != null ? CanIncreaseEvolveDeckQuantity(existing) : CanAddToEvolveDeck(card);
            }

            if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return false;
            }

            var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            return existingMain != null ? CanIncreaseMainDeckQuantity(existingMain) : CanAddToMainDeck(card);
        }

        private bool CanDecreaseQuantityForViewedCard()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return false;

            if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                return existing != null && existing.Quantity > 0;
            }

            if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return false;
            }

            var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            return existingMain != null && existingMain.Quantity > 0;
        }

        private void IncreaseQuantityForViewedCard()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return;

            if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                {
                    if (CanIncreaseEvolveDeckQuantity(existing))
                    {
                        IncreaseEvolveDeckQuantity(existing);
                    }
                }
                else if (CanAddToEvolveDeck(card))
                {
                    AddCardToEvolveDeck(card);
                }
            }
            else if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                // no-op for leaders/tokens
            }
            else
            {
                var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existingMain != null)
                {
                    if (CanIncreaseMainDeckQuantity(existingMain))
                    {
                        IncreaseMainDeckQuantity(existingMain);
                    }
                }
                else if (CanAddToMainDeck(card))
                {
                    AddCardToMainDeck(card);
                }
            }

            OnPropertyChanged(nameof(CurrentInDeckQuantity));
            RaiseQuantityCommandStates();
        }

        private void DecreaseQuantityForViewedCard()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return;

            if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                {
                    DecreaseEvolveDeckQuantity(existing);
                }
            }
            else if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                // no-op
            }
            else
            {
                var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existingMain != null)
                {
                    DecreaseMainDeckQuantity(existingMain);
                }
            }

            OnPropertyChanged(nameof(CurrentInDeckQuantity));
            RaiseQuantityCommandStates();
        }

        // New: available-cards helpers (used by per-row + / - buttons)
        private bool CanIncreaseQuantityForAvailableCard(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;

            if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                return existing != null ? CanIncreaseEvolveDeckQuantity(existing) : CanAddToEvolveDeck(card);
            }

            if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return false;
            }

            var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            return existingMain != null ? CanIncreaseMainDeckQuantity(existingMain) : CanAddToMainDeck(card);
        }

        private bool CanDecreaseQuantity(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;
            
            if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
                return CurrentDeck?.EvolveDeck.Any(e => e.Card.CardNumber == card.CardNumber) ?? false;

            if (card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false)
                return false;

            if (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false)
                return false;

            return CurrentDeck?.MainDeck.Any(e => e.Card.CardNumber == card.CardNumber) ?? false;
        }

        private bool CanDecreaseQuantityForAvailableCard(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;

            if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                return existing != null && existing.Quantity > 0;
            }

            if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return false;
            }

            var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            return existingMain != null && existingMain.Quantity > 0;
        }

        private void IncreaseQuantityForAvailableCard(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                {
                    if (CanIncreaseEvolveDeckQuantity(existing))
                        IncreaseEvolveDeckQuantity(existing);
                }
                else if (CanAddToEvolveDeck(card))
                {
                    AddCardToEvolveDeck(card);
                }
            }
            else if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                // no-op
            }
            else
            {
                var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existingMain != null)
                {
                    if (CanIncreaseMainDeckQuantity(existingMain))
                        IncreaseMainDeckQuantity(existingMain);
                }
                else if (CanAddToMainDeck(card))
                {
                    AddCardToMainDeck(card);
                }
            }

            RefreshDeckLists();
            OnPropertyChanged(nameof(MainDeckCount));
            OnPropertyChanged(nameof(EvolveDeckCount));
            OnPropertyChanged(nameof(CurrentInDeckQuantity));
            RaiseQuantityCommandStates();
        }

        private void DecreaseQuantityForAvailableCard(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            if (card.Type?.Contains("Evolved", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var existing = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existing != null)
                {
                    DecreaseEvolveDeckQuantity(existing);
                }
            }
            else if ((card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                // no-op
            }
            else
            {
                var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existingMain != null)
                {
                    DecreaseMainDeckQuantity(existingMain);
                }
            }

            RefreshDeckLists();
            OnPropertyChanged(nameof(MainDeckCount));
            OnPropertyChanged(nameof(EvolveDeckCount));
            OnPropertyChanged(nameof(CurrentInDeckQuantity));
            RaiseQuantityCommandStates();
        }

        private void AddCardToDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            if (card.Type?.Contains("Leader", StringComparison.OrdinalIgnoreCase) ?? false)
                AddLeaderToDeck(card);
            else if (card.Type?.Contains("Token", StringComparison.OrdinalIgnoreCase) ?? false)
                AddTokenToDeck(card);
            else if (card.Type?.Contains("Evolve", StringComparison.OrdinalIgnoreCase) ?? false)
                AddCardToEvolveDeck(card);
            else
                AddCardToMainDeck(card);

        }

        private void AddLeaderToDeck(CardData? card)
        {
            if (card == null || CurrentDeck == null) return;

            if (CurrentDeck.DeckType is DeckType.Standard or DeckType.Gloryfinder)
            {
                CurrentDeck.Leader1 = card;
                return;
            }

            if (CurrentDeck.Leader1 is null)
            {
                CurrentDeck.Leader1 = card;
            }
            else
            {
                CurrentDeck.Leader2 = card;
            }
        }

        private void AddTokenToDeck(CardData? card)
        {
            return;
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
            RaiseQuantityCommandStates();

            // notify bindings that depend on deck contents
            BumpDeckChangeTick();
        }

        private void RemoveCardFromMainDeck(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return;
            CurrentDeck.MainDeck.Remove(entry);
            RefreshDeckLists();
            OnPropertyChanged(nameof(MainDeckCount));
            RaiseQuantityCommandStates();

            BumpDeckChangeTick();
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
            RaiseQuantityCommandStates();

            BumpDeckChangeTick();
        }

        private void RemoveCardFromEvolveDeck(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return;
            CurrentDeck.EvolveDeck.Remove(entry);
            RefreshDeckLists();
            OnPropertyChanged(nameof(EvolveDeckCount));
            RaiseQuantityCommandStates();

            BumpDeckChangeTick();
        }

        private void IncreaseMainDeckQuantity(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return;

            // Ensure we don't exceed allowed limits
            if (!CanIncreaseMainDeckQuantity(entry)) return;

            entry.Quantity++;
            OnPropertyChanged(nameof(MainDeckCount));
            RaiseQuantityCommandStates();

            BumpDeckChangeTick();
        }

        private void DecreaseMainDeckQuantity(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return;

            // If only 1 left, removing should remove the entry entirely
            if (entry.Quantity <= 1)
            {
                CurrentDeck.MainDeck.Remove(entry);
                RefreshDeckLists();
            }
            else
            {
                entry.Quantity--;
            }

            OnPropertyChanged(nameof(MainDeckCount));
            RaiseQuantityCommandStates();

            BumpDeckChangeTick();
        }

        private void IncreaseEvolveDeckQuantity(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return;

            if (!CanIncreaseEvolveDeckQuantity(entry)) return;

            entry.Quantity++;
            OnPropertyChanged(nameof(EvolveDeckCount));
            RaiseQuantityCommandStates();

            BumpDeckChangeTick();
        }

        private void DecreaseEvolveDeckQuantity(DeckEntry? entry)
        {
            if (entry == null || CurrentDeck == null) return;

            // If only 1 left, remove the entry; otherwise decrement
            if (entry.Quantity <= 1)
            {
                CurrentDeck.EvolveDeck.Remove(entry);
                RefreshDeckLists();
            }
            else
            {
                entry.Quantity--;
            }

            OnPropertyChanged(nameof(EvolveDeckCount));
            RaiseQuantityCommandStates();

            BumpDeckChangeTick();
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

        private void RefreshLeadersAndGloryCard()
        {
            LeadersList.Clear();
            GloryCardList.Clear();

            if (CurrentDeck != null)
            {
                if (CurrentDeck.Leader1 != null)
                    LeadersList.Add(CurrentDeck.Leader1);
                if (CurrentDeck.Leader2 != null)
                    LeadersList.Add(CurrentDeck.Leader2);
                if (CurrentDeck.GloryCard != null)
                    GloryCardList.Add(CurrentDeck.GloryCard);
            }

            // Ensure UI bindings update
            OnPropertyChanged(nameof(LeadersList));
            OnPropertyChanged(nameof(GloryCardList));
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