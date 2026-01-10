using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.Views
{
    public partial class DeckBuilderTabView : UserControl
    {
        private readonly HashSet<ContextMenu> _subscribedContextMenus = new();

        public DeckBuilderTabView()
        {
            InitializeComponent();

            Loaded += DeckBuilderTabView_Loaded;
            Unloaded += DeckBuilderTabView_Unloaded;
        }

        private void DeckBuilderTabView_Loaded(object? sender, RoutedEventArgs e)
        {
            AttachHeaderContextMenuHandlers();

            // Column headers can be recreated (layout changes) so re-attach if layout updates create new headers.
            if (AvailableCardsGrid != null)
                AvailableCardsGrid.LayoutUpdated += AvailableCardsGrid_LayoutUpdated;
        }

        private void DeckBuilderTabView_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (AvailableCardsGrid != null)
                AvailableCardsGrid.LayoutUpdated -= AvailableCardsGrid_LayoutUpdated;

            DetachHeaderContextMenuHandlers();
        }

        private void AvailableCardsGrid_LayoutUpdated(object? sender, System.EventArgs e)
        {
            AttachHeaderContextMenuHandlers();
        }

        private void AttachHeaderContextMenuHandlers()
        {
            if (AvailableCardsGrid == null) return;

            foreach (var header in FindVisualChildren<DataGridColumnHeader>(AvailableCardsGrid))
            {
                var cm = header.ContextMenu;
                if (cm == null) continue;

                if (_subscribedContextMenus.Add(cm))
                {
                    cm.Opened += RarityHeaderContextMenu_Opened;
                }
            }
        }

        private void DetachHeaderContextMenuHandlers()
        {
            foreach (var cm in _subscribedContextMenus)
            {
                cm.Opened -= RarityHeaderContextMenu_Opened;
            }

            _subscribedContextMenus.Clear();
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            var childCount = VisualTreeHelper.GetChildrenCount(depObj);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                    yield return t;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        private void AvailableCardsGrid_Selected(object sender, RoutedEventArgs e)
        {
            // Clear selections in other lists
            if (LeadersListBox != null)
                LeadersListBox.SelectedItem = null;
            if (GloryCardListBox != null)
                GloryCardListBox.SelectedItem = null;
            if (MainDeckListBox != null)
                MainDeckListBox.SelectedItem = null;
            if (EvolveDeckListBox != null)
                EvolveDeckListBox.SelectedItem = null;
        }

        private void LeadersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LeadersListBox.SelectedItem != null)
            {
                // Clear selections in other lists
                if (AvailableCardsGrid != null)
                    AvailableCardsGrid.SelectedItem = null;
                if (GloryCardListBox != null)
                    GloryCardListBox.SelectedItem = null;
                if (MainDeckListBox != null)
                    MainDeckListBox.SelectedItem = null;
                if (EvolveDeckListBox != null)
                    EvolveDeckListBox.SelectedItem = null;
            }
        }

        private void GloryCardListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GloryCardListBox.SelectedItem != null)
            {
                // Clear selections in other lists
                if (AvailableCardsGrid != null)
                    AvailableCardsGrid.SelectedItem = null;
                if (LeadersListBox != null)
                    LeadersListBox.SelectedItem = null;
                if (MainDeckListBox != null)
                    MainDeckListBox.SelectedItem = null;
                if (EvolveDeckListBox != null)
                    EvolveDeckListBox.SelectedItem = null;
            }
        }

        private void MainDeckListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainDeckListBox.SelectedItem != null)
            {
                // Clear selections in other lists
                if (AvailableCardsGrid != null)
                    AvailableCardsGrid.SelectedItem = null;
                if (LeadersListBox != null)
                    LeadersListBox.SelectedItem = null;
                if (GloryCardListBox != null)
                    GloryCardListBox.SelectedItem = null;
                if (EvolveDeckListBox != null)
                    EvolveDeckListBox.SelectedItem = null;
            }
        }

        private void EvolveDeckListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EvolveDeckListBox.SelectedItem != null)
            {
                // Clear selections in other lists
                if (AvailableCardsGrid != null)
                    AvailableCardsGrid.SelectedItem = null;
                if (LeadersListBox != null)
                    LeadersListBox.SelectedItem = null;
                if (GloryCardListBox != null)
                    GloryCardListBox.SelectedItem = null;
                if (MainDeckListBox != null)
                    MainDeckListBox.SelectedItem = null;
            }
        }

        // Ensure the star shows favorited immediately when clicked in the Available Cards grid.
        private void CardFavoriteToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton tb) return;
            if (tb.DataContext is not CardData card) return;

            // Toggle the underlying model and reflect state immediately in the control.
            bool newValue = !card.IsFavorite;
            card.IsFavorite = newValue;
            tb.IsChecked = newValue;
        }

        // Set the ContextMenu.DataContext to the viewmodel when the header context menu opens.
        // This makes the header menu bindings (ItemsSource and Commands) resolve to the DeckBuilderViewModel.
        private void RarityHeaderContextMenu_Opened(object? sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu cm)
            {
                // Attach the UserControl's DataContext (the viewmodel) so bindings inside the ContextMenu work.
                cm.DataContext = this.DataContext;
            }
        }
    }
}