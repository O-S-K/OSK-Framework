using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;

namespace OSK
{
    [System.Serializable]
    public class ConfigInit  
    {
        [TextArea]
        [SerializeField, ReadOnly]
        private string tooltip = "This is the configuration for the game. " +
                                 "It contains all the settings and data that are used in the game. " +
                                 "You can modify this file to change the game settings.";
        
        [Header("Game Settings")]
        public int targetFrameRate = 60;
        public bool logTest = true;

        public string packageName = "";
        public string appstoreID = "";
         
        [Space]
        [Header("Game Configs")]
        public DataConfigs data;
        //public SettingConfigs setting;
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