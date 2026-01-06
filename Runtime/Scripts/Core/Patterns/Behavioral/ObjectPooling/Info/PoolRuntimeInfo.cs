using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    [System.Serializable]
    public class PoolRuntimeInfo
    {
        public string GroupName;
        public Object PrefabKey; 
        public ObjectPool<Object> Pool;
        public Transform DefaultParent;

        public int MaxSize;
        public LimitMode LimitMode;
        
        public int RealTotalCount = 0;
        public string ObjectType;
        public bool IsComponent;
        
        public List<Object> ActiveList = new List<Object>(); 

        public int PeakActiveCount;
        public long EstimatedMemory;

        public int ActiveCount => Pool.CountUsedItems;
        public int TotalCount => Pool.Count;


        public PoolRuntimeInfo(string groupName, Object prefabKey, Transform parent, int maxSize, LimitMode limitMode)        
        {
            GroupName = groupName;
            PrefabKey = prefabKey;
            DefaultParent = parent;
            MaxSize = maxSize;
            LimitMode = limitMode;

            if (prefabKey is GameObject)
            {
                ObjectType = "GameObject";
                IsComponent = false;
            }
            else
            {
                ObjectType = prefabKey.GetType().Name;
                IsComponent = true;
            }

            if (prefabKey != null)
                EstimatedMemory = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(prefabKey);
        }

        // Logic kiểm tra xem có được tạo thêm không
        public bool CanExpand()
        {
            // MaxSize <= 0 nghĩa là Vô hạn -> Luôn trả về true
            if (MaxSize <= 0) return true;
            
            // Nếu chưa chạm trần -> true
            if (TotalCount < MaxSize) return true;

            // Đã chạm trần -> false
            return false;
        }

        public void UpdateStats()
        {
            if (ActiveCount > PeakActiveCount)
                PeakActiveCount = ActiveCount;
        }
    }
}