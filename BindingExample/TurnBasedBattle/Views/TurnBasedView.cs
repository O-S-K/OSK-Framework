using UnityEngine;
using UnityEngine.UI;
using OSK.Example.TurnBasedBattle.Models;
using OSK.Example.TurnBasedBattle.Controllers;

namespace OSK.Example.TurnBasedBattle.Views
{
    // Kế thừa View<BattleModel> chuẩn hệ thống OSK
    public class TurnBasedView : View<BattleModel>
    {
        [Header("UI Text")]
        public Text txtLog;
        public Text txtPlayerHP;
        public Text txtPlayerMP;
        public Text txtEnemyHP;
        public Text txtEnemyName;
        public Text txtPlayerName;
        
        [Header("UI Buttons")]
        public Button btnQuit;
        public Button btnDefend;
        
        [Header("Dynamic Skills")]
        public Transform skillsContainer;
        public Button skillButtonPrefab; // Prefab nút kỹ năng

        private TurnBasedController _controller;

        protected override void OnInit()
        {
            // Được gọi 1 lần khi View Awake (theo Base View của OSK)
            // Lấy reference component Controller từ đâu đó (VD: Singleton hoặc Inject)
            _controller = FindObjectOfType<TurnBasedController>();
        }

        protected override void RefreshUI()
        {
            // Hàm này tự động chạy sau khi Main.UI.Build<TurnBasedView, BattleModel>().SetModel(battleModel).Open() được gọi
            if (Model == null) return;

            // 1. Bind Log
            Bind(Model.BattleLog, log => {
                if (txtLog) txtLog.text = log;
            });

            // 2. Bind Lượt AI / Player
            Bind(Model.CurrentTurn, turn => {
                bool isPlayerTurn = (turn == TurnState.PlayerTurn);
                if (btnDefend) btnDefend.interactable = isPlayerTurn;
                if (skillsContainer) skillsContainer.gameObject.SetActive(isPlayerTurn);
            });

            // 3. Bind Player Data
            if (txtPlayerName) txtPlayerName.text = Model.PlayerData.Name;
            Bind(Model.PlayerData.CurrentHP, hp => {
                if (txtPlayerHP) txtPlayerHP.text = $"HP: {hp} / {Model.PlayerData.MaxHP.Value}";
            });
            Bind(Model.PlayerData.CurrentMP, mp => {
                if (txtPlayerMP) txtPlayerMP.text = $"MP: {mp} / {Model.PlayerData.MaxMP.Value}";
            });

            // 4. Bind Enemy Data
            if (txtEnemyName) txtEnemyName.text = Model.EnemyData.Name;
            Bind(Model.EnemyData.CurrentHP, hp => {
                if (txtEnemyHP) txtEnemyHP.text = $"HP: {hp} / {Model.EnemyData.MaxHP.Value}";
            });

            // 5. Cài đặt các kỹ năng (Tạo nút động)
            SetupSkillButtons();

            // 6. Cài đặt nút bấm cố định
            if (btnDefend)
            {
                btnDefend.onClick.RemoveAllListeners();
                btnDefend.onClick.AddListener(() => _controller?.PlayerDefend());
            }

            if (btnQuit)
            {
                btnQuit.onClick.RemoveAllListeners();
                btnQuit.onClick.AddListener(() => _controller?.QuitBattle());
            }
        }

        private void SetupSkillButtons()
        {
            if (skillsContainer == null || skillButtonPrefab == null) return;

            // Xóa nút cũ
            foreach (Transform child in skillsContainer) {
                Destroy(child.gameObject);
            }

            // Tạo nút mới từ List<SkillModel>
            foreach (var skill in Model.PlayerData.Skills)
            {
                var btn = Instantiate(skillButtonPrefab, skillsContainer);
                btn.gameObject.SetActive(true);
                
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null) txt.text = $"{skill.Name}\n({skill.ManaCost} MP)";

                // Sự kiện bấm
                btn.onClick.AddListener(() => _controller?.PlayerUseSkill(skill));

                // BINDING cực hay: Nút tự mờ đi nếu MP hiện tại không đủ!
                Bind(Model.PlayerData.CurrentMP, mp => {
                    btn.interactable = mp >= skill.ManaCost;
                });
            }
        }
    }
}
