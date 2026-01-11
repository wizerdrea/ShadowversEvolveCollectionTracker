using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShadowverseEvolveCardTracker.Constants
{
    public static class Classes
    {
        public const string Forestcraft = "Forestcraft";
        public const string Swordcraft = "Swordcraft";
        public const string Runecraft = "Runecraft";
        public const string Dragoncraft = "Dragoncraft";
        public const string Abysscraft = "Abysscraft";
        public const string Havencraft = "Havencraft";
        public const string Neutral = "Neutral";

        public static readonly IReadOnlyList<string> AllClasses = new[]
        {
            Forestcraft,
            Swordcraft,
            Runecraft,
            Dragoncraft,
            Abysscraft,
            Havencraft,
            Neutral,
        };
    }
}
