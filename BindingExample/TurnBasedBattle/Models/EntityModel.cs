using System.Collections.Generic;


namespace OSK.Example.TurnBasedBattle.Models
{
    public class SkillModel
    {
        public string Name;
        public int Damage;
        public int ManaCost;
        public bool IsHeal;
    }

    public class EntityModel 
    {
        public string Name;
        public BindableProperty<int> MaxHP = new BindableProperty<int>(100);
        public BindableProperty<int> CurrentHP = new BindableProperty<int>(100);
        
        public BindableProperty<int> MaxMP = new BindableProperty<int>(50);
        public BindableProperty<int> CurrentMP = new BindableProperty<int>(50);
        
        public BindableProperty<bool> IsDead = new BindableProperty<bool>(false);
        public BindableProperty<bool> IsDefending = new BindableProperty<bool>(false);

        public List<SkillModel> Skills = new List<SkillModel>();
        
        public EntityModel() {
            CurrentHP.Bind(hp => {
                if (hp <= 0 && !IsDead.Value) {
                    CurrentHP.Value = 0;
                    IsDead.Value = true;
                }
            }, false);
        }
    }
}
