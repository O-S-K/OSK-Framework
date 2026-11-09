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

        public static bool HasInstance => _instance != null;

        public static SingletonManager Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<SingletonManager>();
                if (_instance != null) return _instance;

                if (!Application.isPlaying)
                {
                    // Trong Edit Mode, không bao giờ tạo GameObject lưu vào scene hierarchy.
                    // Tạo một đối tượng ẩn tạm thời với HideAndDontSave để các lệnh gọi Get<T>()
                    // an toàn trả về null mà không văng lỗi NRE hay làm bẩn scene hierarchy.
                    var editGo = new GameObject("[OSK SingletonManager (EditMode)]")
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    _instance = editGo.AddComponent<SingletonManager>();
                    _instance.hideFlags = HideFlags.HideAndDontSave;
                    return _instance;
                }

                var go = new GameObject("====== [OSK SingletonManager] ======");
                _instance = go.AddComponent<SingletonManager>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                // Tiêu hủy ngay lập tức các GameObject SingletonManager bị trùng
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        private readonly Dictionary<Type, SingletonInfo> _globalSingletons = new();
        private readonly Dictionary<Type, SingletonInfo> _sceneSingletons = new();

        public Dictionary<Type, SingletonInfo> GetGlobalSingletons() => _globalSingletons;
        public Dictionary<Type, SingletonInfo> GetSceneSingletons() => _sceneSingletons;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeManager()
        {
            if (_instance != null && (_instance.gameObject.hideFlags & HideFlags.DontSave) != 0)
            {
                DestroyImmediate(_instance.gameObject);
                _instance = null;
            }

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SingletonManager>();
                if (_instance == null)
                {
                    var go = new GameObject("====== [OSK SingletonManager] ======");
                    _instance = go.AddComponent<SingletonManager>();
                    DontDestroyOnLoad(go);
                }
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
            if (_globalSingletons.TryGetValue(type, out var existingInfo) && existingInfo != null && existingInfo.instance != null)
            {
                if (existingInfo.instance != instance)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(instance.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(instance.gameObject);
                    }
                    return;
                }
                return;
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
                if (gInfo.instance != null) return gInfo.instance as T;
                _globalSingletons.Remove(type);
                return null;
            }
            if (_sceneSingletons.TryGetValue(type, out var sInfo))
            {
                if (sInfo.instance != null) return sInfo.instance as T;
                _sceneSingletons.Remove(type);
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