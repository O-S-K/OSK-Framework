using UnityEngine;
using UnityEngine.UI;
using OSK.Example.TurnBasedBattle.Models;

namespace OSK.Example.TurnBasedBattle.Views
{
    public class SettingsView : View<AppModel>
    {
        public InputField inputPlayerName;
        public Toggle toggleSound;
        public Slider sliderVolume;
        public Button btnClose;

        protected override void RefreshUI()
        {
            if (Model == null) return;

            // Two-Way Binding
            if (inputPlayerName) BindTwoWay(inputPlayerName, Model.PlayerName);
            if (toggleSound) BindTwoWay(toggleSound, Model.IsSoundOn);
            if (sliderVolume) BindTwoWay(sliderVolume, Model.MasterVolume);

            if (btnClose)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(Hide);
            }
        }

        protected override void OnInit()
        {
             
        }
    }
}
