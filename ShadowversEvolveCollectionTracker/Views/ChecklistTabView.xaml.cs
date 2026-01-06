using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using ShadowversEvolveCardTracker.Models;

namespace ShadowversEvolveCardTracker.Views
{
    public partial class ChecklistTabView : UserControl
    {
        public ChecklistTabView()
        {
            InitializeComponent();
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
        }
    }
}