using UnityEngine;
using UnityEngine.UI;
using OSK.Example.TurnBasedBattle.Models;
using OSK.Example.TurnBasedBattle.Core;

namespace OSK.Example.TurnBasedBattle.Views
{
    // Kế thừa View<AppModel> chuẩn hệ thống OSK
    public class AppView : View<AppModel>
    {
        [Header("Main Menu UI")]
        public GameObject mainMenuPanel;
        public Text txtGold;
        public Button btnOpenSettings;
        
        // Mảng các nút màn chơi động
        public Button[] btnLevels;

        [Header("Settings UI")]
        public GameObject settingsPanel;
        public InputField inputPlayerName;
        public Toggle toggleSound;
        public Slider sliderVolume;
        public Button btnCloseSettings;

        [Header("Game Over UI")]
        public GameObject gameOverPanel;
        public Text txtResult;
        public Button btnBackToMenu;

        private TurnBasedLauncher _launcher;

        protected override void OnInit()
        {
            _launcher = FindObjectOfType<TurnBasedLauncher>();
        }

        protected override void RefreshUI()
        {
            if (Model == null) return;

            // Bind chuyển đổi Panel
            Bind(Model.CurrentState, state => {
                if (mainMenuPanel) mainMenuPanel.SetActive(state == AppState.MainMenu);
                if (gameOverPanel) gameOverPanel.SetActive(state == AppState.GameOver);
                if (settingsPanel) settingsPanel.SetActive(state == AppState.Settings);

                if (state == AppState.GameOver) {
                    UpdateGameOverUI();
                }
            });

            // Bind Text Vàng
            Bind(Model.PlayerGold, gold => {
                if (txtGold) txtGold.text = $"Gold: {gold}";
            });

            // Bind 2 chiều cho Settings
            if (inputPlayerName) BindTwoWay(inputPlayerName, Model.PlayerName);
            if (toggleSound) BindTwoWay(toggleSound, Model.IsSoundOn);
            if (sliderVolume) BindTwoWay(sliderVolume, Model.MasterVolume);

            // Gắn sự kiện nút
            if (btnOpenSettings)
            {
                btnOpenSettings.onClick.RemoveAllListeners();
                btnOpenSettings.onClick.AddListener(() => _launcher?.OpenSettings());
            }

            if (btnCloseSettings)
            {
                btnCloseSettings.onClick.RemoveAllListeners();
                btnCloseSettings.onClick.AddListener(() => _launcher?.BackToMenu());
            }

            if (btnLevels != null)
            {
                for (int i = 0; i < btnLevels.Length; i++)
                {
                    int levelIndex = i + 1; // Level 1, 2, 3...
                    if (btnLevels[i] != null)
                    {
                        btnLevels[i].onClick.RemoveAllListeners();
                        btnLevels[i].onClick.AddListener(() => _launcher?.StartLevel(levelIndex));
                    }
                }
            }

            if (btnBackToMenu)
            {
                btnBackToMenu.onClick.RemoveAllListeners();
                btnBackToMenu.onClick.AddListener(() => _launcher?.BackToMenu());
            }
        }

        private void UpdateGameOverUI()
        {
            if (txtResult != null)
            {
                if (Model.LastBattleWon) {
                    txtResult.text = $"VICTORY!\n+{100 * Model.SelectedLevel} Gold";
                    txtResult.color = Color.green;
                } else {
                    txtResult.text = "DEFEAT!";
                    txtResult.color = Color.red;
                }
            }
        }
    }
}
