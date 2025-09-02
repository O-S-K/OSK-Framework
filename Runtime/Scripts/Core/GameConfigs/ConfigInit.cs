using System;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace OSK
{
    [System.Serializable]
    public class ConfigInit
    {
        [BoxGroup("🎮 Game Settings"), LabelWidth(150)]
        public int TargetFrameRate = 60;

        [BoxGroup("🎮 Game Settings"), LabelText("Encrypt Storage")]
        public bool IsEncryptStorage = false;
        
        [BoxGroup("🎮 Game Settings"), LabelText("Enable Logs")]
        public bool IsEnableLogg = true;

        [BoxGroup("🎮 Game Settings")]
        public string DefineOtherSettings = "";
        
        [BoxGroup("🎮 Game Settings")]
        [Button(ButtonSizes.Medium, Name = "Apply Defines Settings")]
        private void ButtonDefineOtherSettings()
        {
            Application.targetFrameRate = TargetFrameRate;

            if (string.IsNullOrEmpty(DefineOtherSettings))
                return;

#if UNITY_EDITOR
            // Danh sách build target cần set define
            UnityEditor.BuildTargetGroup[] buildTargetGroups =
            {
                UnityEditor.BuildTargetGroup.Standalone,
                UnityEditor.BuildTargetGroup.Android,
                UnityEditor.BuildTargetGroup.iOS
            };

            // Tách các define do người dùng nhập
            string[] defines = DefineOtherSettings
                .Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .ToArray();

            foreach (var buildTargetGroup in buildTargetGroups)
            {
                // Lấy defines hiện tại
                string currentDefines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

                // Convert sang list để dễ quản lý
                var defineList = currentDefines.Split(';').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)).ToList();

                bool isChanged = false;

                foreach (var define in defines)
                {
                    // Chỉ thêm define nếu chưa tồn tại
                    if (!defineList.Contains(define))
                    {
                        defineList.Add(define);
                        isChanged = true;
                    }
                }

                // Nếu có thay đổi thì set lại define symbols
                if (isChanged)
                {
                    string newDefines = string.Join(";", defineList);
                    UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newDefines);
                    Debug.Log($"[Define Added] {buildTargetGroup} → {string.Join(", ", defines)}");
                }
            }
#endif
        }
        [BoxGroup("📦 Game Configs")]
        [HideLabel, InlineProperty]
        public DataConfigs data;

        //[BoxGroup("⚙ Settings")]
        //public SettingConfigs setting;

        [BoxGroup("📂 Paths")]
        [HideLabel, InlineProperty]
        public PathConfigs path;
    }


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
        [FolderPath] public string pathLoadFileCsv = "Localization/Localization";
    }
}