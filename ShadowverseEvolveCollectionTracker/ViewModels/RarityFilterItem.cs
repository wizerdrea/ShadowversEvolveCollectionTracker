using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    /// <summary>
    /// Simple filter item representing one rarity option shown in the Rarity header context menu.
    /// </summary>
    public sealed class RarityFilterItem : INotifyPropertyChanged
    {
        private bool _isChecked;

        public RarityFilterItem(string name, bool isChecked = true)
        {
            Name = name;
            _isChecked = isChecked;
        }

        public string Name { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}