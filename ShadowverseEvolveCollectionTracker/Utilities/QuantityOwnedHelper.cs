using ShadowverseEvolveCardTracker.ViewModels;

namespace ShadowverseEvolveCardTracker.Utilities
{
    public static class QuantityOwnedHelper
    {
        public static string Owned = "Owned";
        public static string Unowned = "Unowned";

        public static IEnumerable<RarityFilterItem> GetFilters()
        {
            return new List<RarityFilterItem>()
            {
                new RarityFilterItem(Owned, isChecked: true),
                new RarityFilterItem(Unowned, isChecked: true)
            };
        }
    }
}
