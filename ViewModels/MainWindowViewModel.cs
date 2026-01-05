using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ShadowversEvolveCardTracker.Models;
using ShadowversEvolveCardTracker.Services;

namespace ShadowversEvolveCardTracker.ViewModels
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ICardDataLoader _loader;
        private readonly IFolderDialogService _folderDialogService;

        public ObservableCollection<CardData> AllCards { get; } = new ObservableCollection<CardData>();

        // New: aggregated collection for the checklist UI
        public ObservableCollection<CombinedCardCount> CombinedCardCounts { get; } = new ObservableCollection<CombinedCardCount>();

        private readonly ICollectionView _filteredView;
        public ICollectionView FilteredCards => _filteredView;

        // Checklist view and regex filter
        private readonly ICollectionView _checklistView;
        public ICollectionView ChecklistView => _checklistView;

        private string? _checklistNameFilter;
        public string? ChecklistNameFilter
        {
            get => _checklistNameFilter;
            set
            {
                SetProperty(ref _checklistNameFilter, value);
                _checklistView.Refresh();
            }
        }

        // Owned/Unowned/Both filter (string-based for easy XAML binding)
        private string _checklistQtyFilter = "Both";
        public string ChecklistQtyFilter
        {
            get => _checklistQtyFilter;
            set
            {
                if (SetProperty(ref _checklistQtyFilter, value))
                {
                    _checklistView.Refresh();
                }
            }
        }

        // Selected combined group in Checklist and image navigation
        private CombinedCardCount? _selectedCombinedCard;
        public CombinedCardCount? SelectedCombinedCard
        {
            get => _selectedCombinedCard;
            set
            {
                if (SetProperty(ref _selectedCombinedCard, value))
                {
                    SelectedImageIndex = 0;
                    // notify dependent properties
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCombinedImage)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedImageIndexDisplay)));
                }
            }
        }

        private int _selectedImageIndex;
        public int SelectedImageIndex
        {
            get => _selectedImageIndex;
            set
            {
                if (SetProperty(ref _selectedImageIndex, value))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCombinedImage)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedImageIndexDisplay)));
                }
            }
        }

        public string? SelectedCombinedImage
        {
            get
            {
                if (SelectedCombinedCard == null) return null;
                var imgs = SelectedCombinedCard.Images;
                if (imgs == null || imgs.Count == 0) return null;
                if (SelectedImageIndex < 0) SelectedImageIndex = 0;
                if (SelectedImageIndex >= imgs.Count) SelectedImageIndex = imgs.Count - 1;
                return imgs.ElementAtOrDefault(SelectedImageIndex);
            }
        }

        public ICommand PrevImageCommand { get; }
        public ICommand NextImageCommand { get; }

        public ICommand LoadFolderCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand LoadImagesCommand { get; }

        private string _status = "Ready";
        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        private CardData? _selectedCard;
        public CardData? SelectedCard
        {
            get => _selectedCard;
            set => SetProperty(ref _selectedCard, value);
        }

        // Filter properties bound from headers
        private string? _nameFilter;
        public string? NameFilter { get => _nameFilter; set { SetProperty(ref _nameFilter, value); RefreshFiltered(); } }

        private string? _cardNumberFilter;
        public string? CardNumberFilter { get => _cardNumberFilter; set { SetProperty(ref _cardNumberFilter, value); RefreshFiltered(); } }

        private string? _rarityFilter;
        public string? RarityFilter { get => _rarityFilter; set { SetProperty(ref _rarityFilter, value); RefreshFiltered(); } }

        private string? _setFilter;
        public string? SetFilter { get => _setFilter; set { SetProperty(ref _setFilter, value); RefreshFiltered(); } }

        private string? _formatFilter;
        public string? FormatFilter { get => _formatFilter; set { SetProperty(ref _formatFilter, value); RefreshFiltered(); } }

        private string? _classFilter;
        public string? ClassFilter { get => _classFilter; set { SetProperty(ref _classFilter, value); RefreshFiltered(); } }

        private string? _typeFilter;
        public string? TypeFilter { get => _typeFilter; set { SetProperty(ref _typeFilter, value); RefreshFiltered(); } }

        private string? _traitsFilter;
        public string? TraitsFilter { get => _traitsFilter; set { SetProperty(ref _traitsFilter, value); RefreshFiltered(); } }

        private string? _textFilter;
        public string? TextFilter { get => _textFilter; set { SetProperty(ref _textFilter, value); RefreshFiltered(); } }

        private string _qtyOwnedFilter = "Both";
        public string QtyOwnedFilter
        {
            get => _qtyOwnedFilter;
            set
            {
                if (SetProperty(ref _qtyOwnedFilter, value))
                {
                    _filteredView.Refresh();
                }
            }
        }

        private static string GetSaveFilePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShadowversEvolveCardTracker");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "savedCards.json");
        }

        public MainWindowViewModel(ICardDataLoader loader, IFolderDialogService folderDialogService)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _folderDialogService = folderDialogService ?? throw new ArgumentNullException(nameof(folderDialogService));
            LoadFolderCommand = new RelayCommand(async () => await LoadFolderAsync(), () => true);
            SaveCommand = new RelayCommand(() => { SaveAllCards(); return Task.CompletedTask; }, () => true);
            LoadImagesCommand = new RelayCommand(async () => await LoadImagesAsync(), () => true);

            PrevImageCommand = new RelayCommand(() =>
            {
                if (SelectedCombinedCard == null) return Task.CompletedTask;
                if (SelectedImageIndex > 0) SelectedImageIndex--;
                return Task.CompletedTask;
            }, () => true);

            NextImageCommand = new RelayCommand(() =>
            {
                if (SelectedCombinedCard == null) return Task.CompletedTask;
                var count = SelectedCombinedCard.Images?.Count ?? 0;
                if (count > 0 && SelectedImageIndex < count - 1) SelectedImageIndex++;
                return Task.CompletedTask;
            }, () => true);

            _filteredView = CollectionViewSource.GetDefaultView(AllCards);
            _filteredView.Filter = FilterCard;

            // Create checklist view and set its filter (regex-based + qty filter)
            _checklistView = CollectionViewSource.GetDefaultView(CombinedCardCounts);
            _checklistView.Filter = ChecklistFilter;

            // Keep combined counts in sync with AllCards
            AllCards.CollectionChanged += AllCards_CollectionChanged;
            // If there are pre-existing items, subscribe to them and compute initial combined counts
            foreach (var c in AllCards)
                SubscribeToCard(c);
            RecalculateCombinedCounts();

            // Load previously-saved cards (fire-and-forget)
            _ = LoadSavedAsync();
        }

        private void RefreshFiltered()
        {
            _filteredView.Refresh();
        }

        private bool FilterCard(object? obj)
        {
            if (obj is not CardData c) return false;

            static bool MatchWithRegexOrSubstring(string? value, string? pattern)
            {
                if (string.IsNullOrWhiteSpace(pattern)) return true;
                if (string.IsNullOrWhiteSpace(value)) return false;

                try
                {
                    // Treat the pattern as a regular expression
                    return Regex.IsMatch(value, pattern!, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException)
                {
                    // Invalid regex: fallback to case-insensitive substring match
                    return value.Contains(pattern!, StringComparison.OrdinalIgnoreCase);
                }
            }

            if (!MatchWithRegexOrSubstring(c.Name, NameFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.CardNumber, CardNumberFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Rarity, RarityFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Set, SetFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Format, FormatFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Class, ClassFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Type, TypeFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Traits, TraitsFilter)) return false;
            if (!MatchWithRegexOrSubstring(c.Text, TextFilter)) return false;

            // Qty Owned filter
            switch (QtyOwnedFilter)
            {
                case "Owned":
                    if (c.QuantityOwned <= 0) return false;
                    break;
                case "Unowned":
                    if (c.QuantityOwned != 0) return false;
                    break;
                default: // "Both" or unknown -> accept all
                    break;
            }

            return true;
        }

        // Checklist filter using regex for name + Owned/Unowned/Both for total quantity
        private bool ChecklistFilter(object? obj)
        {
            if (obj is not CombinedCardCount group) return false;
            var name = group.Name ?? string.Empty;

            // Name regex filter
            if (!string.IsNullOrWhiteSpace(ChecklistNameFilter))
            {
                try
                {
                    if (!Regex.IsMatch(name, ChecklistNameFilter!, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    if (!name.Contains(ChecklistNameFilter!, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            // Qty filter
            switch (ChecklistQtyFilter)
            {
                case "Owned":
                    return group.TotalQuantityOwned > 0;
                case "Unowned":
                    return group.TotalQuantityOwned == 0;
                default: // "Both" or unknown
                    return true;
            }
        }

        private async Task LoadSavedAsync()
        {
            try
            {
                var path = GetSaveFilePath();
                if (!File.Exists(path)) return;

                string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<CardData>>(json, opts);
                if (list == null) return;

                var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                await dispatcher.InvokeAsync(() =>
                {
                    AllCards.Clear();
                    foreach (var c in list)
                    {
                        AllCards.Add(c);
                    }
                });
                Status = $"Loaded {list.Count} saved card(s).";
            }
            catch (Exception ex)
            {
                // Non-fatal: show status but continue
                Status = $"Failed to load saved cards: {ex.Message}";
            }
        }

        public void SaveAllCards()
        {
            try
            {
                var path = GetSaveFilePath();
                var list = new List<CardData>(AllCards);
                var opts = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(list, opts);
                File.WriteAllText(path, json, Encoding.UTF8);
                Status = $"Saved {list.Count} card(s).";
            }
            catch (Exception ex)
            {
                Status = $"Failed to save cards: {ex.Message}";
            }
        }

        private async Task LoadFolderAsync()
        {
            var folder = _folderDialogService.ShowFolderDialog("Select folder containing CSV files");
            if (string.IsNullOrWhiteSpace(folder))
            {
                Status = "Folder selection cancelled.";
                return;
            }

            Status = $"Loading CSV files from {folder} ...";

            try
            {
                var cards = await _loader.LoadAllAsync(folder).ConfigureAwait(false);

                // Ensure ObservableCollection is modified on the UI thread.
                var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                await dispatcher.InvokeAsync(async () =>
                {
                    foreach (var c in cards)
                    {
                        if (!CardAlreadyPresent(c))
                            AllCards.Add(c);
                    }
                });

                Status = $"Loaded {cards.Count} card(s) from {folder}";
            }
            catch (Exception ex)
            {
                Status = $"Error loading CSV files: {ex.Message}";
            }
        }

        private async Task LoadImagesAsync()
        {
            var folder = _folderDialogService.ShowFolderDialog("Select folder containing image files");
            if (string.IsNullOrWhiteSpace(folder))
            {
                Status = "Image folder selection cancelled.";
                return;
            }

            Status = $"Searching for images in {folder} ...";

            try
            {
                var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".tif", ".tiff" };
                var destDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShadowversEvolveCardTracker", "CardImages");
                Directory.CreateDirectory(destDir);

                var files = await Task.Run(() =>
                    Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                             .Where(f => extensions.Contains(Path.GetExtension(f)))
                             .ToList());

                int copied = 0;
                int skipped = 0;

                await Task.Run(() =>
                {
                    foreach (var src in files)
                    {
                        try
                        {
                            var dest = Path.Combine(destDir, Path.GetFileName(src));
                            if (!File.Exists(dest))
                            {
                                File.Copy(src, dest);
                                copied++;
                            }
                            else
                            {
                                skipped++;
                            }
                        }
                        catch
                        {
                            // ignore individual file copy failures
                        }
                    }
                }).ConfigureAwait(false);

                var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                await dispatcher.InvokeAsync(() =>
                {
                    Status = $"Images processed. Found {files.Count} files, copied {copied}, skipped {skipped}.";
                });
            }
            catch (Exception ex)
            {
                Status = $"Failed to load images: {ex.Message}";
            }
        }

        // Keep CombinedCardCounts in sync with AllCards
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

            RecalculateCombinedCounts();
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
            if (e.PropertyName == nameof(CardData.QuantityOwned) ||
                e.PropertyName == nameof(CardData.Name) ||
                e.PropertyName == nameof(CardData.Type))
            {
                RecalculateCombinedCounts();
            }
        }

        private void RecalculateCombinedCounts()
        {
            // Group by name + evolved flag so "Foo" and "Foo (Evolved)" are separate groups.
            var groups = AllCards
                .GroupBy(c => new
                {
                    Name = c.Name ?? string.Empty,
                    IsEvolved = !string.IsNullOrEmpty(c.Type) && c.Type.IndexOf("Evolved", StringComparison.OrdinalIgnoreCase) >= 0
                })
                .Select(g => new CombinedCardCount(g))
                .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            CombinedCardCounts.Clear();
            foreach (var item in groups)
                CombinedCardCounts.Add(item);

            // Ensure checklist view is refreshed so regex/qty filter applies to new groups
            _checklistView.Refresh();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                return true;
            }
            return false;
        }

        // Add this property to resolve CS0103
        public int SelectedImageIndexDisplay
        {
            get
            {
                var count = SelectedCombinedCard?.Images?.Count ?? 0;
                return count == 0 ? 0 : SelectedImageIndex + 1;
            }
        }

        private bool CardAlreadyPresent(CardData card)
        {
            foreach (var c in AllCards)
            {
                if (c.Name == card.Name && c.CardNumber == card.CardNumber)
                    return true;
            }
            return false;
        }
    }
}