using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    public class GameConfigsManager : GameFrameworkComponent
    {
        [ReadOnly]
        public ConfigInit Init;
         
        public string VersionApp => Application.version;

        
        public override void OnInit()
        {
            if (Main.Instance.configInit == null)
            {
                OSKLogger.LogError("Not found ConfigInit in Main");
                return;
            }
            Init = Main.Instance.configInit;
        }

        public void CheckVersion(Action onNewVersion)
        {
            string currentVersion = VersionApp;
            string key = "lastVersion";

            if (PlayerPrefs.HasKey(key))
            {
                string savedVersion = PlayerPrefs.GetString(key);
                if (currentVersion != savedVersion)
                {
                    // New version
                    onNewVersion?.Invoke();
                }
                else
                {
                    OSKLogger.Log("No new version");
                }
            }
            else
            {
                OSKLogger.Log("First time version");
            } 

            PlayerPrefs.SetString(key, currentVersion);
            PlayerPrefs.Save();
        }
    }
}