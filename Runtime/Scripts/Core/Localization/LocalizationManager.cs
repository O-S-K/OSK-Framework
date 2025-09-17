using System;
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/*
 *
 *  // gen for AI: https://x.com/LoloelRolo/status/1221259358585217030

Key	                 	French	                            Vietnamese
welcome_message	        Welcome to the game!	            Chào mưn bạn đến với trò chơi!
exit_prompt	            Are you sure you want to exit?	    Bạn có chắc chắn muốn thoát không?

 Main.Localization.SetLanguage("English"); // Or "Vietnamese", etc.
 .text = World.Localization.GetLocalizedString("welcome_message");
*/

namespace OSK
{
    public class LocalizationManager : GameFrameworkComponent
    {
        [SerializeReference] private Dictionary<string, string> k_LocalizedText = new Dictionary<string, string>();
        [ReadOnly, SerializeField] private List<SystemLanguage> _listLanguagesCvs = new List<SystemLanguage>();
        private SystemLanguage _currentLanguage = SystemLanguage.English;
 
        public bool IsSetDefaultLanguage => _isSetDefaultLanguage;
        private bool _isSetDefaultLanguage = false;

        public override void OnInit() {}


        public void SetLanguage(SystemLanguage languageCode)
        {
            _isSetDefaultLanguage = true;
            LoadLocalizationData(languageCode);
            _currentLanguage = languageCode;
            OSKLogger.Log("Localization",$"Set language to: {languageCode}");
        }


        public void SwitchLanguage(SystemLanguage language)
        {
            SetLanguage(language);
            Main.Observer.Notify(KEY_OBSERVER.KEY_UPDATE_LANGUAGE);
        }
  
        public SystemLanguage GetCurrentLanguage => _currentLanguage;
        public SystemLanguage[] GetAllLanguages => _listLanguagesCvs.ToArray();


        public string GetKey(string key)
        {
            if (_isSetDefaultLanguage == false)
            {
                OSKLogger.LogError("Localization","Please set default language first." + key);
                return "";
            }

            if (k_LocalizedText.TryGetValue(key, out var value))
            {
                return value;
            }

            OSKLogger.LogError("Localization",$"Key '{key}' not found in localization data.");
            return "";
        }


        #region Private

        private void LoadLocalizationData(SystemLanguage languageCode)
        {
            var path = Main.Instance.configInit.path.pathLoadFileCsv;
            if (path.StartsWith("Resources/"))
            {
                path = path["Resources/".Length..];
            }
            
            TextAsset textFile = Resources.Load<TextAsset>(path);
            if (textFile == null)
            {
                OSKLogger.LogError("Localization","Not found localization file: " + path);
                return;
            }

            // Parse CSV lines while removing empty lines
            string[] lines = textFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Clear the previous localization data
            k_LocalizedText.Clear();

            // Find the index of the language code in the CSV header
            string[] headers = lines[0].Split(',');
            int languageColumnIndex = Array.IndexOf(headers, languageCode.ToString());

            if (languageColumnIndex == -1)
            {
                OSKLogger.LogError("Localization",$"Language '{languageCode}' not found in localization file.");
                return;
            }

            // Start from the second line to skip the header
            for (int i = 0; i < lines.Length; i++)
            {
                // Handle CSV values with commas or line breaks inside quotes
                string[] columns = ParseCsvLine(lines[i]);

                if (i == 0)
                {
                    GetListLanguageCsv(columns);
                }
                else
                {
                    GetValueFormLanguage(columns, languageColumnIndex, i);
                }
            }

            OSKLogger.Log("Localization",$"Load localization data for language: {languageCode}");
        }

        private void GetValueFormLanguage(string[] columns, int languageColumnIndex, int i)
        {
            // Check if the column exists and ensure the first column (key) is present
            if (columns.Length > languageColumnIndex && !string.IsNullOrWhiteSpace(columns[0]))
            {
                string key = columns[0].Trim();
                string value = columns[languageColumnIndex].Trim();

                // Store the key-value pair in the dictionary
                k_LocalizedText[key] = value;
            }
            else
            {
                OSKLogger.LogWarning("Localization",$"Invalid or missing data at line {i + 1} in localization file.");
            }
        }

        private void GetListLanguageCsv(string[] columns)
        {
            for (int j = 0; j < columns.Length; j++)
            {
                if (Enum.TryParse(columns[j], out SystemLanguage language))
                {
                    if (!_listLanguagesCvs.Contains(language))
                        _listLanguagesCvs.Add(language);
                }
            }
        }

        private string[] ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder currentColumn = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes; // Toggle quotes state
                }
                else if (c == ',' && !inQuotes)
                {
                    // End of a column
                    result.Add(currentColumn.ToString());
                    currentColumn.Clear();
                }
                else
                {
                    currentColumn.Append(c); // Append character to current column
                }
            }

            // Add the last column
            result.Add(currentColumn.ToString());
            return result.ToArray();
        }

        #endregion
    }
}