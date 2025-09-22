using UnityEngine;

namespace OSK
{
    [DefaultExecutionOrder(-1001)]
    public abstract class GameFrameworkComponent : MonoBehaviour
    {
        public virtual void Awake()
        {
            Main.Register(this);
        }
        
        public virtual void OnDestroy() 
        {
            Main.UnRegister(this);
        }

        public abstract void OnInit();
    }
}