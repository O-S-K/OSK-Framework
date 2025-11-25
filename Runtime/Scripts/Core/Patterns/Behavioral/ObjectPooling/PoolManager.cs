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
        public bool IsDestroyAllOnSceneUnload = false;

        // --- CONFIG AR DEBUGGER ---
        [Header("Visual Debugger")]
        public bool ShowDebugLines = false;   // Vẽ đường dây
        public bool ShowLabels = false;       // Vẽ tên trên đầu
        [Range(0.1f, 1f)]
        public float LineOpacity = 0.3f;     // Độ mờ của dây (để không rối mắt)
        
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
            if(IsDestroyAllOnSceneUnload)
                DespawnAllActive();
        }

        public void Preload(PoolData poolData)
        {
            GetOrCreatePoolInfo(poolData.GroupName, poolData.Prefab, poolData.Parent, poolData.Size);
        }

        #region Spawn Methods

        public T Spawn<T>(string groupName, T prefab, Transform parent = null) where T : Object
            => Spawn(groupName, prefab, parent, Vector3.zero, Quaternion.identity);

        public T Spawn<T>(string groupName, T prefab, Transform parent, Transform transform) where T : Object
            => Spawn(groupName, prefab, parent, transform.position, transform.rotation);

        public T Spawn<T>(string groupName, T prefab, Transform parent, Vector3 position) where T : Object
            => Spawn(groupName, prefab, parent, position, Quaternion.identity);

        public T Spawn<T>(string groupName, T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Object
        {
            var instance = SpawnInternal(groupName, prefab, parent, 1);
            if (instance == null) return null;

            // Set Position/Rotation
            if (instance is Component component)
            {
                component.transform.SetPositionAndRotation(position, rotation);
            }
            else if (instance is GameObject go)
            {
                go.transform.SetPositionAndRotation(position, rotation);
            }

            return instance;
        }

        public T Spawn<T>(string groupName, T prefab, Transform parent, int size) where T : Object
        {
            return SpawnInternal(groupName, prefab, parent, size);
        }

        private T SpawnInternal<T>(string groupName, T prefab, Transform parent, int size) where T : Object
        {
            var poolInfo = GetOrCreatePoolInfo(groupName, prefab, parent, size);
            var instance = poolInfo.Pool.GetItem() as T;

            if (instance == null)
            {
                OSKLogger.LogError("Pool", $"Object from pool is null. Group: {groupName}, Prefab: {prefab.name}");
                return null;
            }
            SetupInstance(instance, parent, true);
            if (!InstanceLookup.TryAdd(instance, poolInfo))
            {
                OSKLogger.LogWarning("Pool", $"Instance lookup already contains: {instance}");
            }
            poolInfo.UpdateStats(); 
            TriggerInterface(instance, true);

            return instance;
        }

        #endregion

        #region Despawn Methods

        public void Despawn(Object instance)
        {
            if (instance == null) return;

            if (InstanceLookup.TryGetValue(instance, out var poolInfo))
            {
                TriggerInterface(instance, false);
                SetupInstance(instance, null, false);
                poolInfo.Pool.ReleaseItem(instance);
                InstanceLookup.Remove(instance);
            }
            else
            {
                OSKLogger.LogWarning("Pool", $"{instance} not found in any pool lookup.");
                SetupInstance(instance, null, false);
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

        public void DespawnAllInGroup(string groupName)
        {
            if (GroupPrefabLookup.TryGetValue(groupName, out var prefabDict))
            {
                var toDespawn = new List<Object>();
                foreach (var pair in InstanceLookup)
                {
                    if (prefabDict.ContainsValue(pair.Value))
                    {
                        toDespawn.Add(pair.Key);
                    }
                }

                foreach (var obj in toDespawn) Despawn(obj);
            }
        }

        public void DespawnAllActive()
        {
            var allActive = InstanceLookup.Keys.ToList();
            foreach (var obj in allActive) Despawn(obj);
        }

        #endregion

        #region Editor Tool Methods (Cho Ultimate Window)

        public void ExpandPool(string groupName, Object prefab, int amount)
        {
            if (!IsGroupAndPrefabExist(groupName, prefab)) return;
            
            var pool = GroupPrefabLookup[groupName][prefab].Pool;
            pool.Refill(amount); // Gọi hàm Refill có sẵn trong ObjectPool của bạn
        }
 
        public void TrimPool(string groupName, Object prefab)
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
                }
                GroupPrefabLookup.Remove(groupName);
            }
        }

        #endregion

        #region Internal Helpers

        private PoolRuntimeInfo GetOrCreatePoolInfo(string group, Object prefab, Transform parent, int size)
        {
            if (!GroupPrefabLookup.ContainsKey(group)) GroupPrefabLookup[group] = new Dictionary<Object, PoolRuntimeInfo>();
            if (!GroupPrefabLookup[group].ContainsKey(prefab))
            {
                if (size <= 0) size = 1;
                var pool = new ObjectPool<Object>(() => InstantiatePrefab(prefab, parent), size);
                 
                // TRUYỀN THÊM 'group' VÀO ĐÂY
                var info = new PoolRuntimeInfo(group, prefab, pool); 
                 
                GroupPrefabLookup[group][prefab] = info;
            }
            return GroupPrefabLookup[group][prefab];
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
                if (isSpawn) p.OnSpawn(); else p.OnDespawn();
            }
        }

        private bool IsGroupAndPrefabExist(string groupName, Object prefab)
        {
            return GroupPrefabLookup.ContainsKey(groupName) &&
                   GroupPrefabLookup[groupName].ContainsKey(prefab);
        }

        public bool HasGroup(string groupName) => GroupPrefabLookup.ContainsKey(groupName);
        
        public T Query<T>(string groupName, T prefab) where T : Object
        {
            if (GroupPrefabLookup.TryGetValue(groupName, out var prefabPools))
            {
                if (prefabPools.TryGetValue(prefab, out var info))
                {
                    return info.Pool.GetItem() as T; 
                }
            }
            return null;
        }

        #endregion
        
        
       #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if ((!ShowDebugLines && !ShowLabels) || InstanceLookup.Count == 0) return;

            Vector3 centerPos = transform.position;

            foreach (var item in InstanceLookup)
            {
                Object objRef = item.Key;
                PoolRuntimeInfo info = item.Value;

                GameObject go = null;
                if (objRef is Component c) go = c.gameObject;
                else if (objRef is GameObject g) go = g;

                if (go != null && go.activeInHierarchy)
                {
                    Vector3 targetPos = go.transform.position;
                    int hash = info.GroupName.GetHashCode();
                    Color groupColor = Color.HSVToRGB(Mathf.Abs(hash % 100) / 100f, 0.8f, 1f);
                    
                    // 1. VẼ LINE
                    if (ShowDebugLines)
                    {
                        Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, LineOpacity);
                        Gizmos.DrawLine(centerPos, targetPos);
                    }

                    // 2. VẼ LABEL (CÓ THỜI GIAN)
                    if (ShowLabels)
                    {
                        if (UnityEditor.SceneView.currentDrawingSceneView != null)
                        {
                            var cam = UnityEditor.SceneView.currentDrawingSceneView.camera;
                            if (Vector3.Distance(cam.transform.position, targetPos) < 40f)
                            {
                                // Cơ bản: Tên Group và Tên Object
                                string text = $"<color=#{ColorUtility.ToHtmlStringRGB(groupColor)}>{info.GroupName}</color>\n<b>{go.name}</b>";

                                var timerScript = go.GetComponent<AutoDespawn>();
                                if (timerScript != null)
                                {
                                    float t = timerScript.TimeLeft;
                                    string colorHex = t < 1.0f ? "red" : "yellow"; // Dưới 1s thì báo đỏ
                                    text += $"\n<color={colorHex}>⏳ {t:0.0}s</color>"; // Hiện icon đồng hồ và số giây
                                }
                                // --------------------------------

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