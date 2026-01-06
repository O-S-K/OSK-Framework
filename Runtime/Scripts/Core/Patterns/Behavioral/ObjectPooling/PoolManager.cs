using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OSK
{
    public class PoolManager : GameFrameworkComponent
    {
        public Dictionary<string, Dictionary<Object, PoolRuntimeInfo>> GroupPrefabLookup { get; private set; } = new();

        public Dictionary<Object, PoolRuntimeInfo> InstanceLookup { get; private set; } = new();
        private readonly Dictionary<string, PoolRuntimeInfo> PoolKeyLookup = new();

        public bool IsDestroyAllOnSceneUnload = false;

        [Header("Visual Debugger")]
        public bool ShowDebugLines = false;
        public bool ShowLabels = false;

        [Range(0.1f, 1f)]
        public float LineOpacity = 0.3f;

        public override void OnInit()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public override void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            base.OnDestroy();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (IsDestroyAllOnSceneUnload)
                DespawnAllActive();
            InstanceLookup.Clear();
        }

        // ----------------------------------------------------------------------
        // PRELOAD LOGIC
        // ----------------------------------------------------------------------
        public void Preload(PoolItemData data)
        {
            int initialSize = (data.LoadMode == PreloadMode.Lazy) ? 0 : data.Size;

            var info = GetOrCreatePoolInfo(data.GroupName, data.Prefab, data.Parent, initialSize, data.MaxSize, data.LimitMode);

            if (!string.IsNullOrEmpty(data.Key))
            {
                if (!PoolKeyLookup.ContainsKey(data.Key))
                    PoolKeyLookup[data.Key] = info;
            }

            if (data.LoadMode == PreloadMode.Spread && initialSize > 0)
            {
                StartCoroutine(IESpreadLoad(info, initialSize));
            }
        }

        private IEnumerator IESpreadLoad(PoolRuntimeInfo info, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                // Vẫn check CanExpand để an toàn
                if (!info.CanExpand()) yield break;

                var item = info.Pool.GetItem();
                SetupInstance(item, info.DefaultParent, false);
                info.Pool.ReleaseItem(item);

                // Mỗi frame load 2 cái (hoặc 1 cái tùy chỉnh) cho mượt
                if (i % 2 == 0) yield return null;
            }
        }

        // ----------------------------------------------------------------------
        // SPAWN METHODS
        // ----------------------------------------------------------------------

        #region Spawn Helpers

        public T Spawn<T>(string groupName, T prefab, Transform parent = null, int maxSize = -1, LimitMode limit = LimitMode.RecycleOldest) where T : Object
            => Spawn(groupName, prefab, parent, Vector3.zero, Quaternion.identity, maxSize, limit);

        public T Spawn<T>(string groupName, T prefab, Transform parent, Transform transform, int maxSize = -1, LimitMode limit = LimitMode.RecycleOldest) where T : Object
            => Spawn(groupName, prefab, parent, transform.position, transform.rotation, maxSize, limit);

        public T Spawn<T>(string groupName, T prefab, Transform parent, Vector3 position, int maxSize = -1, LimitMode limit = LimitMode.RecycleOldest) where T : Object
            => Spawn(groupName, prefab, parent, position, Quaternion.identity, maxSize, limit);

        public T Spawn<T>(string groupName, T prefab, Transform parent, Vector3 position, Quaternion rotation, int maxSize = -1, LimitMode limit = LimitMode.RecycleOldest) where T : Object
        {
            var instance = SpawnInternal(groupName, prefab, parent, 1, maxSize, limit);
            if (instance == null) return null;

            if (instance is Component component)
                component.transform.SetPositionAndRotation(position, rotation);
            else if (instance is GameObject go)
                go.transform.SetPositionAndRotation(position, rotation);

            return instance;
        }

        public T Spawn<T>(string groupName, T prefab, Transform parent, int size, int maxSize = -1, LimitMode limit = LimitMode.RecycleOldest) where T : Object
        {
            return SpawnInternal(groupName, prefab, parent, size, maxSize, limit);
        }

        #endregion

        public T SpawnByKey<T>(string key, Transform parent = null) where T : Object
        {
            if (!PoolKeyLookup.TryGetValue(key, out var info))
            {
                MyLogger.LogError($"[Pool] SpawnByKey failed. Key not found: {key}");
                return null;
            }
            return SpawnCore<T>(info, parent);
        }

        private T SpawnInternal<T>(string groupName, T prefab, Transform parent, int size, int maxSize, LimitMode limit) where T : Object
        {
            var info = GetOrCreatePoolInfo(groupName, prefab, parent, size, maxSize, limit);
            if (info == null) return null;

            return SpawnCore<T>(info, parent);
        }

        private T SpawnCore<T>(PoolRuntimeInfo info, Transform parent) where T : Object
        {
            if (info.ActiveCount >= info.TotalCount)
            {
                if (!info.CanExpand())
                {
                    if (info.LimitMode == LimitMode.RejectNew)
                    {
                        MyLogger.LogWarning($"[Pool] MaxSize ({info.MaxSize}) Reached. Reject Spawn: {info.PrefabKey.name}");
                        return null;
                    }
                    if (info.LimitMode == LimitMode.RecycleOldest)
                    {
                        if (info.ActiveList.Count > 0)
                        {
                            var oldestObj = info.ActiveList[0];
                            Despawn(oldestObj); 
                        }
                    }
                }
            }

            // 2. LẤY ITEM TỪ POOL
            var rawInstance = info.Pool.GetItem();
            if (rawInstance == null)
            {
                MyLogger.LogError($"[Pool] GetItem returned null. Prefab={info.PrefabKey.name}");
                return null;
            }

            // 3. CASTING TYPE AN TOÀN
            T result = null;
            if (rawInstance is GameObject go && typeof(Component).IsAssignableFrom(typeof(T)))
            {
                result = go.GetComponent(typeof(T)) as T;
                if (result == null) MyLogger.LogError($"[Pool] Key '{info.PrefabKey.name}' is GameObject but component {typeof(T).Name} missing!");
            }
            else
            {
                result = rawInstance as T;
            }

            if (result == null) return null;

            var finalParent = parent != null ? parent : info.DefaultParent;
            SetupInstance(result, finalParent, true);
            InstanceLookup[result] = info;
            info.ActiveList.Add(result);
            info.UpdateStats();
            TriggerInterface(result, true);

            return result;
        }

        public void ExpandPool(string groupName, Object prefab, int amount)
        {
            var key = NormalizePrefabKey(prefab);
            if (!IsGroupAndPrefabExist(groupName, key)) return;
            GroupPrefabLookup[groupName][key].Pool.Refill(amount);
        }

        // ----------------------------------------------------------------------
        // DESPAWN METHODS
        // ----------------------------------------------------------------------

        #region Despawn

        public void Despawn(Object instance)
        {
            if (instance == null) return;

            if (InstanceLookup.TryGetValue(instance, out var poolInfo))
            {
                TriggerInterface(instance, false);
                SetupInstance(instance, poolInfo.DefaultParent, false);
                poolInfo.ActiveList.Remove(instance);
                InstanceLookup.Remove(instance);

                Object objectToRelease = instance;
                if (instance is Component c) objectToRelease = c.gameObject;

                poolInfo.Pool.ReleaseItem(objectToRelease);
            }
        }

        public void Despawn(Object instance, float delay, bool unscaleTime = false)
        {
            if (delay <= 0)
            {
                Despawn(instance);
                return;
            }

            DOVirtual.DelayedCall(delay, () =>
            {
                if (instance != null) Despawn(instance);
            }, unscaleTime);
        }

        public void DespawnByKey(string key, float delay, bool unscaleTime = false)
        {
            if (delay <= 0)
            {
                DespawnByKey(key);
                return;
            }

            DOVirtual.DelayedCall(delay, () => { DespawnByKey(key); }, unscaleTime);
        }

        public void DespawnByKey(string key)
        {
            if (!PoolKeyLookup.TryGetValue(key, out var info)) return;
            var toDespawn = new List<Object>(info.ActiveList);
            foreach (var obj in toDespawn) Despawn(obj);
        }

        public void DespawnAllInGroup(string groupName)
        {
            if (GroupPrefabLookup.TryGetValue(groupName, out var prefabDict))
            {
                foreach (var info in prefabDict.Values)
                {
                    var toDespawn = new List<Object>(info.ActiveList);
                    foreach (var obj in toDespawn) Despawn(obj);
                }
            }
        }

        public void DespawnAllActive()
        {
            var allActive = InstanceLookup.Keys.ToList();
            foreach (var obj in allActive) Despawn(obj);
        }

        #endregion

        // ----------------------------------------------------------------------
        // DESTROY & CLEANUP
        // ----------------------------------------------------------------------

        #region Destroy

        public void DestroyByObject(string groupName, Object prefab)
        {
            if (!IsGroupAndPrefabExist(groupName, prefab)) return;
            var pool = GroupPrefabLookup[groupName][prefab].Pool;
            pool.DestroyAndClean();
        }

        public void DestroyAllInGroup(string groupName)
        {
            DespawnAllInGroup(groupName);
            if (GroupPrefabLookup.TryGetValue(groupName, out var prefabDict))
            {
                foreach (var info in prefabDict.Values)
                {
                    info.Pool.DestroyAndClean();
                    info.Pool.Clear();
                    info.ActiveList.Clear();
                }

                GroupPrefabLookup.Remove(groupName);
            }
        }

        public void DestroyPoolByKey(string key)
        {
            if (!PoolKeyLookup.TryGetValue(key, out var info)) return;

            var toDespawn = new List<Object>(info.ActiveList);
            foreach (var obj in toDespawn) Despawn(obj);

            info.Pool.DestroyAndClean();
            info.Pool.Clear();
            info.ActiveList.Clear();

            PoolKeyLookup.Remove(key);
            if (GroupPrefabLookup.TryGetValue(info.GroupName, out var prefabDict))
            {
                prefabDict.Remove(info.PrefabKey);
                if (prefabDict.Count == 0) GroupPrefabLookup.Remove(info.GroupName);
            }
        }

        #endregion

        // ----------------------------------------------------------------------
        // INTERNAL HELPERS
        // ----------------------------------------------------------------------

        #region Helpers

        private PoolRuntimeInfo GetOrCreatePoolInfo(string group, Object prefab, Transform parent, int size, int maxSize, LimitMode limitMode)
        {
            var key = NormalizePrefabKey(prefab);
            if (key == null) return null;

            if (!GroupPrefabLookup.TryGetValue(group, out var prefabDict))
            {
                prefabDict = new Dictionary<Object, PoolRuntimeInfo>();
                GroupPrefabLookup[group] = prefabDict;
            }

            if (!prefabDict.TryGetValue(key, out var info))
            {
                info = new PoolRuntimeInfo(group, key, parent, maxSize, limitMode);

                // 2. Định nghĩa hàm tạo object (ĐẾM SỐ LƯỢNG TẠI ĐÂY)
                System.Func<Object> createMethod = () =>
                {
                    var obj = InstantiatePrefab(key, info.DefaultParent);

                    info.RealTotalCount++;
                    //MyLogger.Log($"[Pool Debug] Created new {key.name}. Total: {info.RealTotalCount}/{maxSize}");
                    return obj;
                };

                info.Pool = new ObjectPool<Object>(createMethod, size);

                for (int i = 0; i < size; i++)
                {
                    if (maxSize > 0 && info.RealTotalCount >= maxSize) break;
                    var item = info.Pool.GetItem();
                    SetupInstance(item, info.DefaultParent, false);
                    info.Pool.ReleaseItem(item);
                }

                prefabDict[key] = info;
                return info;
            }

            if (info.DefaultParent == null && parent != null) info.DefaultParent = parent;
            return info;
        }

        private Object InstantiatePrefab(Object prefab, Transform parent)
        {
            return prefab is GameObject go
                ? Instantiate(go, parent)
                : Instantiate((Component)prefab, parent);
        }

        private void SetupInstance(Object instance, Transform parent, bool active)
        {
            if (instance is Component component)
            {
                component.gameObject.SetActive(active);
                if (parent != null) component.transform.SetParent(parent);
            }
            else if (instance is GameObject go)
            {
                go.SetActive(active);
                if (parent != null) go.transform.SetParent(parent);
            }
        }

        private void TriggerInterface(Object instance, bool isSpawn)
        {
            GameObject go = instance is Component c ? c.gameObject : instance as GameObject;
            if (go == null) return;

            var poolables = go.GetComponents<IPoolable>();
            foreach (var p in poolables)
            {
                if (isSpawn) p.OnSpawn();
                else p.OnDespawn();
            }
        }

        private bool IsGroupAndPrefabExist(string groupName, Object prefab)
        {
            return GroupPrefabLookup.ContainsKey(groupName) &&
                   GroupPrefabLookup[groupName].ContainsKey(prefab);
        }

        public bool HasGroup(string groupName) => GroupPrefabLookup.ContainsKey(groupName);

        private Object NormalizePrefabKey(Object prefab)
        {
            if (prefab is GameObject go) return go;
            if (prefab is Component c) return c.gameObject;
            return prefab;
        }

        #endregion

        // ----------------------------------------------------------------------
        // EDITOR DEBUG
        // ----------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if ((!ShowDebugLines && !ShowLabels) || InstanceLookup.Count == 0) return;
            Vector3 centerPos = transform.position;

            foreach (var item in InstanceLookup)
            {
                Object objRef = item.Key;
                PoolRuntimeInfo info = item.Value;
                GameObject go = objRef is Component c ? c.gameObject : objRef as GameObject;

                if (go != null && go.activeInHierarchy)
                {
                    Vector3 targetPos = go.transform.position;
                    int hash = info.GroupName.GetHashCode();
                    Color groupColor = Color.HSVToRGB(Mathf.Abs(hash % 100) / 100f, 0.8f, 1f);

                    if (ShowDebugLines)
                    {
                        Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, LineOpacity);
                        Gizmos.DrawLine(centerPos, targetPos);
                    }

                    if (ShowLabels)
                    {
                        if (UnityEditor.SceneView.currentDrawingSceneView != null)
                        {
                            var cam = UnityEditor.SceneView.currentDrawingSceneView.camera;
                            if (Vector3.Distance(cam.transform.position, targetPos) < 40f)
                            {
                                string text = $"<color=#{ColorUtility.ToHtmlStringRGB(groupColor)}>{info.GroupName}</color>\n<b>{go.name}</b>";
                                var timerScript = go.GetComponent<AutoDespawn>(); // Nếu bạn có class này
                                if (timerScript != null)
                                {
                                    float t = timerScript.TimeLeft; // Giả sử biến TimeLeft là public
                                    string colorHex = t < 1.0f ? "red" : "yellow";
                                    text += $"\n<color={colorHex}>⏳ {t:0.0}s</color>";
                                }

                                GUIStyle style = new GUIStyle();
                                style.alignment = TextAnchor.MiddleCenter;
                                style.fontSize = 10;
                                style.richText = true;
                                style.normal.textColor = Color.white;
                                UnityEditor.Handles.Label(targetPos + Vector3.up * 1.5f, text, style);
                            }
                        }
                    }
                }
            }
        }
#endif
    }
}