using System.Windows;
using System.Windows.Input;

namespace ShadowverseEvolveCardTracker.Views
{
    public partial class InfoDialog : Window
    {
        public string Message { get; set; } = string.Empty;

        public InfoDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        // Close button in header (acts like cancel/close)
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Allow dragging the window by the header bar
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                    // ignore drag failures (e.g., rapid clicks)
                }
            }
        }
    }
}