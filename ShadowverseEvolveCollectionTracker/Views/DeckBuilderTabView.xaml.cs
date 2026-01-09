using System.Windows;
using System.Windows.Controls;

namespace ShadowverseEvolveCardTracker.Views
{
    public partial class DeckBuilderTabView : UserControl
    {
        public DeckBuilderTabView()
        {
            InitializeComponent();
        }

        private void AvailableCardsGrid_Selected(object sender, RoutedEventArgs e)
        {
            // Clear selections in other lists
            if (LeadersListBox != null)
                LeadersListBox.SelectedItem = null;
            if (GloryCardListBox != null)
                GloryCardListBox.SelectedItem = null;
            if (MainDeckListBox != null)
                MainDeckListBox.SelectedItem = null;
            if (EvolveDeckListBox != null)
                EvolveDeckListBox.SelectedItem = null;
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
                if (MainDeckListBox != null)
                    MainDeckListBox.SelectedItem = null;
                if (EvolveDeckListBox != null)
                    EvolveDeckListBox.SelectedItem = null;
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
                if (MainDeckListBox != null)
                    MainDeckListBox.SelectedItem = null;
                if (EvolveDeckListBox != null)
                    EvolveDeckListBox.SelectedItem = null;
            }
        }

        private void MainDeckListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainDeckListBox.SelectedItem != null)
            {
                // Clear selections in other lists
                if (AvailableCardsGrid != null)
                    AvailableCardsGrid.SelectedItem = null;
                if (LeadersListBox != null)
                    LeadersListBox.SelectedItem = null;
                if (GloryCardListBox != null)
                    GloryCardListBox.SelectedItem = null;
                if (EvolveDeckListBox != null)
                    EvolveDeckListBox.SelectedItem = null;
            }
        }

        private void EvolveDeckListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EvolveDeckListBox.SelectedItem != null)
            {
                // Clear selections in other lists
                if (AvailableCardsGrid != null)
                    AvailableCardsGrid.SelectedItem = null;
                if (LeadersListBox != null)
                    LeadersListBox.SelectedItem = null;
                if (GloryCardListBox != null)
                    GloryCardListBox.SelectedItem = null;
                if (MainDeckListBox != null)
                    MainDeckListBox.SelectedItem = null;
            }
        }
    }
}