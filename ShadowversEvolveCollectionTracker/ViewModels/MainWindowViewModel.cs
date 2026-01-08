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
using System.Threading.Tasks;
using System.Windows;
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
        private readonly CardRelationsService _relationsService;

        public ObservableCollection<CardData> AllCards { get; } = new ObservableCollection<CardData>();
        public ObservableCollection<CombinedCardCount> CombinedCardCounts { get; } = new ObservableCollection<CombinedCardCount>();

        public AllCardsTabViewModel AllCardsTab { get; }
        public ChecklistTabViewModel ChecklistTab { get; }
        public SetCompletionTabViewModel SetCompletionTab { get; }

        public ICommand LoadFolderCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand LoadImagesCommand { get; }
        public ICommand FindCardRelationsCommand { get; }

        private string _status = "Ready";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
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
            _relationsService = new CardRelationsService();

            AllCardsTab = new AllCardsTabViewModel(AllCards);
            ChecklistTab = new ChecklistTabViewModel(CombinedCardCounts);
            SetCompletionTab = new SetCompletionTabViewModel(AllCards);

            LoadFolderCommand = new RelayCommand(async () => await LoadFolderAsync(), () => true);
            SaveCommand = new RelayCommand(() => { SaveAllCards(); return Task.CompletedTask; }, () => true);
            LoadImagesCommand = new RelayCommand(async () => await LoadImagesAsync(), () => true);
            FindCardRelationsCommand = new RelayCommand(async () => await FindCardRelationsAsync(), () => AllCards.Count > 0);

            AllCards.CollectionChanged += AllCards_CollectionChanged;
            foreach (var c in AllCards)
                SubscribeToCard(c);
            RecalculateCombinedCounts();

            // Wire up related cards functionality in card viewers
            AllCardsTab.CardViewer.RequestRelatedCards = ShowRelatedCards;
            ChecklistTab.CardViewer.RequestRelatedCards = ShowRelatedCards;

            // Wire up "other versions" requests
            AllCardsTab.CardViewer.RequestOtherVersions = ShowOtherVersions;
            ChecklistTab.CardViewer.RequestOtherVersions = ShowOtherVersions;

            // Provide function so viewer can know how many versions exist (same Name + Type)
            Func<CardData, int> countVersions = card =>
                AllCards.Count(c => string.Equals(c.Name, card.Name, StringComparison.OrdinalIgnoreCase)
                                  && string.Equals(c.Type, card.Type, StringComparison.OrdinalIgnoreCase));

            AllCardsTab.CardViewer.GetOtherVersionsCount = countVersions;
            ChecklistTab.CardViewer.GetOtherVersionsCount = countVersions;

            _ = LoadSavedAsync();
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
                ((RelayCommand)FindCardRelationsCommand).RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
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

                var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                await dispatcher.InvokeAsync(() =>
                {
                    foreach (var c in cards)
                    {
                        if (!CardAlreadyPresent(c))
                            AllCards.Add(c);
                    }
                });

                Status = $"Loaded {cards.Count} card(s) from {folder}";
                ((RelayCommand)FindCardRelationsCommand).RaiseCanExecuteChanged();
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

        private async Task FindCardRelationsAsync()
        {
            Status = "Finding card relations...";

            try
            {
                await Task.Run(() => _relationsService.FindCardRelationsAsync(AllCards)).ConfigureAwait(false);

                var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                await dispatcher.InvokeAsync(() =>
                {
                    var totalRelations = AllCards.Sum(c => c.RelatedCards?.Count ?? 0) / 2; // Divide by 2 because relations are bidirectional
                    Status = $"Found {totalRelations} card relation(s).";
                });
            }
            catch (Exception ex)
            {
                Status = $"Failed to find card relations: {ex.Message}";
            }
        }

        private void ShowRelatedCards(CardData sourceCard)
        {
            if (sourceCard?.RelatedCards == null || sourceCard.RelatedCards.Count == 0)
                return;

            var relatedCards = _relationsService.GetRelatedCardInstances(sourceCard, AllCards);
            
            if (relatedCards.Count > 0)
            {
                // Show in the appropriate card viewer based on which tab is active
                AllCardsTab.CardViewer.SetCards(relatedCards);
                ChecklistTab.CardViewer.SetCards(relatedCards);
                Status = $"Showing {relatedCards.Count} related card(s) for '{sourceCard.Name}'.";
            }
        }

        // New: show all versions (same Name and same Type)
        private void ShowOtherVersions(CardData sourceCard)
        {
            if (sourceCard == null) return;

            var versions = AllCards
                .Where(c => string.Equals(c.Name, sourceCard.Name, StringComparison.OrdinalIgnoreCase)
                         && string.Equals(c.Type, sourceCard.Type, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => string.Equals(c.Set, sourceCard.Set, StringComparison.OrdinalIgnoreCase) &&
                                       string.Equals(c.Rarity, sourceCard.Rarity, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(c => string.Equals(c.Set, sourceCard.Set, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(c => string.Equals(c.Rarity, sourceCard.Rarity, StringComparison.OrdinalIgnoreCase))
                .ThenBy(c => c.CardNumber, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (versions.Count == 0) return;

            // Show in both viewers
            AllCardsTab.CardViewer.SetCards(versions);
            ChecklistTab.CardViewer.SetCards(versions);

            // Set current index to the version matching the source card (by card number)
            int idx = versions.FindIndex(c => string.Equals(c.CardNumber, sourceCard.CardNumber, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                AllCardsTab.CardViewer.CurrentIndex = idx;
                ChecklistTab.CardViewer.CurrentIndex = idx;
            }

            Status = $"Showing {versions.Count} version(s) of '{sourceCard.Name}' ({sourceCard.Type}).";
        }

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
            ((RelayCommand)FindCardRelationsCommand).RaiseCanExecuteChanged();
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
    }
}