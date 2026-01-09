using System.Windows;

namespace ShadowverseEvolveCardTracker.Views
{
    public partial class ConfirmDialog : Window
    {
        public string Message { get; set; } = string.Empty;

        public ConfirmDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}