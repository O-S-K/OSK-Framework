using UnityEngine;
using UnityEngine.UI;
using OSK.Example.TurnBasedBattle.Models;
using OSK.Example.TurnBasedBattle.Core;

namespace OSK.Example.TurnBasedBattle.Views
{
    public class GameOverView : View<AppModel>
    {
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

            if (btnBackToMenu)
            {
                btnBackToMenu.onClick.RemoveAllListeners();
                btnBackToMenu.onClick.AddListener(() => {
                    Hide();
                    _launcher?.BackToMenu();
                });
            }
        }
    }
}
