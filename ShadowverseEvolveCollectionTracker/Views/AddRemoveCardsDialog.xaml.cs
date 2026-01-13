using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ShadowverseEvolveCardTracker.ViewModels;

namespace ShadowverseEvolveCardTracker.Views
{
    public partial class AddRemoveCardsDialog : Window
    {
        private readonly HashSet<ContextMenu> _subscribedContextMenus = new();
        private DataGrid? _cardsDataGrid;

        public AddRemoveCardsDialog()
        {
            InitializeComponent();

            // Provide simple increase/decrease commands on the window for the DataGrid buttons to call
            IncreaseCommand = new RelayCommand<object?>(p =>
            {
                if (p is AddRemoveCardsViewModel.EditableCardEntry entry)
                {
                    entry.Delta++;
                }
                return System.Threading.Tasks.Task.CompletedTask;
            });

            DecreaseCommand = new RelayCommand<object?>(p =>
            {
                if (p is AddRemoveCardsViewModel.EditableCardEntry entry)
                {
                    entry.Delta--;
                }
                return System.Threading.Tasks.Task.CompletedTask;
            });

            Loaded += AddRemoveCardsDialog_Loaded;
            Unloaded += AddRemoveCardsDialog_Unloaded;

            DataContextChanged += AddRemoveCardsDialog_DataContextChanged;
        }

        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }

        private void AddRemoveCardsDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            // Find the first DataGrid in the visual tree (the dialog's cards grid)
            _cardsDataGrid = FindVisualChildren<DataGrid>(this).FirstOrDefault();

            if (_cardsDataGrid != null)
            {
                _cardsDataGrid.LayoutUpdated += CardsDataGrid_LayoutUpdated;
                AttachHeaderContextMenuHandlers();
            }
        }

        private void AddRemoveCardsDialog_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (_cardsDataGrid != null)
                _cardsDataGrid.LayoutUpdated -= CardsDataGrid_LayoutUpdated;

            DetachHeaderContextMenuHandlers();
        }

        private void CardsDataGrid_LayoutUpdated(object? sender, System.EventArgs e)
        {
            AttachHeaderContextMenuHandlers();
        }

        private void AttachHeaderContextMenuHandlers()
        {
            if (_cardsDataGrid == null) return;

            foreach (var header in FindVisualChildren<DataGridColumnHeader>(_cardsDataGrid))
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

        private void HeaderContextMenu_Opened(object? sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu cm)
            {
                // Attach the dialog's DataContext so bindings inside the ContextMenu resolve to the dialog viewmodel
                cm.DataContext = this.DataContext;
            }
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

        private void AddRemoveCardsDialog_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            // When the DataContext changes, ensure already-subscribed context menus get the new DataContext
            foreach (var cm in _subscribedContextMenus)
            {
                cm.DataContext = this.DataContext;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            int updated = 0;
            if (DataContext is AddRemoveCardsViewModel vm)
            {
                updated = vm.ApplyChanges();
            }

            // Show InfoDialog summarizing changes
            var info = new InfoDialog
            {
                Owner = this,
                Title = "Collection Updated",
                Message = updated == 0
                    ? "No card quantities were modified."
                    : $"Modified quantities for {updated} card(s)."
            };

            info.ShowDialog();

            DialogResult = true;
            Close();
        }

        // Make header draggable and support double-click maximize/restore to match main window behavior
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                try
                {
                    DragMove();
                }
                catch
                {
                    // ignore if drag fails (e.g., immediate mouse up)
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            MaximizeRestoreWindow();
        }

        private void MaximizeRestoreWindow()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}