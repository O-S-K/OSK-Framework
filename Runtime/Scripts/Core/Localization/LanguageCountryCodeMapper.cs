using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public static class LanguageCountryCodeMapper
    {
        private static readonly Dictionary<SystemLanguage, string> _countryCodes =
            new Dictionary<SystemLanguage, string>
            {
                { SystemLanguage.Afrikaans, "AF" },
                { SystemLanguage.Arabic, "AR" },
                { SystemLanguage.Basque, "ES" },
                { SystemLanguage.Belarusian, "BY" },
                { SystemLanguage.Bulgarian, "BG" },
                { SystemLanguage.Catalan, "CA" },
                { SystemLanguage.Chinese, "ZH" },
                { SystemLanguage.Czech, "CS" },
                { SystemLanguage.Danish, "DA" },
                { SystemLanguage.Dutch, "NL" },
                { SystemLanguage.English, "EN" },
                { SystemLanguage.Estonian, "ET" },
                { SystemLanguage.Faroese, "FO" },
                { SystemLanguage.Finnish, "FI" },
                { SystemLanguage.French, "FR" },
                { SystemLanguage.German, "DE" },
                { SystemLanguage.Greek, "EL" },
                { SystemLanguage.Hebrew, "HE" },
                { SystemLanguage.Hungarian, "HU" },
                { SystemLanguage.Icelandic, "IS" },
                { SystemLanguage.Indonesian, "ID" },
                { SystemLanguage.Italian, "IT" },
                { SystemLanguage.Japanese, "JA" },
                { SystemLanguage.Korean, "KO" },
                { SystemLanguage.Latvian, "LV" },
                { SystemLanguage.Lithuanian, "LT" },
                { SystemLanguage.Norwegian, "NO" },
                { SystemLanguage.Polish, "PL" },
                { SystemLanguage.Portuguese, "PT" },
                { SystemLanguage.Romanian, "RO" },
                { SystemLanguage.Russian, "RU" },
                { SystemLanguage.SerboCroatian, "SH" },
                { SystemLanguage.Slovak, "SK" },
                { SystemLanguage.Slovenian, "SL" },
                { SystemLanguage.Spanish, "ES" },
                { SystemLanguage.Swedish, "SV" },
                { SystemLanguage.Thai, "TH" },
                { SystemLanguage.Turkish, "TR" },
                { SystemLanguage.Ukrainian, "UA" },
                { SystemLanguage.Vietnamese, "VI" },
                { SystemLanguage.ChineseSimplified, "ZH-Hans" },
                { SystemLanguage.ChineseTraditional, "ZH-Hant" },
                #if UNITY_2022_1_OR_NEWER
                { SystemLanguage.Hindi, "HI" },
                #endif
                { SystemLanguage.Unknown, "XX" },
            };

        public static string GetCountryCode(SystemLanguage language)
        {
            return _countryCodes.GetValueOrDefault(language, "XX"); // "XX" for unknown
        }
    }
}