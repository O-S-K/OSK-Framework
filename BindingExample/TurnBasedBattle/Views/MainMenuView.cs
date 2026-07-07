using UnityEngine;
using UnityEngine.UI;
using OSK.Example.TurnBasedBattle.Models;
using OSK.Example.TurnBasedBattle.Core;

namespace OSK.Example.TurnBasedBattle.Views
{
    public class MainMenuView : View<AppModel>
    {
        public Text txtGold;
        public Button btnOpenSettings;
        public Button[] btnLevels;

        private TurnBasedLauncher _launcher;

        protected override void OnInit()
        {
            _launcher = FindObjectOfType<TurnBasedLauncher>();
        }

        protected override void RefreshUI()
        {
            if (Model == null) return;

            Bind(Model.PlayerGold, gold => {
                if (txtGold) txtGold.text = $"Gold: {gold}";
            });

            if (btnOpenSettings)
            {
                btnOpenSettings.onClick.RemoveAllListeners();
                btnOpenSettings.onClick.AddListener(() => {
                    // Mở Settings đè lên (như 1 Popup)
                    Main.UI.Build<SettingsView, AppModel>().SetModel(Model).Open();
                });
            }

            if (btnLevels != null)
            {
                for (int i = 0; i < btnLevels.Length; i++)
                {
                    int levelIndex = i + 1;
                    if (btnLevels[i] != null)
                    {
                        btnLevels[i].onClick.RemoveAllListeners();
                        btnLevels[i].onClick.AddListener(() => {
                            _launcher?.StartLevel(levelIndex);
                        });
                    }
                }
            }
        }
    }
}
