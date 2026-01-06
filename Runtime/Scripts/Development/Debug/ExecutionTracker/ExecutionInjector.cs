#if UNITY_EDITOR
using UnityEngine;

namespace OSK.ExecutionDebug
{
    public static class ExecutionInjector
    {
        public static bool IsInjected { get; private set; } = false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Inject()
        {
            if (!IsInjected) return;
            
            foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
            {
                if (mb.GetComponent<ExecutionHook>() == null)
                    mb.gameObject.AddComponent<ExecutionHook>();
            }
        }
    }
}
#endif