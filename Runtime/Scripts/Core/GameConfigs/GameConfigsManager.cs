using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    public class GameConfigsManager : GameFrameworkComponent
    {
        [ReadOnly]
        public ConfigInit Init;
         
        [ReadOnly]
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

            if (PrefData.HasKey(key))
            {
                string savedVersion = PrefData.GetString(key);
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

            PrefData.SetString(key, currentVersion);
            PrefData.Save();
        }
    }
}