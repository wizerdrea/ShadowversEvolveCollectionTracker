using ShadowverseEvolveCardTracker.Constants;
using ShadowverseEvolveCardTracker.Models;
using ShadowverseEvolveCardTracker.Services;
using ShadowverseEvolveCardTracker.Utilities;
using ShadowverseEvolveCardTracker.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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

        // NEW: favorites-only filter flag
        private bool _favoritesOnly;

        // NEW: name/text filter backing fields
        private string? _nameFilter;
        private string? _textFilter;

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

        // NEW: Rarity filter items exposed to the header context menu
        public ObservableCollection<RarityFilterItem> RarityFilters { get; } = new();

        // NEW: Type filter items exposed to the header context menu
        public ObservableCollection<RarityFilterItem> TypeFilters { get; } = new();

        // NEW: Class filter items exposed to the header context menu
        public ObservableCollection<RarityFilterItem> ClassFilters { get; } = new();

        // NEW: Set filter items exposed to the header context menu
        public ObservableCollection<RarityFilterItem> SetFilters { get; } = new();

        // NEW: Cost filter items exposed to the header context menu
        public ObservableCollection<RarityFilterItem> CostFilters { get; } = new();

        // NEW: Owned (quantity) filter items exposed to the header context menu
        public ObservableCollection<RarityFilterItem> OwnedFilters { get; } = new();

        // NEW: In-deck quantity filters (0/1/2/3) or (9/1) for Gloryfinder
        public ObservableCollection<RarityFilterItem> InDeckFilters { get; } = new();

        // NEW: Traits filter collection (multi-select)
        public ObservableCollection<RarityFilterItem> TraitsFilters { get; } = new();

        #endregion

        #region Favorites filter

        public bool FavoritesOnly
        {
            get => _favoritesOnly;
            set
            {
                if (SetProperty(ref _favoritesOnly, value))
                    _validCards.Refresh();
            }
        }

        #endregion

        #region Name / Text / Traits filters

        public string? NameFilter
        {
            get => _nameFilter;
            set
            {
                if (SetProperty(ref _nameFilter, value))
                    _validCards.Refresh();
            }
        }

        public string? TextFilter
        {
            get => _textFilter;
            set
            {
                if (SetProperty(ref _textFilter, value))
                    _validCards.Refresh();
            }
        }

        // Commands for Traits dropdown select/clear
        public ICommand SelectAllTraitFiltersCommand { get; private set; }
        public ICommand ClearAllTraitFiltersCommand { get; private set; }

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

        // NEW: Single button label & visibility for glory actions (bound in view)
        public string SetGloryButtonLabel
        {
            get
            {
                var cur = CardViewer.CurrentCard;
                if (CurrentDeck == null || cur == null) return string.Empty;
                if (CurrentDeck.DeckType != DeckType.Gloryfinder) return string.Empty;
                if (CurrentDeck.GloryCard != null && CurrentDeck.GloryCard.CardNumber == cur.CardNumber)
                    return "Move to Main Deck";
                return "Set as Glory Card";
            }
        }

        public bool ShowSetGloryButton
        {
            get
            {
                var cur = CardViewer.CurrentCard;
                if (CurrentDeck == null || cur == null) return false;
                if (CurrentDeck.DeckType != DeckType.Gloryfinder) return false;

                // show when card meets glory candidate rules OR when viewing current glory card
                return IsValidGloryCandidate(cur) || (CurrentDeck.GloryCard != null && CurrentDeck.GloryCard.CardNumber == cur.CardNumber);
            }
        }

        public int CurrentInDeckQuantity
        {
            get
            {
                var card = CardViewer.CurrentCard;
                if (card == null || CurrentDeck == null) return 0;

                // If the card is a leader and assigned to either leader slot, present as 1 "in deck"
                if (_validationService.IsLeaderCard(card))
                {
                    if (CurrentDeck.Leader1?.CardNumber == card.CardNumber ||
                        CurrentDeck.Leader2?.CardNumber == card.CardNumber)
                    {
                        return 1;
                    }
                }

                // If card is the glory card, present as 1 "in deck"
                if (CurrentDeck.DeckType == DeckType.Gloryfinder && CurrentDeck.GloryCard?.CardNumber == card.CardNumber)
                {
                    return 1;
                }

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

        // NEW: single command that either sets the viewed card as glory or moves the current glory back to main
        public ICommand SetOrMoveGloryCommand { get; private set; }

        // NEW: commands used by the Rarity header context menu
        public ICommand SelectAllRarityFiltersCommand { get; private set; }
        public ICommand ClearAllRarityFiltersCommand { get; private set; }

        // NEW: commands used by the Type header context menu
        public ICommand SelectAllTypeFiltersCommand { get; private set; }
        public ICommand ClearAllTypeFiltersCommand { get; private set; }

        // NEW: commands used by the Class header context menu
        public ICommand SelectAllClassFiltersCommand { get; private set; }
        public ICommand ClearAllClassFiltersCommand { get; private set; }

        // NEW: commands used by the Set header context menu
        public ICommand SelectAllSetFiltersCommand { get; private set; }
        public ICommand ClearAllSetFiltersCommand { get; private set; }

        // NEW: commands used by the Cost header context menu
        public ICommand SelectAllCostFiltersCommand { get; private set; }
        public ICommand ClearAllCostFiltersCommand { get; private set; }

        // NEW: commands used by the Owned header context menu
        public ICommand SelectAllOwnedFiltersCommand { get; private set; }
        public ICommand ClearAllOwnedFiltersCommand { get; private set; }

        // NEW: commands used by the In-Deck header context menu
        public ICommand SelectAllInDeckFiltersCommand { get; private set; }
        public ICommand ClearAllInDeckFiltersCommand { get; private set; }

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
            // include favorites-only check to mirror AllCardsTab behavior
            _validCards.Filter = obj =>
            {
                if (obj is not CardData card) return true;
                if (CurrentDeck == null) return false;
                if (!_validationService.IsValidForDeck(card, CurrentDeck)) return false;
                if (FavoritesOnly && !card.IsFavorite) return false;

                // NEW: apply name regex filter
                if (!string.IsNullOrWhiteSpace(NameFilter))
                {
                    try
                    {
                        if (!Regex.IsMatch(card.Name ?? string.Empty, NameFilter!, RegexOptions.IgnoreCase))
                            return false;
                    }
                    catch (ArgumentException)
                    {
                        if (!card.Name?.Contains(NameFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                            return false;
                    }
                }

                // NEW: apply text regex filter
                if (!string.IsNullOrWhiteSpace(TextFilter))
                {
                    try
                    {
                        if (!Regex.IsMatch(card.Text ?? string.Empty, TextFilter!, RegexOptions.IgnoreCase))
                            return false;
                    }
                    catch (ArgumentException)
                    {
                        if (!card.Text?.Contains(TextFilter!, StringComparison.OrdinalIgnoreCase) ?? true)
                            return false;
                    }
                }

                // NEW: apply traits filters (multi-select) if defined (and not all checked)
                if (TraitsFilters.Count > 0)
                {
                    var checkedCount = TraitsFilters.Count(f => f.IsChecked);
                    if (checkedCount != TraitsFilters.Count) // only filter when some are unchecked
                    {
                        var cardParts = (card.Traits ?? string.Empty)
                            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim());
                        if (!cardParts.Any()) return false;

                        bool anyMatch = cardParts.Any(cp =>
                            TraitsFilters.Any(f => f.IsChecked &&
                                string.Equals(f.Name, cp, StringComparison.OrdinalIgnoreCase)));

                        if (!anyMatch) return false;
                    }
                }

                // NEW: apply rarity filters if defined (and not all checked)
                if (RarityFilters.Count > 0)
                {
                    var checkedCount = RarityFilters.Count(f => f.IsChecked);
                    if (checkedCount != RarityFilters.Count) // only filter when some are unchecked
                    {
                        var cardParts = (card.Rarity ?? string.Empty)
                            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim());
                        // If card has no rarity parts, exclude
                        if (!cardParts.Any()) return false;

                        bool anyMatch = cardParts.Any(cp =>
                            RarityFilters.Any(f => f.IsChecked &&
                                string.Equals(f.Name, cp, StringComparison.OrdinalIgnoreCase)));

                        if (!anyMatch) return false;
                    }
                }

                // NEW: apply type filters if defined (and not all checked)
                if (TypeFilters.Count > 0)
                {
                    var checkedCount = TypeFilters.Count(f => f.IsChecked);
                    if (checkedCount != TypeFilters.Count) // only filter when some are unchecked
                    {
                        var cardParts = (card.Type ?? string.Empty)
                            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim());
                        if (!cardParts.Any()) return false;

                        bool anyMatch = cardParts.Any(cp =>
                            TypeFilters.Any(f => f.IsChecked &&
                                string.Equals(f.Name, cp, StringComparison.OrdinalIgnoreCase)));

                        if (!anyMatch) return false;
                    }
                }

                // NEW: apply class filters if defined (and not all checked)
                if (ClassFilters.Count > 0)
                {
                    var checkedCount = ClassFilters.Count(f => f.IsChecked);
                    if (checkedCount != ClassFilters.Count) // only filter when some are unchecked
                    {
                        var cardClass = (card.Class ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(cardClass)) return false;

                        bool anyMatch = ClassFilters.Any(f => f.IsChecked &&
                            string.Equals(f.Name, cardClass, StringComparison.OrdinalIgnoreCase));

                        if (!anyMatch) return false;
                    }
                }

                // NEW: apply set filters if defined (and not all checked)
                if (SetFilters.Count > 0)
                {
                    var checkedCount = SetFilters.Count(f => f.IsChecked);
                    if (checkedCount != SetFilters.Count) // only filter when some are unchecked
                    {
                        var cardParts = (card.Set ?? string.Empty)
                            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim());
                        if (!cardParts.Any()) return false;

                        bool anyMatch = cardParts.Any(cp =>
                            SetFilters.Any(f => f.IsChecked &&
                                cp.Contains(f.Name, StringComparison.OrdinalIgnoreCase)));

                        if (!anyMatch) return false;
                    }
                }

                // NEW: apply cost filters if defined (and not all checked)
                if (CostFilters.Count > 0)
                {
                    var checkedCount = CostFilters.Count(f => f.IsChecked);
                    if (checkedCount != CostFilters.Count) // only filter when some are unchecked
                    {
                        var cardCost = (card.Cost ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(cardCost)) return false;

                        bool anyMatch = CostFilters.Any(f => f.IsChecked &&
                            string.Equals(f.Name, cardCost, StringComparison.OrdinalIgnoreCase));

                        if (!anyMatch) return false;
                    }
                }

                // NEW: apply owned filters if defined (and not all checked)
                if (OwnedFilters.Count > 0)
                {
                    var checkedCount = OwnedFilters.Count(f => f.IsChecked);
                    if (checkedCount != OwnedFilters.Count) // only filter when some are unchecked
                    {
                        var ownedCategory = card.QuantityOwned > 0 ? QuantityOwnedHelper.Owned : QuantityOwnedHelper.Unowned;

                        bool anyMatch = OwnedFilters.Any(f => f.IsChecked &&
                            string.Equals(f.Name, ownedCategory, StringComparison.OrdinalIgnoreCase));

                        if (!anyMatch) return false;
                    }
                }

                // NEW: apply in-deck quantity filters if defined (and not all checked)
                if (InDeckFilters.Count > 0)
                {
                    var checkedCount = InDeckFilters.Count(f => f.IsChecked);
                    if (checkedCount != InDeckFilters.Count)
                    {
                        // Determine in-deck quantity for this card relative to CurrentDeck
                        int inDeckQty = 0;
                        if (_validationService.IsNonDeckCard(card))
                        {
                            // but if it's a leader assigned to a slot, treat as 1
                            if (_validationService.IsLeaderCard(card) &&
                                (CurrentDeck.Leader1?.CardNumber == card.CardNumber ||
                                 CurrentDeck.Leader2?.CardNumber == card.CardNumber))
                            {
                                inDeckQty = 1;
                            }
                            else
                            {
                                inDeckQty = 0;
                            }
                        }
                        else
                        {
                            var deckList = _validationService.IsEvolvedCard(card) ? CurrentDeck.EvolveDeck : CurrentDeck.MainDeck;
                            var existing = deckList.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                            inDeckQty = existing?.Quantity ?? 0;
                        }

                        var qtyStr = inDeckQty.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        bool anyMatch = InDeckFilters.Any(f => f.IsChecked &&
                            string.Equals(f.Name, qtyStr, StringComparison.OrdinalIgnoreCase));

                        if (!anyMatch) return false;
                    }
                }

                return true;
            };

            // Initialize rarity, type and class filters from the card set (order by preferred list first)
            InitializeRarityFilters();
            InitializeTypeFilters();
            InitializeSetFilters();
            InitializeClassFilters();

            // NEW: initialize cost & owned filters
            InitializeCostFilters();
            InitializeOwnedFilters();
            InitializeInDeckFilters();

            // NEW: initialize traits filters
            InitializeTraitsFilters();

            // Subscribe to collection/card changes so filters update when card properties change
            if (_allCards is INotifyCollectionChanged incc)
                incc.CollectionChanged += AllCards_CollectionChanged;

            foreach (var c in _allCards)
                SubscribeToCard(c);

            // Initialize commands directly in constructor
            CreateDeckCommand = new RelayCommand(
                execute: () => { CreateNewDeck(); return System.Threading.Tasks.Task.CompletedTask; },
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

            // NEW: glory command - set or move depending on state
            SetOrMoveGloryCommand = new RelayCommand(
                execute: () => { ExecuteSetOrMoveGlory(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CanSetOrMoveGlory(CardViewer.CurrentCard));

            // NEW: select/clear commands for header menus
            SelectAllRarityFiltersCommand = new RelayCommand(
                execute: () => { SelectAllRarityFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => RarityFilters.Count > 0);

            ClearAllRarityFiltersCommand = new RelayCommand(
                execute: () => { ClearAllRarityFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => RarityFilters.Count > 0);

            SelectAllTypeFiltersCommand = new RelayCommand(
                execute: () => { SelectAllTypeFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => TypeFilters.Count > 0);

            ClearAllTypeFiltersCommand = new RelayCommand(
                execute: () => { ClearAllTypeFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => TypeFilters.Count > 0);

            // NEW: select/clear commands for class menu
            SelectAllClassFiltersCommand = new RelayCommand(
                execute: () => { SelectAllClassFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => ClassFilters.Count > 0);

            ClearAllClassFiltersCommand = new RelayCommand(
                execute: () => { ClearAllClassFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => ClassFilters.Count > 0);

            // NEW: select/clear commands for set menu
            SelectAllSetFiltersCommand = new RelayCommand(
                execute: () => { SelectAllSetFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SetFilters.Count > 0);

            ClearAllSetFiltersCommand = new RelayCommand(
                execute: () => { ClearAllSetFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => SetFilters.Count > 0);

            // NEW: select/clear commands for cost menu
            SelectAllCostFiltersCommand = new RelayCommand(
                execute: () => { SelectAllCostFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CostFilters.Count > 0);

            ClearAllCostFiltersCommand = new RelayCommand(
                execute: () => { ClearAllCostFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => CostFilters.Count > 0);

            // NEW: select/clear commands for owned menu
            SelectAllOwnedFiltersCommand = new RelayCommand(
                execute: () => { SelectAllOwnedFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            ClearAllOwnedFiltersCommand = new RelayCommand(
                execute: () => { ClearAllOwnedFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            SelectAllInDeckFiltersCommand = new RelayCommand(
                execute: () => { SelectAllInDeckFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            ClearAllInDeckFiltersCommand = new RelayCommand(
                execute: () => { ClearAllInDeckFilters(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => OwnedFilters.Count > 0);

            // NEW: traits select/clear commands
            SelectAllTraitFiltersCommand = new RelayCommand(
                execute: () => { SelectAllTraits(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => TraitsFilters.Count > 0);

            ClearAllTraitFiltersCommand = new RelayCommand(
                execute: () => { ClearAllTraits(); return System.Threading.Tasks.Task.CompletedTask; },
                canExecute: () => TraitsFilters.Count > 0);

            CardViewer.PropertyChanged += CardViewer_PropertyChanged;
        }

        #endregion

        #region AllCards collection change / card property subscription

        private void AllCards_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e?.OldItems != null)
            {
                foreach (var old in e.OldItems.OfType<CardData>())
                    UnsubscribeFromCard(old);
            }

            if (e?.NewItems != null)
            {
                foreach (var nw in e.NewItems.OfType<CardData>())
                    SubscribeToCard(nw);
            }

            // Rebuild trait list if card set changed
            InitializeTraitsFilters();
            _validCards.Refresh();
        }

        private void SubscribeToCard(CardData card)
        {
            if (card is INotifyPropertyChanged inpc)
                inpc.PropertyChanged += Card_PropertyChanged;
        }

        private void UnsubscribeFromCard(CardData card)
        {
            if (card is INotifyPropertyChanged inpc)
                inpc.PropertyChanged -= Card_PropertyChanged;
        }

        private void Card_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName)) return;

            // If one of the properties that affect filtering changed, refresh view
            if (e.PropertyName == nameof(CardData.Name) ||
                e.PropertyName == nameof(CardData.Text) ||
                e.PropertyName == nameof(CardData.Traits) ||
                e.PropertyName == nameof(CardData.Rarity) ||
                e.PropertyName == nameof(CardData.Set) ||
                e.PropertyName == nameof(CardData.Type) ||
                e.PropertyName == nameof(CardData.Class) ||
                e.PropertyName == nameof(CardData.Cost) ||
                e.PropertyName == nameof(CardData.QuantityOwned) ||
                e.PropertyName == nameof(CardData.IsFavorite))
            {
                _validCards.Refresh();
            }
        }

        #endregion

        #region Traits Filter Initialization & Handling

        private void InitializeTraitsFilters()
        {
            try
            {
                TraitsFilters.Clear();

                var traits = _allCards
                    .SelectMany(c => (c.Traits ?? string.Empty).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var t in traits)
                {
                    var item = new RarityFilterItem(t, isChecked: true);
                    item.PropertyChanged += TraitFilterItem_PropertyChanged;
                    TraitsFilters.Add(item);
                }
            }
            catch
            {
                // swallow - no traits available
            }
        }

        private void SelectAllTraits()
        {
            foreach (var f in TraitsFilters) f.IsChecked = true;
            _validCards.Refresh();
        }

        private void ClearAllTraits()
        {
            foreach (var f in TraitsFilters) f.IsChecked = false;
            _validCards.Refresh();
        }

        private void TraitFilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                _validCards.Refresh();
            }
        }

        #endregion

        #region Rarity Filter Initialization & Handling

        private void InitializeRarityFilters()
        {
            try
            {
                // Initialize collection, default to checked (show all)
                RarityFilters.Clear();
                foreach (var rarity in Rarities.AllRarities)
                {
                    var item = new RarityFilterItem(rarity, isChecked: true);
                    item.PropertyChanged += RarityFilterItem_PropertyChanged;
                    RarityFilters.Add(item);
                }
            }
            catch
            {
                // swallow - no filters available
            }
        }

        private void SelectAllRarityFilters()
        {
            foreach (var f in RarityFilters) f.IsChecked = true;
            _validCards.Refresh();
        }

        private void ClearAllRarityFilters()
        {
            foreach (var f in RarityFilters) f.IsChecked = false;
            _validCards.Refresh();
        }

        private void RarityFilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                _validCards.Refresh();
            }
        }

        #endregion

        #region Type Filter Initialization & Handling

        private void InitializeTypeFilters()
        {
            try
            {
                // Initialize collection, default to checked (show all)
                TypeFilters.Clear();
                foreach (var t in CardTypes.AllCardTypes)
                {
                    var item = new RarityFilterItem(t, isChecked: true);
                    item.PropertyChanged += TypeFilterItem_PropertyChanged;
                    TypeFilters.Add(item);
                }
            }
            catch
            {
                // swallow - no filters available
            }
        }

        private void SelectAllTypeFilters()
        {
            foreach (var f in TypeFilters) f.IsChecked = true;
            _validCards.Refresh();
        }

        private void ClearAllTypeFilters()
        {
            foreach (var f in TypeFilters) f.IsChecked = false;
            _validCards.Refresh();
        }

        private void TypeFilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                _validCards.Refresh();
            }
        }

        #endregion

        #region Set Filter Initialization & Handling

        private void InitializeSetFilters()
        {
            try
            {
                SetFilters.Clear();

                // Build distinct set names from all cards; default to checked (show all)
                var sets = _allCards
                    .Select(c => (c.Set ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var s in sets)
                {
                    var item = new RarityFilterItem(SetHelper.ExtractSetName(s), isChecked: true);
                    item.PropertyChanged += SetFilterItem_PropertyChanged;
                    SetFilters.Add(item);
                }
            }
            catch
            {
                // swallow - no filters available
            }
        }

        private void SelectAllSetFilters()
        {
            foreach (var f in SetFilters) f.IsChecked = true;
            _validCards.Refresh();
        }

        private void ClearAllSetFilters()
        {
            foreach (var f in SetFilters) f.IsChecked = false;
            _validCards.Refresh();
        }

        private void SetFilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                _validCards.Refresh();
            }
        }

        #endregion

        #region Class Filter Initialization & Handling

        private void InitializeClassFilters()
        {
            try
            {
                ClassFilters.Clear();

                if (CurrentDeck is null) return;

                var classesToAdd = new List<String>();

                switch (CurrentDeck.DeckType)
                {
                    case DeckType.Standard:
                        classesToAdd.Add(CurrentDeck.Class1);
                        classesToAdd.Add(Classes.Neutral);
                        break;
                    case DeckType.CrossCraft:
                        classesToAdd.Add(CurrentDeck.Class1);
                        classesToAdd.Add(CurrentDeck.Class2 ?? string.Empty);
                        classesToAdd.Add(Classes.Neutral);
                        break;
                    case DeckType.Gloryfinder:
                        classesToAdd = Classes.AllClasses.ToList();
                        break;
                    default:
                        return;
                }

                foreach (var cls in classesToAdd)
                {
                    var item = new RarityFilterItem(cls, isChecked: true);
                    item.PropertyChanged += ClassFilterItem_PropertyChanged;
                    ClassFilters.Add(item);
                }
            }
            catch
            {
                // swallow - no filters available
            }
        }

        private void SelectAllClassFilters()
        {
            foreach (var f in ClassFilters) f.IsChecked = true;
            _validCards.Refresh();
        }

        private void ClearAllClassFilters()
        {
            foreach (var f in ClassFilters) f.IsChecked = false;
            _validCards.Refresh();
        }

        private void ClassFilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                _validCards.Refresh();
            }
        }

        #endregion

        #region Cost Filter Initialization & Handling

        private void InitializeCostFilters()
        {
            try
            {
                CostFilters.Clear();

                var costs = _allCards
                    .Select(c => (c.Cost ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s =>
                    {
                        // attempt numeric ordering when possible
                        if (int.TryParse(s, out var n)) return (n, s);
                        return (int.MaxValue, s);
                    })
                    .ToList();

                foreach (var cost in costs)
                {
                    var item = new RarityFilterItem(cost, isChecked: true);
                    item.PropertyChanged += CostFilterItem_PropertyChanged;
                    CostFilters.Add(item);
                }
            }
            catch
            {
                // swallow - no filters available
            }
        }

        private void SelectAllCostFilters()
        {
            foreach (var f in CostFilters) f.IsChecked = true;
            _validCards.Refresh();
        }

        private void ClearAllCostFilters()
        {
            foreach (var f in CostFilters) f.IsChecked = false;
            _validCards.Refresh();
        }

        private void CostFilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                _validCards.Refresh();
            }
        }

        #endregion

        #region Owned Filter Initialization & Handling

        private void InitializeOwnedFilters()
        {
            try
            {
                OwnedFilters.Clear();

                foreach (var f in QuantityOwnedHelper.GetFilters())
                {
                    // attach the change handler and reuse the item
                    f.PropertyChanged += OwnedFilterItem_PropertyChanged;
                    OwnedFilters.Add(f);
                }
            }
            catch
            {
                // swallow - no filters available
            }
        }

        private void SelectAllOwnedFilters()
        {
            foreach (var f in OwnedFilters) f.IsChecked = true;
            _validCards.Refresh();
        }

        private void ClearAllOwnedFilters()
        {
            foreach (var f in OwnedFilters) f.IsChecked = false;
            _validCards.Refresh();
        }

        private void OwnedFilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                _validCards.Refresh();
            }
        }

        #endregion

        #region In-Deck Filter Initialization & Handling

        private void InitializeInDeckFilters()
        {
            try
            {
                InDeckFilters.Clear();

                if (CurrentDeck == null)
                    return;

                IEnumerable<string> options;
                if (CurrentDeck.DeckType == DeckType.Gloryfinder)
                {
                    // Gloryfinder: options "9" and "1" per request
                    options = new[] { "0", "1" };
                }
                else
                {
                    // Standard / CrossCraft: options 0..3
                    options = new[] { "0", "1", "2", "3" };
                }

                foreach (var opt in options)
                {
                    var item = new RarityFilterItem(opt, isChecked: true);
                    item.PropertyChanged += InDeckFilterItem_PropertyChanged;
                    InDeckFilters.Add(item);
                }
            }
            catch
            {
                // swallow
            }
        }

        private void SelectAllInDeckFilters()
        {
            foreach (var f in InDeckFilters) f.IsChecked = true;
            _validCards.Refresh();
        }

        private void ClearAllInDeckFilters()
        {
            foreach (var f in InDeckFilters) f.IsChecked = false;
            _validCards.Refresh();
        }

        private void InDeckFilterItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.PropertyName) || e.PropertyName == nameof(RarityFilterItem.IsChecked))
            {
                _validCards.Refresh();
            }
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

            // If the card we decreased is a leader and its quantity reached zero (or it's no longer in the deck list),
            // clear the leader slot(s) it occupied.
            ClearLeaderSlotIfQuantityZero(entry.Card);

            NotifyDeckChanged();
        }

        private void ExecuteIncreaseViewedCardQuantity()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return;

            // Allow increasing/assigning leader via the + button when viewing the card.
            if (_validationService.IsLeaderCard(card))
            {
                if (TryAssignLeaderToOpenSlot(card))
                {
                    NotifyDeckChanged();
                }
                return;
            }

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

            // If the viewed card is the current glory card, pressing - should clear the glory slot
            if (CurrentDeck.DeckType == DeckType.Gloryfinder && CurrentDeck.GloryCard?.CardNumber == card.CardNumber)
            {
                CurrentDeck.GloryCard = null;
                RefreshLeadersAndGloryCard();
                NotifyDeckChanged();
                return;
            }

            // Special handling for leader cards: allow decreasing (clearing the slot) even if they are non-deck.
            if (_validationService.IsLeaderCard(card))
            {
                var removed = false;

                if (CurrentDeck.Leader1?.CardNumber == card.CardNumber)
                {
                    CurrentDeck.Leader1 = null;
                    removed = true;
                }

                if (CurrentDeck.Leader2?.CardNumber == card.CardNumber)
                {
                    CurrentDeck.Leader2 = null;
                    removed = true;
                }

                // Also decrease any deck entry quantities if present
                var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existingMain != null)
                {
                    _operationsHandler.DecreaseQuantity(existingMain, CurrentDeck, false);
                    RefreshDeckLists();
                    removed = true;
                }

                var existingEvolve = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existingEvolve != null)
                {
                    _operationsHandler.DecreaseQuantity(existingEvolve, CurrentDeck, true);
                    RefreshDeckLists();
                    removed = true;
                }

                if (removed)
                    NotifyDeckChanged();

                return;
            }

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

            // Allow increasing/assigning leader via the + button in the available cards grid.
            if (_validationService.IsLeaderCard(card))
            {
                if (TryAssignLeaderToOpenSlot(card))
                {
                    NotifyDeckChanged();
                }
                return;
            }

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

            // NEW: If the card is the current glory card in a Gloryfinder deck, pressing '-' in
            // the Available cards grid should clear the glory slot.
            if (CurrentDeck.DeckType == DeckType.Gloryfinder && CurrentDeck.GloryCard?.CardNumber == card.CardNumber)
            {
                CurrentDeck.GloryCard = null;
                RefreshLeadersAndGloryCard();
                NotifyDeckChanged();
                return;
            }

            // allow decreasing leader cards if they are assigned
            if (_validationService.IsLeaderCard(card))
            {
                var removed = false;

                if (CurrentDeck.Leader1?.CardNumber == card.CardNumber)
                {
                    CurrentDeck.Leader1 = null;
                    removed = true;
                }

                if (CurrentDeck.Leader2?.CardNumber == card.CardNumber)
                {
                    CurrentDeck.Leader2 = null;
                    removed = true;
                }

                // Also decrease any deck entry quantities if present
                var existingMain = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existingMain != null)
                {
                    _operationsHandler.DecreaseQuantity(existingMain, CurrentDeck, false);
                    RefreshDeckLists();
                    removed = true;
                }

                var existingEvolve = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
                if (existingEvolve != null)
                {
                    _operationsHandler.DecreaseQuantity(existingEvolve, CurrentDeck, true);
                    RefreshDeckLists();
                    removed = true;
                }

                if (removed)
                    NotifyDeckChanged();

                return;
            }

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

        // NEW: Set or Move Glory command handling
        private bool IsValidGloryCandidate(CardData card)
        {
            if (card == null || CurrentDeck == null) return false;
            if (CurrentDeck.DeckType != DeckType.Gloryfinder) return false;

            // valid glory: non-leader, non-token, non-evolved, of deck's class
            if (_validationService.IsLeaderCard(card)) return false;
            if (_validationService.IsTokenCard(card)) return false;
            if (_validationService.IsEvolvedCard(card)) return false;

            if (!string.Equals(card.Class, CurrentDeck.Class1, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private bool CanSetOrMoveGlory(CardData? card)
        {
            if (card == null || CurrentDeck == null) return false;
            if (CurrentDeck.DeckType != DeckType.Gloryfinder) return false;

            // If viewing the current glory card -> allow move if main deck can accept it
            if (CurrentDeck.GloryCard != null && CurrentDeck.GloryCard.CardNumber == card.CardNumber)
            {
                // can move to main deck if there's space and no duplicate already in main deck
                int currentCount = CurrentDeck.MainDeck.Sum(e => e.Quantity);
                bool alreadyInMain = CurrentDeck.MainDeck.Any(e => e.Card.CardNumber == card.CardNumber);
                return currentCount < 50 && !alreadyInMain;
            }

            // Else allow setting as glory if it meets candidate rules and there is no existing glory card
            return IsValidGloryCandidate(card) && CurrentDeck.GloryCard == null;
        }

        private void ExecuteSetOrMoveGlory()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return;

            // If current card is the deck's glory card -> move it to main deck (if possible)
            if (CurrentDeck.GloryCard != null && CurrentDeck.GloryCard.CardNumber == card.CardNumber)
            {
                // check capacity and duplicates
                int currentCount = CurrentDeck.MainDeck.Sum(e => e.Quantity);
                bool alreadyInMain = CurrentDeck.MainDeck.Any(e => e.Card.CardNumber == card.CardNumber);
                if (currentCount < 50 && !alreadyInMain)
                {
                    CurrentDeck.MainDeck.Add(new DeckEntry { Card = card, Quantity = 1 });
                    CurrentDeck.GloryCard = null;
                    RefreshDeckLists();
                    RefreshLeadersAndGloryCard();
                    NotifyDeckChanged();
                }
                return;
            }

            // Setting a new glory card
            if (!IsValidGloryCandidate(card) || CurrentDeck.GloryCard != null) return;

            // If card is present in main deck remove it
            var existing = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber);
            if (existing != null)
            {
                CurrentDeck.MainDeck.Remove(existing);
            }

            CurrentDeck.GloryCard = card;
            RefreshLeadersAndGloryCard();
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

            // allow leader removal even if it's a "non-deck" card, but only when assigned to a leader slot
            if (_validationService.IsNonDeckCard(card))
            {
                if (_validationService.IsLeaderCard(card))
                {
                    return CurrentDeck.Leader1?.CardNumber == card.CardNumber ||
                           CurrentDeck.Leader2?.CardNumber == card.CardNumber;
                }

                return false;
            }

            if (CurrentDeck.DeckType is DeckType.Gloryfinder &&
                !string.IsNullOrWhiteSpace(CurrentDeck.GloryCard?.CardNumber) &&
                CurrentDeck.GloryCard.CardNumber == card.CardNumber)
            {
                return true;
            }

            if (_validationService.IsEvolvedCard(card))
                return CurrentDeck.EvolveDeck.Any(e => e.Card.CardNumber == card.CardNumber);

            return CurrentDeck.MainDeck.Any(e => e.Card.CardNumber == card.CardNumber);
        }

        private bool CanIncreaseViewedCardQuantity()
        {
            var card = CardViewer.CurrentCard;
            if (card == null || CurrentDeck == null) return false;

            // allow increasing leader via + when there is an open/valid leader slot
            if (_validationService.IsLeaderCard(card))
            {
                return _validationService.CanAddLeader(card, CurrentDeck);
            }

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

            // If the viewed card is the current glory card -> allow '-' to clear it
            if (CurrentDeck.DeckType == DeckType.Gloryfinder && CurrentDeck.GloryCard?.CardNumber == card.CardNumber)
                return true;

            // allow decreasing leader cards when they are assigned to a leader slot
            if (_validationService.IsNonDeckCard(card))
            {
                if (_validationService.IsLeaderCard(card))
                {
                    return CurrentDeck.Leader1?.CardNumber == card.CardNumber ||
                           CurrentDeck.Leader2?.CardNumber == card.CardNumber;
                }

                return false;
            }

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

                // Update glory button state/label
                RaiseGloryCommandStates();
                OnPropertyChanged(nameof(SetGloryButtonLabel));
                OnPropertyChanged(nameof(ShowSetGloryButton));
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

                // Glory-specific UI updates
                RaiseGloryCommandStates();
                OnPropertyChanged(nameof(SetGloryButtonLabel));
                OnPropertyChanged(nameof(ShowSetGloryButton));
            }

            if (string.IsNullOrEmpty(prop) || prop == nameof(Deck.MainDeck) || prop == nameof(Deck.EvolveDeck))
            {
                RefreshDeckLists();
                OnPropertyChanged(nameof(MainDeckCount));
                OnPropertyChanged(nameof(EvolveDeckCount));
                OnPropertyChanged(nameof(CurrentInDeckQuantity));
                RaiseQuantityCommandStates();
                EvaluateDeckValidity();

                // Reinitialize in-deck filters when deck contents or deck type change
                InitializeInDeckFilters();
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

            // Glory button state may have changed
            RaiseGloryCommandStates();
            OnPropertyChanged(nameof(SetGloryButtonLabel));
            OnPropertyChanged(nameof(ShowSetGloryButton));
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

            InitializeClassFilters();
            InitializeSetFilters();

            // Ensure cost/owned filters reflect any card collection changes
            InitializeCostFilters();
            InitializeOwnedFilters();

            // In-deck filters depend on current deck type/contents
            InitializeInDeckFilters();

            // Ensure trait list reflects any card changes
            InitializeTraitsFilters();

            // Glory UI state
            RaiseGloryCommandStates();
            OnPropertyChanged(nameof(SetGloryButtonLabel));
            OnPropertyChanged(nameof(ShowSetGloryButton));
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

        /// <summary>
        /// If the provided card is assigned as a leader and the deck no longer contains any entries
        /// for that card (quantity zero), clear the leader slot(s) it occupied.
        /// </summary>
        private void ClearLeaderSlotIfQuantityZero(CardData card)
        {
            if (CurrentDeck == null) return;

            // determine remaining quantity in mains/evolves for this card
            var mainQty = CurrentDeck.MainDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber)?.Quantity ?? 0;
            var evolveQty = CurrentDeck.EvolveDeck.FirstOrDefault(e => e.Card.CardNumber == card.CardNumber)?.Quantity ?? 0;
            var totalQty = mainQty + evolveQty;

            if (totalQty == 0)
            {
                var cleared = false;
                if (CurrentDeck.Leader1?.CardNumber == card.CardNumber)
                {
                    CurrentDeck.Leader1 = null;
                    cleared = true;
                }

                if (CurrentDeck.Leader2?.CardNumber == card.CardNumber)
                {
                    CurrentDeck.Leader2 = null;
                    cleared = true;
                }

                if (cleared)
                {
                    // ensure UI and validation reflect change
                    RefreshLeadersAndGloryCard();
                    EvaluateDeckValidity();
                }
            }
        }

        /// <summary>
        /// Try to assign the provided leader card into an open valid leader slot.
        /// Returns true when assignment occurred.
        /// </summary>
        private bool TryAssignLeaderToOpenSlot(CardData card)
        {
            if (CurrentDeck == null) return false;

            if (!_validationService.CanAddLeader(card, CurrentDeck)) return false;

            // Assign to first available slot according to deck type.
            if (CurrentDeck.Leader1 == null)
            {
                CurrentDeck.Leader1 = card;
                RefreshLeadersAndGloryCard();
                EvaluateDeckValidity();
                return true;
            }

            if (CurrentDeck.DeckType == DeckType.CrossCraft && CurrentDeck.Leader2 == null)
            {
                CurrentDeck.Leader2 = card;
                RefreshLeadersAndGloryCard();
                EvaluateDeckValidity();
                return true;
            }

            return false;
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

            // Glory command state as part of global command state refresh
            RaiseGloryCommandStates();
        }

        private void RaiseGloryCommandStates()
        {
            RaiseCommand(SetOrMoveGloryCommand);
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