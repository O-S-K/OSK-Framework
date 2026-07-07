

namespace OSK.Example.TurnBasedBattle.Models
{
    public class BattleModel 
    {
        public BindableProperty<TurnState> CurrentTurn = new BindableProperty<TurnState>();
        public BindableProperty<string> BattleLog = new BindableProperty<string>("Trận đấu bắt đầu!");
        
        public EntityModel PlayerData;
        public EntityModel EnemyData;
    }
}
