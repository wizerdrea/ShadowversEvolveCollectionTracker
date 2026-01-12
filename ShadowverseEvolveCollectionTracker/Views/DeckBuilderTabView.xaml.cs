using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ShadowverseEvolveCardTracker.Models;
using ShadowverseEvolveCardTracker.ViewModels;

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

            // Keep popup DataContext in sync when the viewmodel (DataContext) changes
            DataContextChanged += DeckBuilderTabView_DataContextChanged;
        }

        private void DeckBuilderTabView_Loaded(object? sender, RoutedEventArgs e)
        {
            AttachHeaderContextMenuHandlers();

            if (AvailableCardsGrid != null)
                AvailableCardsGrid.LayoutUpdated += AvailableCardsGrid_LayoutUpdated;

            if (TraitsPopup != null)
                TraitsPopup.DataContext = this.DataContext;

            // Wire the Select All / Clear All buttons to viewmodel commands as a robust fallback
            if (SelectAllTraitsButton != null)
                SelectAllTraitsButton.Click += SelectAllTraitsButton_Click;
            if (ClearAllTraitsButton != null)
                ClearAllTraitsButton.Click += ClearAllTraitsButton_Click;
        }

        private void DeckBuilderTabView_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (AvailableCardsGrid != null)
                AvailableCardsGrid.LayoutUpdated -= AvailableCardsGrid_LayoutUpdated;

            DetachHeaderContextMenuHandlers();

            DataContextChanged -= DeckBuilderTabView_DataContextChanged;

            if (SelectAllTraitsButton != null)
                SelectAllTraitsButton.Click -= SelectAllTraitsButton_Click;
            if (ClearAllTraitsButton != null)
                ClearAllTraitsButton.Click -= ClearAllTraitsButton_Click;
        }

        private void DeckBuilderTabView_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            // Update popup DataContext so bindings inside it bind to the viewmodel
            if (TraitsPopup != null)
                TraitsPopup.DataContext = this.DataContext;
        }

        // ToggleButton checked -> open popup and rotate arrow up
        private void TraitsToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (TraitsPopup != null)
                TraitsPopup.IsOpen = true;

            if (TraitsArrowPath != null)
                TraitsArrowPath.RenderTransform = new RotateTransform(180);
        }

        // ToggleButton unchecked -> close popup and rotate arrow down
        private void TraitsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (TraitsPopup != null)
                TraitsPopup.IsOpen = false;

            if (TraitsArrowPath != null)
                TraitsArrowPath.RenderTransform = new RotateTransform(0);
        }

        // When popup closes (eg. clicking outside), ensure toggle is unchecked and arrow reset
        private void TraitsPopup_Closed(object sender, EventArgs e)
        {
            if (TraitsToggle != null && TraitsToggle.IsChecked == true)
            {
                // set to false without re-entering Checked handler (Unchecked will run)
                TraitsToggle.IsChecked = false;
            }

            if (TraitsArrowPath != null)
                TraitsArrowPath.RenderTransform = new RotateTransform(0);
        }

        private void SelectAllTraitsButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is DeckBuilderViewModel vm && vm.SelectAllTraitFiltersCommand != null)
            {
                var cmd = vm.SelectAllTraitFiltersCommand;
                if (cmd.CanExecute(null))
                    cmd.Execute(null);
                else
                {
                    foreach (var f in vm.TraitsFilters) f.IsChecked = true;
                    vm.ValidCards.Refresh();
                }
            }
        }

        private void ClearAllTraitsButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is DeckBuilderViewModel vm && vm.ClearAllTraitFiltersCommand != null)
            {
                var cmd = vm.ClearAllTraitFiltersCommand;
                if (cmd.CanExecute(null))
                    cmd.Execute(null);
                else
                {
                    foreach (var f in vm.TraitsFilters) f.IsChecked = false;
                    vm.ValidCards.Refresh();
                }
            }
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

        // Updated: correct event signature for DataGrid SelectionChanged
        private void AvailableCardsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AvailableCardsGrid.SelectedItem != null)
            {
                // Clear selections in other lists
                if (LeadersListBox != null)
                    LeadersListBox.SelectedItem = null;
                if (GloryCardListBox != null)
                    GloryCardListBox.SelectedItem = null;
                if (MainDeckListControl != null)
                    MainDeckListControl.SelectedItem = null;
                if (EvolveDeckListControl != null)
                    EvolveDeckListControl.SelectedItem = null;
                if (TokenDeckListControl != null)
                    TokenDeckListControl.SelectedItem = null;
            }
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
                if (MainDeckListControl != null)
                    MainDeckListControl.SelectedItem = null;
                if (EvolveDeckListControl != null)
                    EvolveDeckListControl.SelectedItem = null;
                if (TokenDeckListControl != null)
                    TokenDeckListControl.SelectedItem = null;
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
                if (MainDeckListControl != null)
                    MainDeckListControl.SelectedItem = null;
                if (EvolveDeckListControl != null)
                    EvolveDeckListControl.SelectedItem = null;
                if (TokenDeckListControl != null)
                    TokenDeckListControl.SelectedItem = null;
            }
        }

        private void MainDeckListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainDeckListControl != null && MainDeckListControl.SelectedItem != null)
            {
                // Clear selections in other lists
                if (AvailableCardsGrid != null)
                    AvailableCardsGrid.SelectedItem = null;
                if (LeadersListBox != null)
                    LeadersListBox.SelectedItem = null;
                if (GloryCardListBox != null)
                    GloryCardListBox.SelectedItem = null;
                if (EvolveDeckListControl != null)
                    EvolveDeckListControl.SelectedItem = null;
                if (TokenDeckListControl != null)
                    TokenDeckListControl.SelectedItem = null;
            }
        }

        private void EvolveDeckListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handler name left as-is because XAML still references it.
            if (EvolveDeckListControl != null && EvolveDeckListControl.SelectedItem != null)
            {
                // Clear selections in other lists
                if (AvailableCardsGrid != null)
                    AvailableCardsGrid.SelectedItem = null;
                if (LeadersListBox != null)
                    LeadersListBox.SelectedItem = null;
                if (GloryCardListBox != null)
                    GloryCardListBox.SelectedItem = null;
                if (MainDeckListControl != null)
                    MainDeckListControl.SelectedItem = null;
                if (TokenDeckListControl != null)
                    TokenDeckListControl.SelectedItem = null;
            }
        }

        // NEW: Token selection changed handler
        private void TokenDeckListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TokenDeckListControl != null && TokenDeckListControl.SelectedItem != null)
            {
                // Clear selections in other lists
                if (AvailableCardsGrid != null)
                    AvailableCardsGrid.SelectedItem = null;
                if (LeadersListBox != null)
                    LeadersListBox.SelectedItem = null;
                if (GloryCardListBox != null)
                    GloryCardListBox.SelectedItem = null;
                if (MainDeckListControl != null)
                    MainDeckListControl.SelectedItem = null;
                if (EvolveDeckListControl != null)
                    EvolveDeckListControl.SelectedItem = null;
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