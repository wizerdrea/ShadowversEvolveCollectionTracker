using System.ComponentModel;
using System.Windows;
using ShadowversEvolveCardTracker.Services;
using ShadowversEvolveCardTracker.ViewModels;

namespace ShadowversEvolveCardTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Inject concrete services here; using interfaces in the VM keeps it testable.
            var loader = new CsvCardDataLoader();
            var folderService = new FolderDialogService();
            DataContext = new MainWindowViewModel(loader, folderService);

            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                // Save synchronously on close.
                vm.SaveAllCards();
            }
        }
    }
}