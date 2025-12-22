using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

#if CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace OSK
{
    public partial class ResourceManager : GameFrameworkComponent
    {
        // Cache cho Resources
        private readonly Dictionary<string, Object> k_ResourceCache = new();
        private readonly Dictionary<string, int> k_ReferenceCount = new();

        public override void OnInit()
        {
        }

      
        public T Load<T>(string path, bool usePool = false) where T : Object
        {
            if (k_ResourceCache.TryGetValue(path, out var cached))
            {
                k_ReferenceCount[path]++;
                return (T)cached;
            }

            T resource = Resources.Load<T>(path);
            if (resource == null)
            {
                OSK.MyLogger.LogError($"[ResourceManager] Không tìm thấy resource tại: {path}");
                return null;
            }

            if (usePool)
                resource = Main.Pool.Spawn(KEY_POOL.KEY_POOL_RESOURCE, resource, transform);

            k_ResourceCache[path] = resource;
            k_ReferenceCount[path] = 1;

            return resource;
        }

        public T Spawn<T>(string path, bool usePool = false) where T : Object
        {
            T resource = Load<T>(path);
            if (resource == null)
            {
                OSK.MyLogger.LogError($"[ResourceManager] Spawn thất bại: {path}");
                return null;
            }

            return usePool
                ? Main.Pool.Spawn(KEY_POOL.KEY_POOL_RESOURCE, Instantiate(resource), transform)
                : Instantiate(resource);
        }

        public void Unload(string path)
        {
            if (!k_ResourceCache.TryGetValue(path, out var resource)) return;

            k_ReferenceCount[path]--;
            if (k_ReferenceCount[path] <= 0)
            {
                Resources.UnloadAsset(resource);
                k_ResourceCache.Remove(path);
                k_ReferenceCount.Remove(path);
            }
        }
    }
}