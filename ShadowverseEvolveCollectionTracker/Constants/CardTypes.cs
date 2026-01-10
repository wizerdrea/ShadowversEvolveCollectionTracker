namespace ShadowverseEvolveCardTracker.Constants
{
    public static class CardTypes
    {
        public const string Leader = "Leader";
        public const string Follower = "Follower";
        public const string Evolved = "Evolved";
        public const string Spell = "Spell";
        public const string Amulet = "Amulet";
        public const string Token = "Token";

        public static readonly IReadOnlyList<string> AllCardTypes = new[]
        {
            Leader,
            Follower,
            Evolved,
            Spell,
            Amulet,
            Token
        };
    }
}
