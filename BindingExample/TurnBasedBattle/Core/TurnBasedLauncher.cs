using UnityEngine;
using OSK.Example.TurnBasedBattle.Models;
using OSK.Example.TurnBasedBattle.Controllers;
using OSK.Example.TurnBasedBattle.Views;

namespace OSK.Example.TurnBasedBattle.Core
{
    public class TurnBasedLauncher : MonoBehaviour
    {
        [SerializeField] private TurnBasedController _battleController;
        [SerializeField] private AppModel _appModel;

        void Start()
        {
            _appModel = new AppModel();
            Main.UI.Build<MainMenuView, AppModel>().SetModel(_appModel).Open();
        }

        public void StartLevel(int level)
        {
            _appModel.SelectedLevel = level;
            
            var battleModel = new BattleModel();
            
            // Player Setup (Đa dạng skill tùy cấp độ màn chơi)
            battleModel.PlayerData = new EntityModel { Name = _appModel.PlayerName.Value };
            battleModel.PlayerData.MaxHP.Value = 100 + (level * 50); // Càng chơi cao máu càng nhiều
            battleModel.PlayerData.CurrentHP.Value = battleModel.PlayerData.MaxHP.Value;
            battleModel.PlayerData.MaxMP.Value = 50 + (level * 20);
            battleModel.PlayerData.CurrentMP.Value = battleModel.PlayerData.MaxMP.Value;

            // Kỹ năng cơ bản
            battleModel.PlayerData.Skills.Add(new SkillModel { Name = "Chém thường", Damage = 20, ManaCost = 0 });
            
            // Càng lên level cao càng mở khóa nhiều kỹ năng
            if (level >= 1) {
                battleModel.PlayerData.Skills.Add(new SkillModel { Name = "Siêu cấp chém", Damage = 45, ManaCost = 20 });
                battleModel.PlayerData.Skills.Add(new SkillModel { Name = "Hồi máu", Damage = 50, ManaCost = 15, IsHeal = true });
            }
            if (level >= 3) {
                battleModel.PlayerData.Skills.Add(new SkillModel { Name = "Hút máu", Damage = 35, ManaCost = 25 });
            }
            if (level >= 4) {
                battleModel.PlayerData.Skills.Add(new SkillModel { Name = "Thánh kiếm", Damage = 100, ManaCost = 50 });
            }
            if (level >= 5) {
                battleModel.PlayerData.Skills.Add(new SkillModel { Name = "Hủy diệt", Damage = 250, ManaCost = 100 });
            }
            
            // Enemy Setup đa dạng theo Level
            battleModel.EnemyData = new EntityModel();
            switch (level)
            {
                case 1:
                    battleModel.EnemyData.Name = "Slime Xanh";
                    battleModel.EnemyData.MaxHP.Value = 150;
                    break;
                case 2:
                    battleModel.EnemyData.Name = "Sói Xám";
                    battleModel.EnemyData.MaxHP.Value = 300;
                    break;
                case 3:
                    battleModel.EnemyData.Name = "Quỷ Lùn Goblin";
                    battleModel.EnemyData.MaxHP.Value = 550;
                    break;
                case 4:
                    battleModel.EnemyData.Name = "Chúa Tể Orc";
                    battleModel.EnemyData.MaxHP.Value = 900;
                    break;
                case 5:
                default:
                    battleModel.EnemyData.Name = "Rồng Địa Ngục (BOSS)";
                    battleModel.EnemyData.MaxHP.Value = 1500;
                    break;
            }
            battleModel.EnemyData.CurrentHP.Value = battleModel.EnemyData.MaxHP.Value; 
            _battleController.Setup(battleModel, this);
            Main.UI.Get<MainMenuView>()?.Hide(); // Ẩn Menu
            Main.UI.Build<TurnBasedView, BattleModel>().SetModel(battleModel).Open();
        }

        public void EndBattle(bool isWin)
        {
            _appModel.LastBattleWon = isWin;
            if (isWin) {
                _appModel.PlayerGold.Value += 100 * _appModel.SelectedLevel;
            }

            Main.UI.Get<TurnBasedView>()?.Hide();
            Main.UI.Build<GameOverView, AppModel>().SetModel(_appModel).Open();
        }

        public void OpenSettings()
        {
            Main.UI.Build<SettingsView, AppModel>().SetModel(_appModel).Open();
        }

        public void BackToMenu()
        {
            Main.UI.Get<GameOverView>()?.Hide();
            Main.UI.Get<SettingsView>()?.Hide();
            Main.UI.Build<MainMenuView, AppModel>().SetModel(_appModel).Open();
        }
    }
}
