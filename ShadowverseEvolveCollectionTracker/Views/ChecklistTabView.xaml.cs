using ShadowverseEvolveCardTracker.Models;
using ShadowverseEvolveCardTracker.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ShadowverseEvolveCardTracker.Views
{
    public partial class ChecklistTabView : UserControl
    {
        private readonly HashSet<ContextMenu> _subscribedContextMenus = new();

        public ChecklistTabView()
        {
            InitializeComponent();


            Loaded += ChecklistTabView_Loaded;
            Unloaded += ChecklistTabView_Unloaded;
        }

        private void ChecklistTabView_Loaded(object? sender, RoutedEventArgs e)
        {
            AttachHeaderContextMenuHandlers();

            if (ChecklistDataGrid != null)
                ChecklistDataGrid.LayoutUpdated += ChecklistDataGrid_LayoutUpdated;

            if (SelectAllSetsButton != null)
                SelectAllSetsButton.Click += SelectAllSetsButton_Click;
            if (ClearAllSetsButton != null)
                ClearAllSetsButton.Click += ClearAllSetsButton_Click;
        }

        private void ChecklistTabView_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (ChecklistDataGrid != null)
                ChecklistDataGrid.LayoutUpdated -= ChecklistDataGrid_LayoutUpdated;

            if (SelectAllSetsButton != null)
                SelectAllSetsButton.Click -= SelectAllSetsButton_Click;
            if (ClearAllSetsButton != null)
                ClearAllSetsButton.Click -= ClearAllSetsButton_Click;

            DetachHeaderContextMenuHandlers();
        }

        private void ChecklistDataGrid_LayoutUpdated(object? sender, System.EventArgs e)
        {
            AttachHeaderContextMenuHandlers();
        }

        private void AttachHeaderContextMenuHandlers()
        {
            if (ChecklistDataGrid == null) return;

            foreach (var header in FindVisualChildren<DataGridColumnHeader>(ChecklistDataGrid))
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

        // Toggle favorites for all cards in the combined group when user clicks the star.
        private void GroupFavoriteToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton tb) return;
            if (tb.DataContext is not CombinedCardCount group) return;

            // Toggle: if any card was favorite, clear all; otherwise set all favorite.
            bool newValue = !group.HasFavorite;
            foreach (var card in group.AllCards)
            {
                card.IsFavorite = newValue;
            }

            // Immediately reflect the new state in the toggle's visual so the UI shows the change right away.
            tb.IsChecked = newValue;
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

        private void SetsToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (SetsPopup != null)
                SetsPopup.IsOpen = true;

            if (SetsArrowPath != null)
                SetsArrowPath.RenderTransform = new RotateTransform(180);
        }

        private void SetsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SetsPopup != null)
                SetsPopup.IsOpen = false;

            if (SetsArrowPath != null)
                SetsArrowPath.RenderTransform = new RotateTransform(0);
        }

        private void SetsPopup_Closed(object sender, EventArgs e)
        {
            if (SetsToggle != null && SetsToggle.IsChecked == true)
            {
                // set to false without re-entering Checked handler (Unchecked will run)
                SetsToggle.IsChecked = false;
            }

            if (SetsArrowPath != null)
                SetsArrowPath.RenderTransform = new RotateTransform(0);
        }

        private void SelectAllSetsButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ChecklistTabViewModel vm && vm.SelectAllSetFiltersCommand != null)
            {
                var cmd = vm.SelectAllSetFiltersCommand;
                if (cmd.CanExecute(null))
                    cmd.Execute(null);
                else
                {
                    foreach (var f in vm.SetFilters) f.IsChecked = true;
                    vm.ChecklistView.Refresh();
                }
            }
        }

        private void ClearAllSetsButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ChecklistTabViewModel vm && vm.ClearAllSetFiltersCommand != null)
            {
                var cmd = vm.ClearAllSetFiltersCommand;
                if (cmd.CanExecute(null))
                    cmd.Execute(null);
                else
                {
                    foreach (var f in vm.SetFilters) f.IsChecked = false;
                    vm.ChecklistView.Refresh();
                }
            }
        }
    }
}