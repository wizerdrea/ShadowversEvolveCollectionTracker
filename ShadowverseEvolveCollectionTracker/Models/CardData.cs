using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace ShadowverseEvolveCardTracker.Models
{
    /// <summary>
    /// Strongly-typed representation of a row from the card CSV.
    /// Columns matched: Name, Card #, Rarity, Set, Format, Class, Type, Traits
    /// </summary>
    public sealed class CardData : INotifyPropertyChanged
    {
        private static string _saveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShadowverseEvolveCardTracker", "CardImages");

        static CardData()
        {
            // Ensure images folder exists
            Directory.CreateDirectory(_saveFolder);
        }

        public string Name { get; init; } = string.Empty;

        // Column header contains a space and hash; use a clear property name.
        public string CardNumber { get; init; } = string.Empty;

        public string Rarity { get; init; } = string.Empty;

        public string Set { get; init; } = string.Empty;

        public string Format { get; init; } = string.Empty;

        public string Class { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        // Traits may contain slashes and multiple values.
        public string Traits { get; init; } = string.Empty;

        public string Cost { get; init; } = string.Empty;
        public string Attack { get; init; } = string.Empty;
        public string Defense { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;

        // Related cards: use a serializable class instead of ValueTuple so JSON saves properly
        private HashSet<RelatedCard> _relatedCards = new();
        public HashSet<RelatedCard> RelatedCards
        {
            get => _relatedCards;
            set
            {
                if (!ReferenceEquals(_relatedCards, value))
                {
                    _relatedCards = value ?? new HashSet<RelatedCard>();
                    OnPropertyChanged();
                }
            }
        }

        // Quantity owned is editable in the UI; default is 0.
        private int _quantityOwned = 0;
        public int QuantityOwned
        {
            get => _quantityOwned;
            set
            {
                if (_quantityOwned != value)
                {
                    _quantityOwned = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MissingForPlayset));
                }
            }
        }

        // Favorite flag, default false. Editable from UI (Card viewer).
        private bool _isFavorite = false;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    OnPropertyChanged();
                }
            }
        }

        // Wishlist desired quantity: default 0. If >=1 card is "on the wishlist".
        private int _wishlistDesiredQuantity = 0;
        public int WishlistDesiredQuantity
        {
            get => _wishlistDesiredQuantity;
            set
            {
                var newValue = value < 0 ? 0 : value;
                if (_wishlistDesiredQuantity != newValue)
                {
                    _wishlistDesiredQuantity = newValue;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsWishlisted));
                }
            }
        }

        public bool IsWishlisted
        {
            get => WishlistDesiredQuantity > 0;
            set
            {
                var desired = value ? Math.Max(1, MissingForPlayset) : 0;
                if (!(WishlistDesiredQuantity > 0 && value))
                {
                    WishlistDesiredQuantity = desired; // existing setter raises OnPropertyChanged including IsWishlisted
                }
            }
        }

        /// <summary>
        /// Number of copies required to have a full playset for this card.
        /// Rules:
        /// - If Type contains "leader" or "token" (case-insensitive) OR Format equals "gloryfinder" (case-insensitive), playset size is 1.
        /// - Otherwise playset size is 3.
        /// </summary>
        public int CopiesNeededForPlayset
        {
            get
            {
                var type = Type ?? string.Empty;
                if (type.IndexOf("leader", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    type.IndexOf("token", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    string.Equals(Format, "gloryfinder", StringComparison.OrdinalIgnoreCase))
                {
                    return 1;
                }

                return 3;
            }
        }

        /// <summary>
        /// How many more copies are needed to reach a full playset (never negative).
        /// </summary>
        public int MissingForPlayset => Math.Max(0, CopiesNeededForPlayset - QuantityOwned);

        public string ImageFile => Path.Join(_saveFolder, $"{CardNumber}.png");

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}