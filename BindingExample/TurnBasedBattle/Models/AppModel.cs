using System;


namespace OSK.Example.TurnBasedBattle.Models
{
    public enum AppState { MainMenu, InBattle, GameOver, Settings }

    [Serializable]
    public class AppModel
    {
        public BindableProperty<AppState> CurrentState = new BindableProperty<AppState>(AppState.MainMenu);
        public BindableProperty<int> PlayerGold = new BindableProperty<int>(0);
         
        public BindableProperty<bool> IsSoundOn = new BindableProperty<bool>(true);
        public BindableProperty<float> MasterVolume = new BindableProperty<float>(1.0f);
        public BindableProperty<string> PlayerName = new BindableProperty<string>("Hiệp sĩ Gà");

        public int SelectedLevel = 1;
        public bool LastBattleWon = false;
    }
}
