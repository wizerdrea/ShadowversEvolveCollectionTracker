using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShadowverseEvolveCardTracker.Utilities
{
    public static class SetHelper
    {
        public static string ExtractSetName(string? setString)
        {
            if (string.IsNullOrWhiteSpace(setString))
                return string.Empty;

            // Accept straight (") and curly (“ ”) double quotes as delimiters
            char[] quoteChars = new[] { '"', '“', '”' };

            int first = setString.IndexOfAny(quoteChars);
            if (first < 0)
                return setString.Trim();

            int second = setString.IndexOfAny(quoteChars, first + 1);
            if (second <= first)
                return setString.Trim();

            return setString.Substring(first + 1, second - first - 1).Trim();
        }
    }
}
