using UnityEngine;

namespace OSK
{
    // Summary:
    // A generic singleton class that ensures only one instance of the specified MonoBehaviour type exists globally.
    // The instance is automatically created if it does not exist, and it can be set to persist across
    // scene loads.
    //   Type Parameters:
    //   T: The type of MonoBehaviour that this singleton will manage.
    // Remarks:
    //   - The singleton instance is accessed via the static Instance property.
    //   - If multiple instances are found, all but the first one will be destroyed.
    //   - The IsDontDestroySingleton property can be overridden to control whether the instance
    //     should persist across scene loads.
    //   - The instance is created with the name of the type T if it does not
    //     already exist in the scene.
    
    
    public abstract class SingletonGlobal<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected virtual bool IsDontDestroySingleton => true;

        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var found = Object.FindObjectsOfType<T>();

                if (found.Length > 1)
                {
                    Logg.LogWarning(
                        $"[SingletonGlobal<{typeof(T).Name}>] Multiple instances found. Destroying extras.");
                    for (int i = 1; i < found.Length; i++)
                        Object.Destroy(found[i].gameObject);
                }

                _instance = found.Length > 0 ? found[0] : new GameObject(typeof(T).Name).AddComponent<T>();

                if ((_instance as SingletonGlobal<T>)?.IsDontDestroySingleton == true)
                {
                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }
    }
}