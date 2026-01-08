using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShadowverseEvolveCardTracker.Models
{
    /// <summary>
    /// Represents a card entry within a deck with its quantity.
    /// </summary>
    public sealed class DeckEntry : INotifyPropertyChanged
    {
        private int _quantity;

        public CardData Card { get; init; } = null!;

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}