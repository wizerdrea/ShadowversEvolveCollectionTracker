using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShadowverseEvolveCardTracker.Controls
{
    /// <summary>
    /// Interaction logic for DeckListControl.xaml
    /// </summary>
    public partial class DeckListControl : UserControl
    {
        public DeckListControl()
        {
            InitializeComponent();
        }

        // HeaderText
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(DeckListControl), new PropertyMetadata(string.Empty));

        public string HeaderText
        {
            get => (string)GetValue(HeaderTextProperty);
            set => SetValue(HeaderTextProperty, value);
        }

        // ItemsSource
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(DeckListControl), new PropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        // SelectedItem
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(DeckListControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        // IncreaseCommand
        public static readonly DependencyProperty IncreaseCommandProperty =
            DependencyProperty.Register(nameof(IncreaseCommand), typeof(ICommand), typeof(DeckListControl), new PropertyMetadata(null));

        public ICommand IncreaseCommand
        {
            get => (ICommand)GetValue(IncreaseCommandProperty);
            set => SetValue(IncreaseCommandProperty, value);
        }

        // DecreaseCommand
        public static readonly DependencyProperty DecreaseCommandProperty =
            DependencyProperty.Register(nameof(DecreaseCommand), typeof(ICommand), typeof(DeckListControl), new PropertyMetadata(null));

        public ICommand DecreaseCommand
        {
            get => (ICommand)GetValue(DecreaseCommandProperty);
            set => SetValue(DecreaseCommandProperty, value);
        }

        // Re-expose SelectionChanged so parent can attach handlers (keeps existing DeckBuilderTabView.xaml.cs)
        public event SelectionChangedEventHandler? SelectionChanged;

        private void PART_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }
    }
}
