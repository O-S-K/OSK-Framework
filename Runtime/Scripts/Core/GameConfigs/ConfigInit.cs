using System;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace OSK
{
    [System.Serializable]
    public class ConfigInit
    {
        // ======================================================================
        // 🎮 GAME SETTINGS
        // ======================================================================
        [TitleGroup("🎮 Game Settings", boldTitle: true)]
        [FoldoutGroup("🎮 Game Settings/FPS & Speed", expanded: true)]
        [LabelWidth(150)]
        [Range(30, 144)]
        public int TargetFrameRate = 60;

        [FoldoutGroup("🎮 Game Settings/FPS & Speed")]
        [LabelWidth(150)]
        [Range(0, 10)]
        public int GameSpeed = 1;


        // ---------------- SAVE PATH SETTINGS ----------------
        [FoldoutGroup("🎮 Game Settings/Save Path")]
        [VerticalGroup("🎮 Game Settings/Save Path/PathGroup")]
        [HorizontalGroup("🎮 Game Settings/Save Path/PathGroup/Top", Width = 0.7f)]
        [LabelWidth(150)]
        public IOUtility.StorageDirectory directoryPathSave = IOUtility.StorageDirectory.PersistentData;

        [HorizontalGroup("🎮 Game Settings/Save Path/PathGroup/Top", Width = 0.3f)]
        [Button("📂 Open Folder", ButtonHeight = 22)]
        private void OpenSaveDirectory()
        {
            string path = IOUtility.GetDirectoryPath(directoryPathSave, CustomPathSave);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder(path);
#else
            Application.OpenURL(path.Replace("\\", "/"));
#endif
            MyLogger.Log($"[Open Folder] {path}");
        }

        [FoldoutGroup("🎮 Game Settings/Save Path")]
        [LabelWidth(150)]
        [FolderPath]
        [ShowIf(nameof(directoryPathSave), IOUtility.StorageDirectory.Custom)]
        public string CustomPathSave = "";

        [FoldoutGroup("🎮 Game Settings/Save Path")]
        [ShowInInspector, ReadOnly, GUIColor(0.8f, 1f, 0.8f)]
        [LabelText("Current Save Path")]
        private string CurrentSavePath => IOUtility.GetDirectoryPath(directoryPathSave, CustomPathSave);


        // ---------------- RUNTIME SETTINGS ----------------
        [FoldoutGroup("🎮 Game Settings/Runtime Options")]
        [LabelWidth(150)]
        public bool RunInBackground = true;

        [FoldoutGroup("🎮 Game Settings/Runtime Options")]
        [LabelWidth(150)]
        public int VSyncCount;

        [FoldoutGroup("🎮 Game Settings/Runtime Options")]
        [LabelWidth(150)]
        public bool NeverSleep = true;


        // ---------------- SECURITY & LOGGING ----------------
        [FoldoutGroup("🎮 Game Settings/Security & Logging")]
        [LabelWidth(150), LabelText("Encrypt Storage")]
        public bool IsEncryptStorage = false;

        [FoldoutGroup("🎮 Game Settings/Security & Logging")]
        [LabelWidth(150), LabelText("Enable Logs")]
        public bool IsEnableLogg = true;



        // ======================================================================
        // ⚙️ ENGINE SETTINGS
        // ======================================================================
        [TitleGroup("⚙️ Engine Settings", boldTitle: true)]
        [FoldoutGroup("⚙️ Engine Settings/Define Symbols", expanded: true)]
        [LabelWidth(150)]
        [MultiLineProperty(3)]
        [Tooltip("Enter scripting define symbols separated by comma or semicolon")]
        public string DefineOtherSettings = "";

        [FoldoutGroup("⚙️ Engine Settings/Define Symbols")]
        [Button(ButtonSizes.Medium, Name = "Apply Defines Settings")]
        private void ApplyDefineSettings()
        {
            Application.targetFrameRate = TargetFrameRate;

            if (string.IsNullOrEmpty(DefineOtherSettings))
                return;

#if UNITY_EDITOR
            UnityEditor.BuildTargetGroup[] targetGroups =
            {
                UnityEditor.BuildTargetGroup.Standalone,
                UnityEditor.BuildTargetGroup.Android,
                UnityEditor.BuildTargetGroup.iOS
            };

            string[] defines = DefineOtherSettings
                .Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .ToArray();

            foreach (var group in targetGroups)
            {
                string current = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                var list = current.Split(';').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)).ToList();

                bool changed = false;
                foreach (var define in defines)
                {
                    if (!list.Contains(define))
                    {
                        list.Add(define);
                        changed = true;
                    }
                }

                if (changed)
                {
                    string joined = string.Join(";", list);
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(group, joined);
                    Debug.Log($"[Define Added] {group} → {string.Join(", ", defines)}");
                }
            }
#endif
        }



        // ======================================================================
        // 📦 GAME CONFIG REFERENCES
        // ======================================================================
        [TitleGroup("📦 Game Configs", boldTitle: true)]
        [FoldoutGroup("📦 Game Configs/Data References", expanded: true)]
        [HideLabel, InlineProperty]
        public DataConfigs data;

        //[TitleGroup("📦 Game Configs")]
        //[HideLabel, InlineProperty]
        //public SettingConfigs setting;

        [FoldoutGroup("📦 Game Configs/Path References")]
        [HideLabel, InlineProperty]
        public PathConfigs path;
    }



    // ======================================================================
    // SUPPORT CLASSES
    // ======================================================================

    [System.Serializable]
    public class SettingConfigs
    {
        public bool isMusicOnDefault = true;
        public bool isSoundOnDefault = true;
        public bool isVibrationOnDefault = true;
        public bool isCheckInternetDefault = true;
        public SystemLanguage languageDefault = SystemLanguage.English;
    }

    [System.Serializable]
    public class DataConfigs
    {
        public ListViewSO listViewS0;
        public ListSoundSO listSoundSo;
    }

    [System.Serializable]
    public class PathConfigs
    {
        [FolderPath]
        public string pathLoadFileCsv = "Localization/Localization";
    }
}
