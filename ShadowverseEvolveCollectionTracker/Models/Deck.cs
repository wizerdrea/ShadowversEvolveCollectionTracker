using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ShadowverseEvolveCardTracker.Models
{
    /// <summary>
    /// Represents a complete deck with leader(s), glory card, main deck, and evolve deck.
    /// </summary>
    public sealed class Deck : INotifyPropertyChanged
    {
        private string _name = "New Deck";
        private DeckType _deckType;
        private string _class1 = string.Empty;
        private string? _class2;
        private CardData? _leader1;
        private CardData? _leader2;
        private CardData? _gloryCard;

        public Guid Id { get; init; } = Guid.NewGuid();

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value ?? "New Deck";
                    OnPropertyChanged();
                }
            }
        }

        public DeckType DeckType
        {
            get => _deckType;
            set
            {
                if (_deckType != value)
                {
                    _deckType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Class1
        {
            get => _class1;
            set
            {
                if (_class1 != value)
                {
                    _class1 = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        public string? Class2
        {
            get => _class2;
            set
            {
                if (_class2 != value)
                {
                    _class2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public CardData? Leader1
        {
            get => _leader1;
            set
            {
                if (!ReferenceEquals(_leader1, value))
                {
                    _leader1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public CardData? Leader2
        {
            get => _leader2;
            set
            {
                if (!ReferenceEquals(_leader2, value))
                {
                    _leader2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public CardData? GloryCard
        {
            get => _gloryCard;
            set
            {
                if (!ReferenceEquals(_gloryCard, value))
                {
                    _gloryCard = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<DeckEntry> MainDeck { get; init; } = new();
        public List<DeckEntry> EvolveDeck { get; init; } = new();
        public List<DeckEntry> Tokens { get; init; } = new();

        // Validation helpers
        public bool IsValid
        {
            get
            {
                return DeckType switch
                {
                    DeckType.Standard => IsValidStandard,
                    DeckType.Gloryfinder => IsValidGloryfinder,
                    DeckType.CrossCraft => IsValidCrossCraft,
                    _ => false
                };
            }
        }

        private bool IsValidStandard
        {
            get
            {
                if (Leader1 == null) return false;
                int mainCount = MainDeck.Sum(e => e.Quantity);
                int evolveCount = EvolveDeck.Sum(e => e.Quantity);
                return mainCount >= 40 && mainCount <= 50 && evolveCount <= 10;
            }
        }

        private bool IsValidGloryfinder
        {
            get
            {
                if (Leader1 == null || GloryCard == null) return false;
                int mainCount = MainDeck.Sum(e => e.Quantity);
                int evolveCount = EvolveDeck.Sum(e => e.Quantity);
                
                // Main deck must have 50 unique cards
                if (mainCount != 50 || MainDeck.Any(e => e.Quantity > 1)) return false;
                
                // Evolve deck up to 20 unique cards
                if (evolveCount > 20 || EvolveDeck.Any(e => e.Quantity > 1)) return false;
                
                // Glory card cannot be in main deck
                if (MainDeck.Any(e => e.Card.CardNumber == GloryCard.CardNumber)) return false;
                
                return true;
            }
        }

        private bool IsValidCrossCraft
        {
            get
            {
                if (Leader1 == null || Leader2 == null) return false;
                int mainCount = MainDeck.Sum(e => e.Quantity);
                int evolveCount = EvolveDeck.Sum(e => e.Quantity);
                
                if (mainCount < 40 || mainCount > 50 || evolveCount > 10) return false;
                
                // Must have at least one card from each leader's class
                var hasClass1 = MainDeck.Any(e => string.Equals(e.Card.Class, Class1, StringComparison.OrdinalIgnoreCase));
                var hasClass2 = MainDeck.Any(e => string.Equals(e.Card.Class, Class2, StringComparison.OrdinalIgnoreCase));
                
                return hasClass1 && hasClass2;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}