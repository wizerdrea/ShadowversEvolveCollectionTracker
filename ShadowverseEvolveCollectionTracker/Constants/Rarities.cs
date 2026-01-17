namespace ShadowverseEvolveCardTracker.Constants
{
    public static class Rarities
    {
        public const string Bronze = "Bronze";
        public const string Silver = "Silver";
        public const string Gold = "Gold";
        public const string Legendary = "Legendary";
        public const string SuperLegendary = "Super Legendary";
        public const string Ultimate = "Ultimate";
        public const string Special = "Special";
        public const string Premium = "Premium";
        public const string Other = "-";

        public static readonly IReadOnlyList<string> AllRarities = new[]
        {
            Bronze,
            Silver,
            Gold,
            Legendary,
            SuperLegendary,
            Ultimate,
            Special,
            Premium,
            Other,
        };
    }
}
