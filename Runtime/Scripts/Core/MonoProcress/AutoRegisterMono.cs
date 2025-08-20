using UnityEngine;

namespace OSK
{
    public abstract class AutoRegisterMono : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            Main.Mono.Register(this);
        }

        protected virtual void OnDisable()
        {
            Main.Mono.UnRegister(this);
        }
    }

    /* example of usage
      public class Player : AutoRegisterMono, IUpdate, IFixedUpdate, ILateUpdate
      {
          public void Tick()
          {
              Logg.Log("Player Update");
          }

          public void FixedTick()
          {
              Logg.Log("Player Fixed Update");
          }

          public void LateTick()
          {
              Logg.Log("Player Late Update");
          }
      } 
     */
}