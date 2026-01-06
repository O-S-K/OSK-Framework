using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace OSK
{
    [DefaultExecutionOrder(-101)] 
    public class PoolConfig : MonoBehaviour 
    {
        [Title("Pool Preload Configuration")]
        [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true, ShowFoldout =  true)]
        [TableList]
        public List<PoolGroupData> Groups = new();

        private void Awake()
        {
            PreloadAll();
            CheckDuplicateKeys();
        }

        [Button(ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.4f)]
        public void PreloadAll()
        {
            foreach (var group in Groups)
            {
                if (string.IsNullOrEmpty(group.GroupName))
                {
                    MyLogger.LogWarning("[PoolPreload] GroupName is empty", this);
                    continue;
                }

                foreach (var pool in group.Pools)
                {
                    if (pool.Prefab == null)
                    {
                        MyLogger.LogWarning($"[PoolPreload] Pool '{pool.GroupName}' prefab is null",this);
                        continue;
                    }

                    var poolItem = new PoolItemData {
                        GroupName = group.GroupName,
                        Key = pool.Key, 
                        Prefab = pool.Prefab,
                        Parent = pool.Parent,
                        Size =  pool.Size,
                        MaxSize = pool.MaxSize,
                        LoadMode = pool.LoadMode,
                        LimitMode = pool.LimitMode
                    };
                    MyLogger.Log("PoolPreload: " + poolItem);
                    Main.Pool.Preload(poolItem);
                }
            }
        }
        
        [Button(ButtonSizes.Small), GUIColor(1f, 0.4f, 0.4f)]
        public void ClearAll()
        {
            foreach (var group in Groups)
            {
                if (string.IsNullOrEmpty(group.GroupName))
                {
                    MyLogger.LogWarning("[PoolPreload] GroupName is empty", this);
                    continue;
                }

                foreach (var pool in group.Pools)
                {
                    Main.Pool.DestroyPoolByKey(pool.Key);
                }
            } 
        }
        
        private void CheckDuplicateKeys()
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (var group in Groups)
            {
                foreach (var pool in group.Pools)
                {
                    if (keys.Contains(pool.Key))
                    {
                        MyLogger.LogWarning($"[PoolPreload] Duplicate key found: {pool.Key}", this);
                    }
                    else
                    {
                        keys.Add(pool.Key);
                    }
                }
            }
        }
    }
}