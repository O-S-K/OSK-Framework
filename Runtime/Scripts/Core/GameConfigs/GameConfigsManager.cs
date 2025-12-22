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
                MyLogger.LogError("Not found ConfigInit in Main");
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
                    MyLogger.Log("No new version");
                }
            }
            else
            {
                MyLogger.Log("First time version");
            } 

            PrefData.SetString(key, currentVersion);
            PrefData.Save();
        }
    }
}