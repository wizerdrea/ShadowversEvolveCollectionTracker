using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.Views
{
    public partial class AllCardsTabView : UserControl
    {
        public AllCardsTabView()
        {
            InitializeComponent();
        }

        // Ensure the star shows favorited immediately when clicked in the DataGrid column.
        private void CardFavoriteToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton tb) return;
            if (tb.DataContext is not CardData card) return;

            // Toggle the underlying model and reflect state immediately in the control.
            bool newValue = !card.IsFavorite;
            card.IsFavorite = newValue;
            tb.IsChecked = newValue;
        }

        // Toggle wishlist: set desired quantity to 1 when turning on, 0 when turning off.
        private void CardWishlistToggle_Click(object? sender, RoutedEventArgs e)
        {
            ToggleButton? tb = sender as ToggleButton;
            CheckBox? cb = sender as CheckBox;

            object? src = (object?)tb ?? (object?)cb;
            if (src is null) return;

            if ((src as FrameworkElement)?.DataContext is not CardData card) return;

            bool currentlyWishlisted = card.IsWishlisted;
            bool newValue = !currentlyWishlisted;

            card.IsWishlisted = newValue;

            if (tb != null)
                tb.IsChecked = newValue;
            else if (cb != null)
                cb.IsChecked = newValue;
        }
    }
}