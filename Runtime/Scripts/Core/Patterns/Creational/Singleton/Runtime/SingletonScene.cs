using UnityEngine;

namespace OSK
{
    // Summary:
    //     A generic singleton class that ensures only one instance of the specified MonoBehaviour
    //     type exists in the scene at any time. If multiple instances are found, it destroys
    //     the extras and returns the first instance found.
    //     not created with the intention of being persistent across scenes.
    // // Type Parameters:
    //   T:
    //     The type of MonoBehaviour that this singleton will manage. It must inherit from MonoBehaviour.
    // // Remarks:
    //     This class is useful for managing singletons that are tied to a specific
    //     scene, ensuring that only one instance exists at any given time. It automatically
    //     handles the case where multiple instances are found by destroying the extras,
    //     and it provides a static Instance property to access the singleton instance.
   
    public abstract class SingletonScene<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
             //   Logg.DebugCallChain(typeof(T).Name); // 🔍 log luồng gọi tới Singleton

                var found = Object.FindObjectsOfType<T>();

                if (found.Length > 1)
                {
                    Debug.LogWarning($"[SingletonScene<{typeof(T).Name}>] Multiple instances found. Destroying extras.");
                    for (int i = 1; i < found.Length; i++)
                        Object.Destroy(found[i].gameObject);
                }

                if (found.Length == 0)
                {
                    Debug.LogWarning($"[SingletonScene<{typeof(T).Name}>] No instance found");
                }
                else
                {
                    _instance = found[0];
                }
                return _instance;
            }
        }
    }
}