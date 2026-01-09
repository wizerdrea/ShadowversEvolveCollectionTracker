using System.Windows;
using System.Windows.Input;
using ShadowverseEvolveCardTracker.ViewModels;

namespace ShadowverseEvolveCardTracker.Views
{
    public partial class CreateDeckWizardDialog : Window
    {
        public CreateDeckWizardDialog()
        {
            InitializeComponent();
        }

        private CreateDeckWizardViewModel ViewModel => (CreateDeckWizardViewModel)DataContext;

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Cancel();
            DialogResult = false;
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GoBack();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GoNext();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Cancel();
            DialogResult = false;
            Close();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Finish();
            DialogResult = true;
            Close();
        }
    }
}