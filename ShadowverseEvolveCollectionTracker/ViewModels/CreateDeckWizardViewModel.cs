using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    public class CreateDeckWizardViewModel : INotifyPropertyChanged
    {
        private readonly IEnumerable<CardData> _allCards;
        
        private int _currentStep = 1;
        private DeckType _selectedDeckType = DeckType.Standard;
        private string? _selectedClass1;
        private string? _selectedClass2;
        private string _deckName = string.Empty;

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    OnPropertyChanged(nameof(IsStep1));
                    OnPropertyChanged(nameof(IsStep2));
                    OnPropertyChanged(nameof(IsStep3));
                    OnPropertyChanged(nameof(CanGoNext));
                    OnPropertyChanged(nameof(CanGoBack));
                    OnPropertyChanged(nameof(CanFinish));
                }
            }
        }

        public bool IsStep1 => CurrentStep == 1;
        public bool IsStep2 => CurrentStep == 2;
        public bool IsStep3 => CurrentStep == 3;

        public DeckType SelectedDeckType
        {
            get => _selectedDeckType;
            set
            {
                if (SetProperty(ref _selectedDeckType, value))
                {
                    OnPropertyChanged(nameof(IsStandard));
                    OnPropertyChanged(nameof(IsGloryfinder));
                    OnPropertyChanged(nameof(IsCrossCraft));
                    OnPropertyChanged(nameof(NeedsTwoClasses));
                    OnPropertyChanged(nameof(CanGoNext));
                    
                    // Reset class selections when deck type changes
                    SelectedClass1 = null;
                    SelectedClass2 = null;
                }
            }
        }

        public bool IsStandard => SelectedDeckType == DeckType.Standard;
        public bool IsGloryfinder => SelectedDeckType == DeckType.Gloryfinder;
        public bool IsCrossCraft => SelectedDeckType == DeckType.CrossCraft;
        public bool NeedsTwoClasses => IsCrossCraft;

        public string? SelectedClass1
        {
            get => _selectedClass1;
            set
            {
                if (SetProperty(ref _selectedClass1, value))
                    OnPropertyChanged(nameof(CanGoNext));
            }
        }

        public string? SelectedClass2
        {
            get => _selectedClass2;
            set
            {
                if (SetProperty(ref _selectedClass2, value))
                    OnPropertyChanged(nameof(CanGoNext));
            }
        }

        public string DeckName
        {
            get => _deckName;
            set
            {
                if (SetProperty(ref _deckName, value))
                    OnPropertyChanged(nameof(CanFinish));
            }
        }

        public List<string> AvailableClasses { get; }

        public bool CanGoNext
        {
            get
            {
                return CurrentStep switch
                {
                    1 => true, // Can always go from step 1 (deck type is always selected)
                    2 => IsClassSelectionValid(),
                    _ => false
                };
            }
        }

        public bool CanGoBack => CurrentStep > 1;

        public bool CanFinish => CurrentStep == 3 && !string.IsNullOrWhiteSpace(DeckName);

        public bool DialogResult { get; set; }

        public CreateDeckWizardViewModel(IEnumerable<CardData> allCards)
        {
            _allCards = allCards ?? throw new ArgumentNullException(nameof(allCards));
            
            // Get available classes from cards
            AvailableClasses = _allCards
                .Select(c => c.Class)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public void GoNext()
        {
            if (CanGoNext && CurrentStep < 3)
            {
                CurrentStep++;
                
                // Auto-populate deck name if on step 3
                if (CurrentStep == 3 && string.IsNullOrWhiteSpace(DeckName))
                {
                    DeckName = GenerateDefaultDeckName();
                }
            }
        }

        public void GoBack()
        {
            if (CanGoBack)
            {
                CurrentStep--;
            }
        }

        public void Finish()
        {
            if (CanFinish)
            {
                DialogResult = true;
            }
        }

        public void Cancel()
        {
            DialogResult = false;
        }

        public Deck CreateDeck()
        {
            return new Deck
            {
                Name = DeckName,
                DeckType = SelectedDeckType,
                Class1 = SelectedClass1 ?? string.Empty,
                Class2 = NeedsTwoClasses ? SelectedClass2 : null
            };
        }

        private bool IsClassSelectionValid()
        {
            if (string.IsNullOrWhiteSpace(SelectedClass1))
                return false;

            if (NeedsTwoClasses)
            {
                if (string.IsNullOrWhiteSpace(SelectedClass2))
                    return false;
                
                // Ensure the two classes are different
                if (string.Equals(SelectedClass1, SelectedClass2, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private string GenerateDefaultDeckName()
        {
            var typeName = SelectedDeckType switch
            {
                DeckType.Standard => "Standard",
                DeckType.Gloryfinder => "Gloryfinder",
                DeckType.CrossCraft => "Cross Craft",
                _ => "Deck"
            };

            if (NeedsTwoClasses && !string.IsNullOrWhiteSpace(SelectedClass1) && !string.IsNullOrWhiteSpace(SelectedClass2))
            {
                return $"{SelectedClass1}/{SelectedClass2} {typeName}";
            }
            else if (!string.IsNullOrWhiteSpace(SelectedClass1))
            {
                return $"{SelectedClass1} {typeName}";
            }

            return $"New {typeName} Deck";
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