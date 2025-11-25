using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OSK
{
    public static class LocalizationTagPreserver
    {
        // Regex matches any HTML-like tag: opening, closing, or self-closing.
        // Examples matched: <color=#48AC0B>, </color>, <sprite name="foo"/>, <b>
        private static readonly Regex TagRegex = new Regex(@"<[^<>]+?>", RegexOptions.Compiled);

        // Preprocess a single text cell: replace tags with placeholders.
        // Returns processedText and a dictionary mapping placeholders back to original tags.
        public static string PreprocessCell(string input, out Dictionary<string, string> placeholderToTag)
        {
            placeholderToTag = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(input)) return input;

            int idx = 0;
            // Use MatchEvaluator to progressively replace and capture original tag
            var dictionary = placeholderToTag;
            string result = TagRegex.Replace(input, match =>
            {
                string tag = match.Value; // the original tag text, e.g. "<color=#48AC0B>"
                string token = $"__LOC_TAG_{idx}__";
                // Ensure uniqueness in case translator injects same token (idx ensures unique per preprocess call)
                dictionary[token] = tag;
                idx++;
                return token;
            });

            return result;
        }

        // Postprocess: restore placeholders back into tags using the provided map.
        // If a placeholder is missing in the map, it is left as-is.
        public static string PostprocessCell(string translated, Dictionary<string, string> placeholderToTag)
        {
            if (string.IsNullOrEmpty(translated) || placeholderToTag == null || placeholderToTag.Count == 0)
                return translated;

            // Replace placeholders back. We'll replace by longest tokens first just in case.
            // (Though tokens are all same prefix + int, this is safe.)
            foreach (var kv in placeholderToTag)
            {
                if (translated.Contains(kv.Key))
                    translated = translated.Replace(kv.Key, kv.Value);
            }

            return translated;
        }

        // Helper: Preprocess entire CSV represented as List<string[]>
        // Returns a list of maps for each cell so you can postprocess individually.
        // Usage pattern:
        //   var placeholdersForCells = PreprocessCsv(rows);
        //   send rows to translator (cells contain placeholders)
        //   after getting translatedRows: for each cell i,j call PostprocessCell(translatedRows[i][j], placeholdersForCells[i][j])
        public static Dictionary<(int row, int col), Dictionary<string, string>> PreprocessCsv(List<string[]> rows)
        {
            var map = new Dictionary<(int row, int col), Dictionary<string, string>>();
            if (rows == null) return map;
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int c = 0; c < row.Length; c++)
                {
                    string cell = row[c] ?? "";
                    var placeholders = new Dictionary<string, string>();
                    string processed = PreprocessCell(cell, out placeholders);
                    // replace in-place
                    row[c] = processed;
                    map[(r, c)] = placeholders;
                }
            }

            return map;
        }

        // Helper: PostprocessCsv - applies saved placeholder maps to translated rows
        public static void PostprocessCsv(List<string[]> rowsTranslated,
            Dictionary<(int row, int col), Dictionary<string, string>> placeholderMap)
        {
            if (rowsTranslated == null || placeholderMap == null) return;
            for (int r = 0; r < rowsTranslated.Count; r++)
            {
                var row = rowsTranslated[r];
                for (int c = 0; c < row.Length; c++)
                {
                    var key = (r, c);
                    if (placeholderMap.TryGetValue(key, out var map))
                    {
                        row[c] = PostprocessCell(row[c], map);
                    }
                }
            }
        }
    }
}