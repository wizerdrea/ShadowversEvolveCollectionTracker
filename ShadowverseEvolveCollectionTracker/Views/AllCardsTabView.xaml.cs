using ShadowverseEvolveCardTracker.Models;
using ShadowverseEvolveCardTracker.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ShadowverseEvolveCardTracker.Views
{
    public partial class AllCardsTabView : UserControl
    {
        private readonly HashSet<ContextMenu> _subscribedContextMenus = new();

        public AllCardsTabView()
        {
            InitializeComponent();

            Loaded += AllCardsTabView_Loaded;
            Unloaded += AllCardsTabView_Unloaded;
        }

        private void AllCardsTabView_Loaded(object? sender, RoutedEventArgs e)
        {
            AttachHeaderContextMenuHandlers();

            if (AllCardsDataGrid != null)
                AllCardsDataGrid.LayoutUpdated += AllCardsDataGrid_LayoutUpdated;

            if (SelectAllTraitsButton != null)
                SelectAllTraitsButton.Click += SelectAllTraitsButton_Click;
            if (ClearAllTraitsButton != null)
                ClearAllTraitsButton.Click += ClearAllTraitsButton_Click;
        }

        private void AllCardsTabView_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (AllCardsDataGrid != null)
                AllCardsDataGrid.LayoutUpdated -= AllCardsDataGrid_LayoutUpdated;

            if (SelectAllTraitsButton != null)
                SelectAllTraitsButton.Click -= SelectAllTraitsButton_Click;
            if (ClearAllTraitsButton != null)
                ClearAllTraitsButton.Click -= ClearAllTraitsButton_Click;

            DetachHeaderContextMenuHandlers();
        }

        private void AllCardsDataGrid_LayoutUpdated(object? sender, System.EventArgs e)
        {
            AttachHeaderContextMenuHandlers();
        }

        private void AttachHeaderContextMenuHandlers()
        {
            if (AllCardsDataGrid == null) return;

            foreach (var header in FindVisualChildren<DataGridColumnHeader>(AllCardsDataGrid))
            {
                var cm = header.ContextMenu;
                if (cm == null) continue;

                if (_subscribedContextMenus.Add(cm))
                {
                    cm.Opened += HeaderContextMenu_Opened;
                }
            }
        }

        private void DetachHeaderContextMenuHandlers()
        {
            foreach (var cm in _subscribedContextMenus)
            {
                cm.Opened -= HeaderContextMenu_Opened;
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

        // Ensure the star shows favorited immediately when clicked in the DataGrid column.
        private void CardFavoriteToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton tb) return;
            if (tb.DataContext is not CardData card) return;

            // Toggle the underlying model and reflect state immediately in the control.
            bool newValue = !card.IsFavorite;
            card.IsFavorite = newValue;
            tb.IsChecked = newValue;
        }

        // Toggle wishlist: set desired quantity to 1 when turning on, 0 when turning off.
        private void CardWishlistToggle_Click(object? sender, RoutedEventArgs e)
        {
            ToggleButton? tb = sender as ToggleButton;
            CheckBox? cb = sender as CheckBox;

            object? src = (object?)tb ?? (object?)cb;
            if (src is null) return;

            if ((src as FrameworkElement)?.DataContext is not CardData card) return;

            bool currentlyWishlisted = card.IsWishlisted;
            bool newValue = !currentlyWishlisted;

            card.IsWishlisted = newValue;

            if (tb != null)
                tb.IsChecked = newValue;
            else if (cb != null)
                cb.IsChecked = newValue;
        }

        // Set the ContextMenu.DataContext to the viewmodel when the header context menu opens.
        // This makes the header menu bindings (ItemsSource and Commands) resolve to the AllCardsTabViewModel.
        private void HeaderContextMenu_Opened(object? sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu cm)
            {
                cm.DataContext = this.DataContext;
            }
        }

        private void TraitsToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (TraitsPopup != null)
                TraitsPopup.IsOpen = true;

            if (TraitsArrowPath != null)
                TraitsArrowPath.RenderTransform = new RotateTransform(180);
        }

        private void TraitsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (TraitsPopup != null)
                TraitsPopup.IsOpen = false;

            if (TraitsArrowPath != null)
                TraitsArrowPath.RenderTransform = new RotateTransform(0);
        }

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
            if (DataContext is AllCardsTabViewModel vm && vm.SelectAllTraitFiltersCommand != null)
            {
                var cmd = vm.SelectAllTraitFiltersCommand;
                if (cmd.CanExecute(null))
                    cmd.Execute(null);
                else
                {
                    foreach (var f in vm.TraitsFilters) f.IsChecked = true;
                    vm.FilteredCards.Refresh();
                }
            }
        }

        private void ClearAllTraitsButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is AllCardsTabViewModel vm && vm.ClearAllTraitFiltersCommand != null)
            {
                var cmd = vm.ClearAllTraitFiltersCommand;
                if (cmd.CanExecute(null))
                    cmd.Execute(null);
                else
                {
                    foreach (var f in vm.TraitsFilters) f.IsChecked = false;
                    vm.FilteredCards.Refresh();
                }
            }
        }
    }
}