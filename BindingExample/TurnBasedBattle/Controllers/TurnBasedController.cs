using System.Collections;
using UnityEngine;
using OSK.Example.TurnBasedBattle.Models;
using OSK.Example.TurnBasedBattle.Core;

namespace OSK.Example.TurnBasedBattle.Controllers
{
    public class TurnBasedController : MonoBehaviour
    {
        private BattleModel _battle;
        private TurnBasedLauncher _launcher;

        public void Setup(BattleModel model, TurnBasedLauncher launcher)
        {
            _battle = model;
            _launcher = launcher;
            StartTurn(TurnState.PlayerTurn);
        }

        private void StartTurn(TurnState turn)
        {
            _battle.CurrentTurn.Value = turn;
            
            // Hồi 1 ít MP mỗi lượt
            if (turn == TurnState.PlayerTurn) {
                _battle.PlayerData.IsDefending.Value = false; // Xóa trạng thái thủ
                _battle.PlayerData.CurrentMP.Value = Mathf.Min(_battle.PlayerData.MaxMP.Value, _battle.PlayerData.CurrentMP.Value + 10);
            } else if (turn == TurnState.EnemyTurn) {
                _battle.EnemyData.IsDefending.Value = false;
                StartCoroutine(EnemyAILogic());
            }
        }

        public void PlayerUseSkill(SkillModel skill)
        {
            MyLogger.Log($"Player chọn dùng skill: {skill.Name} (MP Cost: {skill.ManaCost}, Damage: {skill.Damage})");
            if (_battle.CurrentTurn.Value != TurnState.PlayerTurn) return;

            if (_battle.PlayerData.CurrentMP.Value < skill.ManaCost) {
                _battle.BattleLog.Value = "Không đủ MP để dùng " + skill.Name + "!";
                return;
            }

            // Trừ MP
            _battle.PlayerData.CurrentMP.Value -= skill.ManaCost;

            if (skill.IsHeal) {
                _battle.BattleLog.Value = $"{_battle.PlayerData.Name} dùng {skill.Name}, hồi {skill.Damage} HP!";
                _battle.PlayerData.CurrentHP.Value = Mathf.Min(_battle.PlayerData.MaxHP.Value, _battle.PlayerData.CurrentHP.Value + skill.Damage);
            } else {
                int finalDamage = skill.Damage;
                if (_battle.EnemyData.IsDefending.Value) finalDamage /= 2; // Giảm nửa sát thương nếu quái đang thủ
                
                _battle.BattleLog.Value = $"{_battle.PlayerData.Name} dùng {skill.Name}, gây {finalDamage} sát thương!";
                _battle.EnemyData.CurrentHP.Value -= finalDamage;
                
                // Hiệu ứng hút máu nếu chiêu có chữ Hút máu
                if (skill.Name.Contains("Hút máu")) {
                    int heal = finalDamage / 2;
                    _battle.PlayerData.CurrentHP.Value = Mathf.Min(_battle.PlayerData.MaxHP.Value, _battle.PlayerData.CurrentHP.Value + heal);
                    _battle.BattleLog.Value += $"\nĐồng thời hút lại {heal} HP!";
                }
            }

            CheckWinLoseOrNextTurn();
        }

        public void PlayerDefend()
        {
            if (_battle.CurrentTurn.Value != TurnState.PlayerTurn) return;
            
            _battle.PlayerData.IsDefending.Value = true;
            _battle.BattleLog.Value = $"{_battle.PlayerData.Name} phòng thủ! Giảm 50% sát thương nhận vào.";
            
            CheckWinLoseOrNextTurn();
        }

        public void QuitBattle()
        {
            StopAllCoroutines();
            _launcher.EndBattle(false); // Bỏ cuộc = Thua
        }

        private IEnumerator EnemyAILogic()
        {
            _battle.BattleLog.Value = $"{_battle.EnemyData.Name} đang suy nghĩ...";
            yield return new WaitForSeconds(1.0f);

            // Đưa ra quyết định random: Đánh hoặc Thủ
            if (Random.value > 0.3f) {
                int baseDmg = 10 + (_battle.EnemyData.MaxHP.Value / 10); // Quái máu càng nhiều thì đánh càng đau
                int damage = Random.Range(baseDmg, baseDmg + 15);
                if (_battle.PlayerData.IsDefending.Value) damage /= 2;

                _battle.BattleLog.Value = $"{_battle.EnemyData.Name} tấn công, gây {damage} sát thương!";
                _battle.PlayerData.CurrentHP.Value -= damage;
            } else {
                _battle.EnemyData.IsDefending.Value = true;
                _battle.BattleLog.Value = $"{_battle.EnemyData.Name} co cụm phòng thủ!";
            }

            CheckWinLoseOrNextTurn();
        }

        private void CheckWinLoseOrNextTurn()
        {
            if (_battle.EnemyData.IsDead.Value) {
                _battle.BattleLog.Value = "VICTORY! Bạn đã thắng.";
                _battle.CurrentTurn.Value = TurnState.GameOver;
                StartCoroutine(EndBattleDelay(true));
            }
            else if (_battle.PlayerData.IsDead.Value) {
                _battle.BattleLog.Value = "DEFEAT! Bạn đã thua.";
                _battle.CurrentTurn.Value = TurnState.GameOver;
                StartCoroutine(EndBattleDelay(false));
            }
            else {
                StartTurn(_battle.CurrentTurn.Value == TurnState.PlayerTurn ? TurnState.EnemyTurn : TurnState.PlayerTurn);
            }
        }
        
        private IEnumerator EndBattleDelay(bool isWin)
        {
            yield return new WaitForSeconds(2.0f);
            _launcher.EndBattle(isWin);
        }
    }
}
