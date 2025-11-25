using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    [DisallowMultipleComponent]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private bool updateOnStart = true;
        [SerializeField] private bool useTMP = false;

        private Text uiText;
        private TMP_Text tmpText;
        private SystemLanguage lastLanguage;

        private void Awake()
        {
            uiText = GetComponent<Text>();
            tmpText = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            if (updateOnStart) UpdateLocalized();
            Main.Observer.Add(KEY_OBSERVER.KEY_UPDATE_LANGUAGE, OnLanguageUpdate);
        }

        private void OnDestroy()
        {
            Main.Observer.Remove(KEY_OBSERVER.KEY_UPDATE_LANGUAGE, OnLanguageUpdate);
        }

        private void OnEnable()
        {
            if (Main.Localization == null) return;
            if (!Main.Localization.IsSetDefaultLanguage) return;
            if (lastLanguage == Main.Localization.GetCurrentLanguage) return;
            lastLanguage = Main.Localization.GetCurrentLanguage;
            UpdateLocalized();
        }

        private void OnLanguageUpdate(object data = null) => UpdateLocalized();

        public void UpdateLocalized()
        {
            if (string.IsNullOrEmpty(key)) return;
            if (Main.Localization == null) return;

            string localized = Main.Localization.GetKey(key);
            if (localized == null) return;

            if (useTMP && tmpText != null)
            {
                tmpText.text = localized;
            }
            else if (uiText != null)
            {
                uiText.text = localized;
            }
            else if (tmpText != null) // fallback auto-detect TMP
            {
                tmpText.text = localized;
            }
        }

        // allow runtime set
        public void SetKey(string newKey)
        {
            key = newKey;
            UpdateLocalized();
        }

        public string GetKey() => key;
        
        
#if UNITY_EDITOR 
        [UnityEditor.CustomEditor(typeof(OSK.LocalizedText))]
        public class LocalizedTextEditor : UnityEditor.Editor
        {
            private SystemLanguage previewLang = SystemLanguage.English;
            private string previewValue = "";

            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                var comp = (OSK.LocalizedText)target;

                UnityEditor.EditorGUILayout.Space(8);
                UnityEditor.EditorGUILayout.LabelField("Preview Localization Runtime", UnityEditor.EditorStyles.boldLabel);

                // language dropdown
                previewLang = (SystemLanguage)UnityEditor.EditorGUILayout.EnumPopup("Preview Language", previewLang);

                if (GUILayout.Button("Show Localized Value", GUILayout.Height(25)))
                {
                    previewValue = GetPreviewLocalization(comp, previewLang);
                }

                if (!string.IsNullOrEmpty(previewValue))
                {
                    UnityEditor.EditorGUILayout.HelpBox(previewValue, UnityEditor.MessageType.Info);
                }
            }

            private string GetPreviewLocalization(OSK.LocalizedText comp, SystemLanguage lang)
            {
                if (comp == null) return "null";
                if (string.IsNullOrEmpty(comp.GetKey())) return "Key empty";

                if (Main.Localization == null) return "Main.Localization = NULL";
                if (!Main.Localization.IsSetDefaultLanguage) return "Default language not loaded";

                // TEMP switch language
                var original = Main.Localization.GetCurrentLanguage;
                Main.Localization.SetLanguage(lang);

                string val = Main.Localization.GetKey(comp.GetKey());

                // restore
                Main.Localization.SetLanguage(original);

                return string.IsNullOrEmpty(val)
                    ? $"⚠ Key not found for {lang}"
                    : $"[{lang}] → {val}";
            }
        }
#endif
        
    }
}
