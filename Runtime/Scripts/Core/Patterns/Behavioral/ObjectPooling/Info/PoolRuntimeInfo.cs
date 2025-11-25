using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    [System.Serializable]
    public class PoolRuntimeInfo
    {
        public string GroupName; // <--- THÊM DÒNG NÀY
        
        public Object Prefab;
        public ObjectPool<Object> Pool;
        
        // ... (Các biến cũ: ObjectType, IsComponent, History...) giữ nguyên
        public string ObjectType;
        public bool IsComponent;
        public List<float> History = new List<float>();
        public int MaxHistoryPoints = 40;
        public int PeakActiveCount;
        public long EstimatedMemory;

        public int ActiveCount => Pool.CountUsedItems;
        public int TotalCount => Pool.Count;

        // Cập nhật Constructor để nhận thêm groupName
        public PoolRuntimeInfo(string groupName, Object prefab, ObjectPool<Object> pool)
        {
            GroupName = groupName; // <--- GÁN Ở ĐÂY
            Prefab = prefab;
            Pool = pool;
            
            if (prefab is GameObject) { ObjectType = "GameObject"; IsComponent = false; }
            else { ObjectType = prefab.GetType().Name; IsComponent = true; }
            
            if(prefab != null) EstimatedMemory = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(prefab);
        }

        public void UpdateStats()
        {
            if (ActiveCount > PeakActiveCount) PeakActiveCount = ActiveCount;
        }

        public void Snapshot()
        {
            float ratio = TotalCount > 0 ? (float)ActiveCount / TotalCount : 0;
            History.Add(ratio);
            if (History.Count > MaxHistoryPoints) History.RemoveAt(0);
        }
    }
}