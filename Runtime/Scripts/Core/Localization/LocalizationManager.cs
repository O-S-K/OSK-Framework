using System;
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    public class LocalizationManager : GameFrameworkComponent
    {
        [SerializeReference]
        private Dictionary<string, string> k_LocalizedText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // asset path maps (key -> resources path)
        [SerializeReference, ReadOnly]
        private Dictionary<string, string> spritePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        [SerializeReference, ReadOnly]
        private Dictionary<string, string> audioPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // caches for loaded assets
        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        [ReadOnly, SerializeField]
        private List<SystemLanguage> _listLanguagesCvs = new List<SystemLanguage>();

        private SystemLanguage _currentLanguage = SystemLanguage.English;

        public bool IsSetDefaultLanguage => _isSetDefaultLanguage;
        private bool _isSetDefaultLanguage = false;

        public override void OnInit()
        {
            /* keep as-is */
        }

        #region Public API

        // Set to Awake Init
        public void SetLanguage(SystemLanguage languageCode)
        {
            _isSetDefaultLanguage = true;
            LoadLocalizationData(languageCode);
            _currentLanguage = languageCode;
            MyLogger.Log($"Set language to: {languageCode}");
        }

        public void SwitchLanguage(SystemLanguage language)
        {
            SetLanguage(language);
            // notify listeners
            Main.Observer.Notify(KEY_OBSERVER.KEY_UPDATE_LANGUAGE);
        }

        public SystemLanguage GetCurrentLanguage => _currentLanguage;
        public SystemLanguage[] GetAllLanguages => _listLanguagesCvs.ToArray();

        /// <summary>
        /// Get localized text by key. Returns empty string and logs if missing.
        /// </summary>
        public string GetKey(string key)
        {
            if (!_isSetDefaultLanguage)
            {
                MyLogger.LogError("Please set default language first. Key: " + key);
                return "";
            }

            if (k_LocalizedText.TryGetValue(key, out var value))
            {
                return value ?? "";
            }

            MyLogger.LogError($"Key '{key}' not found in localization data.");
            return "";
        }

        /// <summary>
        /// Get Sprite for a given key (loaded from Resources path configured in CSV).
        /// Returns null if not found.
        /// </summary>
        public Sprite GetSprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            if (spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            if (spritePaths.TryGetValue(key, out var path) && !string.IsNullOrEmpty(path))
            {
                var loaded = Resources.Load<Sprite>(path);
                if (loaded != null)
                {
                    spriteCache[key] = loaded;
                    return loaded;
                }
                else
                {
                    MyLogger.LogWarning($"Sprite at Resources path '{path}' for key '{key}' not found.");
                }
            }

            return null;
        }

        /// <summary>
        /// Get AudioClip for a given key (loaded from Resources path configured in CSV).
        /// Returns null if not found.
        /// </summary>
        public AudioClip GetAudioClip(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            if (audioCache.TryGetValue(key, out var cached) && cached != null) return cached;

            if (audioPaths.TryGetValue(key, out var path) && !string.IsNullOrEmpty(path))
            {
                var loaded = Resources.Load<AudioClip>(path);
                if (loaded != null)
                {
                    audioCache[key] = loaded;
                    return loaded;
                }
                else
                {
                    MyLogger.LogWarning($"AudioClip at Resources path '{path}' for key '{key}' not found.");
                }
            }

            return null;
        }

        /// <summary>
        /// Optional: preload all sprite/audio paths to cache (useful to avoid hiccups at runtime).
        /// Call after SetLanguage if you want to load them immediately.
        /// </summary>
        public void PreloadAssets()
        {
            foreach (var kv in spritePaths)
            {
                if (!spriteCache.ContainsKey(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                {
                    var s = Resources.Load<Sprite>(kv.Value);
                    if (s != null) spriteCache[kv.Key] = s;
                }
            }

            foreach (var kv in audioPaths)
            {
                if (!audioCache.ContainsKey(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                {
                    var a = Resources.Load<AudioClip>(kv.Value);
                    if (a != null) audioCache[kv.Key] = a;
                }
            }
        }

        #endregion

        #region CSV Loading

        private void LoadLocalizationData(SystemLanguage languageCode)
        {
            if (Main.Instance.configInit.data == null || Main.Instance.configInit.data.localizationCSV == null)
            {
                MyLogger.LogError("Localization CSV file is not assigned in ConfigInit.");
                return;
            }


            TextAsset textFile = Main.Instance.configInit.data.localizationCSV;
            string[] lines = textFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Clear previous data
            k_LocalizedText.Clear();
            spritePaths.Clear();
            audioPaths.Clear();
            spriteCache.Clear();
            audioCache.Clear();
            _listLanguagesCvs.Clear();

            if (lines.Length == 0)
            {
                MyLogger.LogWarning("Localization file is empty: " + textFile.name);
                return;
            }

            // Header parsing
            string[] headers = ParseCsvLine(lines[0]);
            // Find text column index (header name equals SystemLanguage.ToString())
            int languageColumnIndex = Array.FindIndex(headers, h => string.Equals(h, languageCode.ToString(), StringComparison.OrdinalIgnoreCase));

            // Find asset columns:
            string langCode = LanguageCountryCodeMapper.GetCountryCode(languageCode); // e.g., "en", "vi"
            int spriteLangIndex = Array.FindIndex(headers, h => string.Equals(h, $"sprite_{langCode}", StringComparison.OrdinalIgnoreCase));
            int audioLangIndex = Array.FindIndex(headers, h => string.Equals(h, $"audio_{langCode}", StringComparison.OrdinalIgnoreCase));

            // Fallback: generic "sprite" / "audio" columns
            int spriteGenericIndex = Array.FindIndex(headers, h => string.Equals(h, "sprite", StringComparison.OrdinalIgnoreCase));
            int audioGenericIndex = Array.FindIndex(headers, h => string.Equals(h, "audio", StringComparison.OrdinalIgnoreCase));

            // If language text column not found, log and return
            if (languageColumnIndex == -1)
            {
                MyLogger.LogError($"Language '{languageCode}' not found in localization file headers.");
                return;
            }

            // iterate lines (skip header)
            for (int i = 1; i < lines.Length; i++)
            {
                string[] columns = ParseCsvLine(lines[i]);
                if (columns.Length == 0) continue;

                // First column expected to be key
                if (columns.Length == 0 || string.IsNullOrWhiteSpace(columns[0]))
                {
                    MyLogger.LogWarning($"Invalid or missing key at CSV line {i + 1}.");
                    continue;
                }

                string key = columns[0].Trim();

                // --- Text ---
                string textVal = "";
                if (languageColumnIndex >= 0 && languageColumnIndex < columns.Length)
                {
                    textVal = columns[languageColumnIndex].Trim();
                }

                k_LocalizedText[key] = textVal;

                // --- Sprite path: prefer language-specific, otherwise generic ---
                string spritePath = null;
                if (spriteLangIndex >= 0 && spriteLangIndex < columns.Length)
                    spritePath = columns[spriteLangIndex].Trim();
                else if (spriteGenericIndex >= 0 && spriteGenericIndex < columns.Length)
                    spritePath = columns[spriteGenericIndex].Trim();

                if (!string.IsNullOrEmpty(spritePath))
                {
                    spritePaths[key] = spritePath;
                }

                // --- Audio path: prefer language-specific, otherwise generic ---
                string audioPath = null;
                if (audioLangIndex >= 0 && audioLangIndex < columns.Length)
                    audioPath = columns[audioLangIndex].Trim();
                else if (audioGenericIndex >= 0 && audioGenericIndex < columns.Length)
                    audioPath = columns[audioGenericIndex].Trim();

                if (!string.IsNullOrEmpty(audioPath))
                {
                    audioPaths[key] = audioPath;
                }
            }

            // populate detected languages list from header (once)
            GetListLanguageCsv(headers);

            MyLogger.Log($"Load localization data for language: {languageCode}. Keys: {k_LocalizedText.Count}, Sprites: {spritePaths.Count}, Audios: {audioPaths.Count}");
        }

        private void GetListLanguageCsv(string[] headers)
        {
            for (int j = 0; j < headers.Length; j++)
            {
                var h = headers[j].Trim();
                if (Enum.TryParse<SystemLanguage>(h, true, out var language))
                {
                    if (!_listLanguagesCvs.Contains(language))
                        _listLanguagesCvs.Add(language);
                }
            }
        }

        /// <summary>
        /// CSV parser that respects quoted columns with commas.
        /// (Your original implementation; kept as-is)
        /// </summary>
        private string[] ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder currentColumn = new StringBuilder();

            for (int idx = 0; idx < line.Length; idx++)
            {
                char c = line[idx];
                if (c == '"')
                {
                    // handle double-quote escaping "" inside quoted string
                    if (inQuotes && idx + 1 < line.Length && line[idx + 1] == '"')
                    {
                        currentColumn.Append('"');
                        idx++; // skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentColumn.ToString());
                    currentColumn.Clear();
                }
                else
                {
                    currentColumn.Append(c);
                }
            }

            result.Add(currentColumn.ToString());
            return result.ToArray();
        }

        #endregion
    }
}