using ShadowversEvolveCardTracker.Services;
using ShadowversEvolveCardTracker.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ShadowversEvolveCardTracker
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Set window icon
            try
            {
                var iconUri = new Uri("pack://application:,,,/app_icon.ico");
                this.Icon = BitmapFrame.Create(iconUri);
            }
            catch
            {
                // Icon not found, ignore
            }

            // Inject concrete services here; using interfaces in the VM keeps it testable.
            var loader = new CsvCardDataLoader();
            var folderService = new FolderDialogService();
            DataContext = new MainWindowViewModel(loader, folderService);

            this.Closing += MainWindow_Closing;

            // Update maximize/restore button icon when window state changes
            StateChanged += OnWindowStateChanged;

            // TEMPORARY: Generate icon on first run
#if DEBUG
            // Uncomment the line below to generate the icon, then comment it out again
            // IconGenerator.GenerateAppIcon();
#endif
        }

        private void OnWindowStateChanged(object? sender, EventArgs e)
        {
            UpdateMaximizeRestoreButton();
        }

        private void UpdateMaximizeRestoreButton()
        {
            if (MaximizeRestoreButton != null)
            {
                MaximizeRestoreButton.ToolTip = WindowState == WindowState.Maximized ? "Restore Down" : "Maximize";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to maximize/restore
                MaximizeRestoreWindow();
            }
            else
            {
                // Single click to drag
                DragMove();
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                // Save synchronously on close.
                vm.SaveAllCards();
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