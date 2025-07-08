using System;
using UnityEngine;

namespace OSK
{
    public class GameConfigsManager : GameFrameworkComponent
    {
        private ConfigInit init;
         
        public string VersionApp => Application.version;

        
        public override void OnInit()
        {
            if (Main.Instance.configInit == null)
            {
                Logg.LogError("Not found ConfigInit in Main");
                return;
            }
            init = Main.Instance.configInit;
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
                    Logg.Log("No new version");
                }
            }
            else
            {
                Logg.Log("First time version");
            } 

            PlayerPrefs.SetString(key, currentVersion);
            PlayerPrefs.Save();
        }

        public string GetUrlAppstore => "https://apps.apple.com/app/id" + init.appstoreID;
        public string GetUrlGooglePlay => "https://play.google.com/store/apps/details?id=" + init.googlePlayID;

        public void GetLinkURL()
        {
#if UNITY_ANDROID
            Application.OpenURL(GetUrlGooglePlay);
#elif UNITY_IOS
            Application.OpenURL(GetUrlAppstore);
#endif
        }
    }
}