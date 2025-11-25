using UnityEngine;
using System.Collections.Generic;

namespace OSK
{
    public class CompoundPoolable : MonoBehaviour, IPoolable
    {
        [System.Serializable]
        public class SubPart
        {
            public string poolGroup;
            public GameObject prefab;
            public Transform spawnPoint;
        }

        public List<SubPart> parts = new List<SubPart>();
        private List<GameObject> _spawnedParts = new List<GameObject>();

     
        public void OnSpawn()
        {
            foreach (var part in parts)
            {
                GameObject obj = Main.Pool.Spawn(part.poolGroup, part.prefab, null, part.spawnPoint.position, part.spawnPoint.rotation);
                obj.transform.SetParent(part.spawnPoint); 
                _spawnedParts.Add(obj);
            }
        }

        public void OnDespawn()
        {
            foreach (var obj in _spawnedParts)
            {
                if (obj != null && obj.activeSelf)
                {
                    Main.Pool.Despawn(obj);
                }
            }
            _spawnedParts.Clear();
        }
    }
}