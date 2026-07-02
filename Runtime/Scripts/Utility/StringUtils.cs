using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace OSK
{
    public static class StringUtils
    {
        #region Example Usage

        // Split:
        // string[] parts = StringUtils.Split("a--b--c", "--");
        // string[] csv = StringUtils.SplitParams("id,name,value");
        //
        // Number parsing:
        // int amount = StringUtils.ConvertExcelToInt("$1,234");
        // float percent = StringUtils.ConvertExcelToFloat("12.5%");
        // List<int> numbers = StringUtils.ExtractInts("lv10_wave3");
        //
        // Text helpers:
        // string shortName = StringUtils.ShortenString(playerName, 12);
        // string readable = StringUtils.ToTitleCase("hello_world");
        //
        // Random text:
        // string code = StringUtils.Shuffle("ABCDEFG0123456789", 6);
        // string shuffled = StringUtils.Shuffle("abcdef");

        #endregion

        #region Split

        // Splits text by another string.
        public static string[] Split(string strValue, string splitValue)
        {
            if (string.IsNullOrEmpty(strValue))
            {
                return new string[0];
            }

            if (string.IsNullOrEmpty(splitValue))
            {
                return new[] { strValue };
            }

            return strValue.Split(new[] { splitValue }, StringSplitOptions.None);
        }

        // Splits comma-separated text.
        public static string[] SplitParams(string strValue)
        {
            if (string.IsNullOrEmpty(strValue))
            {
                return new string[0];
            }

            return strValue.Split(new[] { ',' }, StringSplitOptions.None);
        }

        // Splits comma-separated text and trims each part.
        public static string[] SplitParamsTrim(string strValue, bool removeEmpty = true)
        {
            string[] parts = SplitParams(strValue);
            List<string> result = new List<string>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                string value = parts[i].Trim();
                if (removeEmpty && string.IsNullOrEmpty(value))
                {
                    continue;
                }

                result.Add(value);
            }

            return result.ToArray();
        }

        #endregion

        #region Numbers

        // Extracts integer groups from each comma-separated section.
        public static List<int[]> GetNumbersFromString(string input)
        {
            List<int[]> resultList = new List<int[]>();
            if (string.IsNullOrEmpty(input))
            {
                return resultList;
            }

            string[] parts = SplitParams(input);
            for (int i = 0; i < parts.Length; i++)
            {
                resultList.Add(ExtractInts(parts[i]).ToArray());
            }

            return resultList;
        }

        // Extracts all integers from text.
        public static List<int> ExtractInts(string input)
        {
            List<int> numbers = new List<int>();
            if (string.IsNullOrEmpty(input))
            {
                return numbers;
            }

            MatchCollection matches = Regex.Matches(input, @"-?\d+");
            for (int i = 0; i < matches.Count; i++)
            {
                if (int.TryParse(matches[i].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    numbers.Add(value);
                }
            }

            return numbers;
        }

        // Extracts all floats from text.
        public static List<float> ExtractFloats(string input)
        {
            List<float> numbers = new List<float>();
            if (string.IsNullOrEmpty(input))
            {
                return numbers;
            }

            MatchCollection matches = Regex.Matches(input, @"-?\d+(\.\d+)?");
            for (int i = 0; i < matches.Count; i++)
            {
                if (float.TryParse(matches[i].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    numbers.Add(value);
                }
            }

            return numbers;
        }

        // Converts spreadsheet-like text to int.
        public static int ConvertExcelToInt(string input, int numFail = 0)
        {
            if (TryConvertExcelToFloat(input, out float value))
            {
                return Mathf.RoundToInt(value);
            }

            Debug.LogWarning("Unable to convert '" + input + "' to an integer.");
            return numFail;
        }

        // Converts spreadsheet-like text to float.
        public static float ConvertExcelToFloat(string input, int numFail = 0)
        {
            if (TryConvertExcelToFloat(input, out float value))
            {
                return value;
            }

            Debug.LogWarning("Unable to convert '" + input + "' to a float.");
            return numFail;
        }

        // Tries to convert spreadsheet-like text to float.
        public static bool TryConvertExcelToFloat(string input, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            string cleanedInput = Regex.Replace(input, @"[^\d.\-]", string.Empty);
            return float.TryParse(cleanedInput, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        #endregion

        #region Random

        // Creates a random string from provided characters.
        public static string Shuffle(string characters, int length)
        {
            if (string.IsNullOrEmpty(characters) || length <= 0)
            {
                return string.Empty;
            }

            char[] stringChars = new char[length];
            for (int i = 0; i < length; i++)
            {
                stringChars[i] = characters[UnityEngine.Random.Range(0, characters.Length)];
            }

            return new string(stringChars);
        }

        // Shuffles characters in a string.
        public static string Shuffle(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            char[] characters = input.ToCharArray();
            for (int i = characters.Length - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                char temp = characters[i];
                characters[i] = characters[randomIndex];
                characters[randomIndex] = temp;
            }

            return new string(characters);
        }

        #endregion

        #region Format

        // Shortens text and appends ellipsis when it is longer than length.
        public static string ShortenString(string input, int length)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            if (length <= 0)
            {
                return string.Empty;
            }

            return input.Length > length ? input.Substring(0, length) + "..." : input;
        }

        // Returns fallback when text is null or empty.
        public static string DefaultIfEmpty(string input, string fallback)
        {
            return string.IsNullOrEmpty(input) ? fallback : input;
        }

        // Removes all whitespace from text.
        public static string RemoveWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return Regex.Replace(input, @"\s+", string.Empty);
        }

        // Converts snake_case, kebab-case, or spaced text to Title Case.
        public static string ToTitleCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            string normalized = Regex.Replace(input, @"[_\-]+", " ").Trim();
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized.ToLowerInvariant());
        }

        // Converts text to camelCase.
        public static string ToCamelCase(string input)
        {
            string pascal = ToPascalCase(input);
            if (string.IsNullOrEmpty(pascal))
            {
                return string.Empty;
            }

            return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
        }

        // Converts text to PascalCase.
        public static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            string[] parts = Regex.Split(input, @"[\s_\-]+");
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                {
                    continue;
                }

                string lower = parts[i].ToLowerInvariant();
                builder.Append(char.ToUpperInvariant(lower[0]));
                if (lower.Length > 1)
                {
                    builder.Append(lower.Substring(1));
                }
            }

            return builder.ToString();
        }

        #endregion

        #region Check

        // Checks if text is null, empty, or whitespace.
        public static bool IsNullOrWhiteSpace(string input)
        {
            return string.IsNullOrEmpty(input) || input.Trim().Length == 0;
        }

        // Checks if text contains another text with optional case sensitivity.
        public static bool Contains(string input, string value, bool ignoreCase = true)
        {
            if (input == null || value == null)
            {
                return false;
            }

            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.IndexOf(value, comparison) >= 0;
        }

        #endregion
    }
}
