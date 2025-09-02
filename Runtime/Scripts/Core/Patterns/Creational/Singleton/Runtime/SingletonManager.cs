using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OSK
{
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


        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeManager()
        {
            if (_instance == null)
            {
                var go = new GameObject("SingletonManager");
                _instance = go.AddComponent<SingletonManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string currentScene = scene.name;
            var toRemove = new List<Type>();

            foreach (var kvp in _sceneSingletons)
            {
                var info = kvp.Value;

                // Nếu scene hiện tại không nằm trong whitelist -> destroy instance
                if (!info.IsValidInScene(currentScene))
                {
                    if (info.instance != null)
                        Destroy(info.instance.gameObject);
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
                _sceneSingletons.Remove(key);

            // Tự động register lại các singleton scene mới nếu có trên scene
            AutoRegisterGlobal();
            AutoRegisterSceneSingletons();
        }

        public void RegisterGlobal(MonoBehaviour instance)
        {
            if (instance == null) return;

            Type type = instance.GetType();
            // Nếu có attribute Global
            var globalAttr = Attribute.GetCustomAttribute(type, typeof(SingletonGlobalAttribute));
            if (globalAttr != null)
            {
                // Nếu đã có instance khác → huỷ cái cũ, giữ cái mới
                if (_globalSingletons.ContainsKey(type))
                {
                    if (_globalSingletons[type].instance != instance)
                        Destroy(_globalSingletons[type].instance.gameObject);
                }

                _globalSingletons[type] = new SingletonInfo(instance);
                DontDestroyOnLoad(instance.gameObject);
            }
        }

        public void RegisterScene(MonoBehaviour instance)
        {
            if (instance == null) return;

            Type type = instance.GetType();
            // Nếu có attribute Scene
            var sceneAttr = Attribute.GetCustomAttribute(type, typeof(SingletonSceneAttribute));
            if (sceneAttr != null)
            {
                var allowedScenes = ((SingletonSceneAttribute)sceneAttr).Scenes.ToList();
                string currentScene = SceneManager.GetActiveScene().name;

                // Nếu scene hiện tại không hợp lệ → huỷ instance
                if (allowedScenes.Count > 0 && !allowedScenes.Contains(currentScene))
                {
                    Destroy(instance.gameObject);
                    return;
                }

                // Nếu đã có instance khác → huỷ cái cũ, giữ cái trên scene mới
                if (_sceneSingletons.ContainsKey(type))
                {
                    var oldInstance = _sceneSingletons[type].instance;
                    if (oldInstance != null && oldInstance != instance)
                    {
                        Destroy(oldInstance.gameObject);
                    }
                }

                _sceneSingletons[type] = new SingletonInfo(instance, allowedScenes);
            }
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

        public void CleanupSceneSingletons(string currentScene)
        {
            var toRemove = new List<Type>();

            foreach (var kvp in _sceneSingletons)
            {
                var info = kvp.Value;

                if (!info.IsValidInScene(currentScene) || info.instance == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
                _sceneSingletons.Remove(key);
        }

        public void CleanAllSingletons()
        {
            _globalSingletons.Clear();
            _sceneSingletons.Clear();
        }

        public static void AutoRegisterSceneSingletons()
        {
            foreach (var mono in FindObjectsOfType<MonoBehaviour>())
            {
                Type type = mono.GetType();
                if (Attribute.IsDefined(type, typeof(SingletonSceneAttribute)))
                {
                    Instance.RegisterScene(mono);
                }
            }
        }

        public static void AutoRegisterGlobal()
        {
            foreach (var mono in FindObjectsOfType<MonoBehaviour>())
            {
                Type type = mono.GetType();
                if (Attribute.IsDefined(type, typeof(SingletonGlobalAttribute)))
                {
                    Instance.RegisterGlobal(mono);
                }
            }
        }

        public static SingletonInfo GetSingletonInfo(MonoBehaviour instance)
        {
            if (instance == null) return null;
            SingletonInfo info = null;
            Type type = instance.GetType();
            if (Instance._globalSingletons.TryGetValue(type, out var gInfo) && gInfo.instance == instance)
            {
                info = gInfo;
            }
            if (Instance._sceneSingletons.TryGetValue(type, out var sInfo) && sInfo.instance == instance)
            {
                info = sInfo;
            }
            return info;
        }
    }
}