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

        [BoxGroup("📱 Store IDs")]
        [LabelText("App Store ID")]
        public string appstoreID = "";

        [BoxGroup("📱 Store IDs")]
        [LabelText("Google Play ID")]
        public string googlePlayID = "";

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
        public UIParticleSO uiParticleSO;
    }

    [System.Serializable]
    public class PathConfigs
    {
        [FolderPath] public string pathLoadFileCsv = "Localization/Localization";
    }
}