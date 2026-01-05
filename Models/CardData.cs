using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace ShadowversEvolveCardTracker.Models
{
    /// <summary>
    /// Strongly-typed representation of a row from the card CSV.
    /// Columns matched: Name, Card #, Rarity, Set, Format, Class, Type, Traits
    /// </summary>
    public sealed class CardData : INotifyPropertyChanged
    {
        private static string _saveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShadowversEvolveCardTracker", "CardImages");

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
                }
            }
        }

        public string ImageFile => Path.Join(_saveFolder, $"{CardNumber}.png");

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}