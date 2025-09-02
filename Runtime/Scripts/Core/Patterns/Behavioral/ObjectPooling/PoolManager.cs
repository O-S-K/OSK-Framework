using System.Linq;
using DG.Tweening;
using UnityEngine;
using System.Collections.Generic;

namespace OSK
{
    public class PoolManager : GameFrameworkComponent
    {
        public Dictionary<string, Dictionary<Object, ObjectPool<Object>>> GroupPrefabLookup { get; private set; } = new();
        public Dictionary<Object, ObjectPool<Object>> InstanceLookup { get; private set; } = new();

        public override void OnInit() {}

        public void Preload(PoolData poolData)
        {
            WarmPool(poolData.GroupName, poolData.Prefab, poolData.Parent, poolData.Size);
        } 
        
        #region Spawn Methods

        public T Spawn<T>(string groupName, T prefab, Transform parent = null) where T : Object
        {
            return Spawn(groupName, prefab, parent, Vector3.zero, Quaternion.identity);
        }

        public T Spawn<T>(string groupName, T prefab, Transform parent, Transform transform) where T : Object
        {
            return Spawn(groupName, prefab, parent, transform.position, transform.rotation);
        }

        public T Spawn<T>(string groupName, T prefab, Transform parent, Vector3 position) where T : Object
        {
            return Spawn(groupName, prefab, parent, position, Quaternion.identity);
        }

        public T Spawn<T>(string groupName, T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Object
        {
            var instance = Spawn(groupName, prefab, parent, 1);
            if (instance is Component component)
            {
                component.transform.position = position;
                component.transform.rotation = rotation;
            }
            else if (instance is GameObject go)
            {
                go.transform.position = position;
                go.transform.rotation = rotation;
            }

            return instance;
        }

        public T Spawn<T>(string groupName, T prefab, Transform parent, int size) where T : Object
        {
            if (!IsGroupAndPrefabExist(groupName, prefab))
            {
                if (size <= 0)
                {
                    Logg.LogError("Pool size must be greater than 0.");
                    return null;
                }

                WarmPool(groupName, prefab, parent, size);
            }

            var pool = GroupPrefabLookup[groupName][prefab];
            var instance = pool.GetItem() as T;

            if (instance == null)
            {
                Logg.LogError($"Object from pool is null or destroyed. Group: {groupName}, Prefab: {prefab.name}");
                return null;
            }

            switch (instance)
            {
                case Component component:
                    component.gameObject.SetActive(true);
                    component.transform.SetParent(parent);
                    break;
                case GameObject go:
                    go.SetActive(true);
                    go.transform.SetParent(parent);
                    break;
            }

            if (!InstanceLookup.TryAdd(instance, pool))
            {
                Logg.LogWarning($"This object pool already contains the item provided: {instance}");
                return instance;
            }

            return instance;
        }

        private void WarmPool<T>(string group, T prefab, Transform parent, int size) where T : Object
        {
            if (IsGroupAndPrefabExist(group, prefab))
            {
                Logg.LogError($"Pool for prefab '{prefab.name}' in group '{group}' has already been created.");
                return;
            }

            if (size <= 0)
            {
                Logg.LogError("Pool size must be greater than 0.");
                return;
            }

            var pool = new ObjectPool<Object>(() => InstantiatePrefab(prefab, parent), size);
            if (!GroupPrefabLookup.ContainsKey(group))
            {
                GroupPrefabLookup[group] = new Dictionary<Object, ObjectPool<Object>>();
            }

            GroupPrefabLookup[group][prefab] = pool;
        }

        private Object InstantiatePrefab<T>(T prefab, Transform parent) where T : Object
        {
            return prefab is GameObject go
                ? Instantiate(go, parent)
                : Instantiate((Component)(object)prefab, parent);
        }
        #endregion
        
        #region Despawn Methods

        public void Despawn(Object instance)
        {
            DeactivateInstance(instance);
            if (InstanceLookup.TryGetValue(instance, out var pool))
            {
                pool.ReleaseItem(instance);
                InstanceLookup.Remove(instance);
            }
            else
            {
                Logg.LogWarning($"{instance} not found in any pool.");
            }
        }

        public void Despawn(Object instance, float delay, bool unscaleTime = false)
        {
            DOVirtual.DelayedCall(delay, () =>
            {
                if (instance != null) Despawn(instance);
            }, unscaleTime);
        }

        public void DespawnAllInGroup(string groupName)
        {
            if (GroupPrefabLookup.TryGetValue(groupName, out var prefabPools))
            {
                foreach (var pool in prefabPools.Values)
                {
                    List<Object> toRemove = new();
                    foreach (var pair in InstanceLookup)
                    {
                        if (pair.Value == pool)
                        {
                            DeactivateInstance(pair.Key);
                            pool.ReleaseItem(pair.Key);
                            toRemove.Add(pair.Key);
                        }
                    }

                    foreach (var obj in toRemove)
                        InstanceLookup.Remove(obj);
                }
            }
        }

        public void DespawnAllActive()
        {
            foreach (var kv in InstanceLookup)
            {
                DeactivateInstance(kv.Key);
                kv.Value.ReleaseItem(kv.Key);
            }

            InstanceLookup.Clear();
        }

        private void DeactivateInstance(Object instance)
        {
            if (instance is Component component)
                component.gameObject.SetActive(false);
            else if (instance is GameObject go)
                go.SetActive(false);
        }

        public void DestroyAllInGroup(string groupName)
        {
            if (GroupPrefabLookup.TryGetValue(groupName, out var prefabPools))
            {
                // Create a copy of the dictionary to avoid modifying it while iterating
                foreach (var kvp in prefabPools.ToList())
                {
                    var pool = kvp.Value;
                    pool.DestroyAndClean();
                    pool.Clear();
                }

                GroupPrefabLookup.Remove(groupName);
            }
        }

        public void DestroyAllGroups()
        {
            foreach (var prefabPools in GroupPrefabLookup.Values)
            {
                foreach (var pool in prefabPools.Values)
                {
                    pool.DestroyAndClean();
                    pool.Clear();
                }
            }

            GroupPrefabLookup.Clear();
        }

        public void CleanAllDestroyedInPools()
        {
            foreach (var prefabPools in GroupPrefabLookup.Values)
            {
                foreach (var pool in prefabPools.Values)
                {
                    pool.DestroyAndClean();
                }
            }
        }

        #endregion

        #region Query Methods

        public bool HasGroup(string groupName)
        {
            return GroupPrefabLookup.ContainsKey(groupName);
        }
        
        public T Query<T>(string groupName, T prefab) where T : Object
        {
            if (GroupPrefabLookup.TryGetValue(groupName, out var prefabPools))
            {
                if (prefabPools.TryGetValue(prefab, out var pool))
                {
                    return pool.GetItem() as T;
                }
            }

            return null;
        }

        public string GetGroupFormToPrefab<T>(T prefab) where T : Object
        {
            foreach (var group in GroupPrefabLookup)
            {
                if (group.Value.ContainsKey(prefab))
                {
                    return group.Key;
                }
            }

            return null;
        }
        
        private bool IsGroupAndPrefabExist(string groupName, Object prefab)
        {
            return GroupPrefabLookup.ContainsKey(groupName) &&
                   GroupPrefabLookup[groupName].ContainsKey(prefab);
        }

        #endregion
    }
}