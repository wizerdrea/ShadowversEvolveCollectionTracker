using System.IO;
using System.Text;
using ShadowverseEvolveCardTracker.Models;

namespace ShadowverseEvolveCardTracker.Services
{
    /// <summary>
    /// Reads CSV files and returns strongly-typed CardData objects.
    /// Default folderPath example: @"D:\Data\CardSetCsvs\"
    /// </summary>
    public sealed class CsvCardDataLoader : ICardDataLoader
    {
        public async Task<IReadOnlyList<CardData>> LoadAllAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("folderPath must be provided", nameof(folderPath));
            }

            var result = new List<CardData>();

            if (!Directory.Exists(folderPath))
            {
                return result;
            }

            // Enumerate CSV files in folder and all subdirectories, but do so safely to avoid
            // stopping on inaccessible folders.
            var files = EnumerateFilesSafe(folderPath, "*.csv").ToList();

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string text;
                try
                {
                    text = await File.ReadAllTextAsync(file, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Skip unreadable files
                    continue;
                }

                var records = ParseCsvRecords(text).ToList();

                if (records.Count == 0)
                {
                    continue;
                }

                // First record is header
                var header = records[0].Select(h => (h ?? string.Empty).Trim()).ToArray();

                // Map remaining records
                for (int r = 1; r < records.Count; r++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var row = records[r];
                    // CreateCardData performs mapping
                    result.Add(CreateCardData(row, header, file));
                }
            }

            return result;
        }

        // Safe recursive file enumerator that skips directories we cannot access.
        private static IEnumerable<string> EnumerateFilesSafe(string root, string searchPattern)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var dir = stack.Pop();
                string[] files = Array.Empty<string>();
                try
                {
                    files = Directory.GetFiles(dir, searchPattern);
                }
                catch
                {
                    // Skip directories we can't read
                }

                foreach (var f in files)
                {
                    yield return f;
                }

                string[] subdirs = Array.Empty<string>();
                try
                {
                    subdirs = Directory.GetDirectories(dir);
                }
                catch
                {
                    // Skip directories we can't enumerate
                }

                foreach (var sd in subdirs)
                {
                    stack.Push(sd);
                }
            }
        }

        // Helper to create CardData (avoids using 'with' on class)
        private static CardData CreateCardData(string[] row, string[] header, string sourceFile)
        {
            string GetByHeader(string name)
            {
                // Find index of header equal to name (case-insensitive)
                for (int i = 0; i < header.Length; i++)
                {
                    if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                    {
                        return i < row.Length ? row[i] : string.Empty;
                    }
                }
                return string.Empty;
            }

            return new CardData
            {
                Name = GetByHeader("Name"),
                CardNumber = GetByHeader("Card #"),
                Rarity = GetByHeader("Rarity"),
                Set = GetByHeader("Set"),
                Format = GetByHeader("Format"),
                Class = GetByHeader("Class"),
                Type = GetByHeader("Type"),
                Traits = GetByHeader("Traits"),
                Cost = GetByHeader("Cost"),
                Attack = GetByHeader("Attack"),
                Defense = GetByHeader("Defense"),
                Text = GetByHeader("Text"),
                QuantityOwned = 0
            };
        }

        // Basic CSV parser that handles quoted fields with commas and double-quote escaping.
        // Returns each record as string[]; preserves empty fields.
        private static IEnumerable<string[]> ParseCsvRecords(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '"')
                {
                    // Escaped quote
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++; // skip next quote
                        continue;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes)
                {
                    if (c == ',')
                    {
                        fields.Add(sb.ToString());
                        sb.Clear();
                        continue;
                    }

                    if (c == '\r')
                    {
                        continue;
                    }

                    if (c == '\n')
                    {
                        fields.Add(sb.ToString());
                        sb.Clear();
                        yield return fields.ToArray();
                        fields.Clear();
                        continue;
                    }
                }

                sb.Append(c);
            }

            // End of file: finalize last field/record
            if (sb.Length > 0 || fields.Count > 0)
            {
                fields.Add(sb.ToString());
                yield return fields.ToArray();
            }
        }
    }
}