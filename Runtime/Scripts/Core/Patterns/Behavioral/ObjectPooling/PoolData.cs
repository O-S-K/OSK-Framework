using UnityEngine;

namespace OSK
{
    [System.Serializable]
    public class PoolData
    {
        public string GroupName = KEY_POOL.KEY_POOL_DEFAULT_CONTAINER;
        public Object Prefab;
        public Transform Parent = null;
        public int Size = 10;

        public PoolData(string groupName, Object prefab, Transform parent, int size)
        {
            GroupName = groupName;
            Prefab = prefab;
            Parent = parent;
            Size = size;
        }
    }
}