using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    // SingletonManager handles both global and scene-specific singletons.
    // Global singletons persist across scene loads, while scene singletons are tied to the current scene.
    // The manager ensures only one instance of each type exists, destroying duplicates as needed.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1111)] // Ensure it initializes early
    public class SingletonManager : MonoBehaviour
    {
        private static SingletonManager _instance;

        public static SingletonManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SingletonManager");
                    _instance = go.AddComponent<SingletonManager>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        private readonly Dictionary<Type, SingletonInfo> _globalSingletons = new();
        private readonly Dictionary<Type, SingletonInfo> _sceneSingletons = new();

        public Dictionary<Type, SingletonInfo> GetGlobalSingletons() => _globalSingletons;
        public Dictionary<Type, SingletonInfo> GetSceneSingletons() => _sceneSingletons;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeManager()
        {
            if (_instance == null)
            {
                var go = new GameObject("======= [OSK SingletonManager] ==========");
                _instance = go.AddComponent<SingletonManager>();
                DontDestroyOnLoad(go);
            }
        }

        // Example usage: Awake ()
        // { 
        //      SingletonManager.Instance.RegisterScene(this); or SingletonManager.Instance.RegisterGlobal(this);
        // }

        public void RegisterGlobal(MonoBehaviour instance)
        {
            if (instance == null) return;

            Type type = instance.GetType();
            if (_globalSingletons.ContainsKey(type))
            {
                if (_globalSingletons[type].instance != instance)
                {
                    Destroy(_globalSingletons[type].instance.gameObject);
                    _globalSingletons.Remove(type);
                }
            }

            _globalSingletons[type] = new SingletonInfo(instance);
            DontDestroyOnLoad(instance.gameObject);
        }

        public void RegisterScene(MonoBehaviour instance)
        {
            if (instance == null) return;

            Type type = instance.GetType();
            if (_sceneSingletons.ContainsKey(type))
            {
                var oldInstance = _sceneSingletons[type].instance;
                if (oldInstance != null && oldInstance != instance)
                {
                    Destroy(oldInstance.gameObject);
                }
            }

            _sceneSingletons[type] = new SingletonInfo(instance);
        }

        public T Get<T>() where T : MonoBehaviour
        {
            Type type = typeof(T);

            if (_globalSingletons.TryGetValue(type, out var gInfo))
            {
                if (gInfo.instance == null)
                {
                    _globalSingletons.Remove(type);
                    return null;
                }

                return gInfo.instance as T;
            }

            if (_sceneSingletons.TryGetValue(type, out var sInfo))
            {
                if (sInfo.instance == null)
                {
                    _sceneSingletons.Remove(type);
                    return null;
                }

                return sInfo.instance as T;
            }

            return null;
        }

        public void CleanAllSingletons()
        {
            _globalSingletons.Clear();
            _sceneSingletons.Clear();
        }
    }
}