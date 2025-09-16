using UnityEngine;
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
    public partial class ResourceManager
    {
        
#if USE_ADDRESSABLES
        private readonly Dictionary<string, AsyncOperationHandle> k_LoadedHandles = new();
        private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> k_InstanceHandles = new();
#endif


#if USE_ADDRESSABLES && CYSHARP_UNITASK

        public async UniTask<T> LoadAddressable<T>(AssetReference assetRef) where T : UnityEngine.Object
        {
            if (k_LoadedHandles.TryGetValue(assetRef.AssetGUID, out var handle))
            {
                return handle.Result as T;
            }

            var newHandle = assetRef.LoadAssetAsync<T>();
            await newHandle.ToUniTask();

            if (newHandle.Status == AsyncOperationStatus.Succeeded)
            {
                k_LoadedHandles[assetRef.AssetGUID] = newHandle;
                return newHandle.Result;
            }
            else
            {
                Debug.LogError("Resource",$"Không load được: {assetRef.AssetGUID}");
                return null;
            }
        }

        public async UniTask<Object> SpawnAddressable(AssetReference assetRef, Transform parent = null)
        {
            var handle = assetRef.InstantiateAsync(parent);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                k_ResourceCache[assetRef.AssetGUID] = handle.Result;
                return handle.Result;
            }
            else
            {
                Debug.LogError("Resource",$"Không instantiate được: {assetRef.AssetGUID}");
                return null;
            }
        }

        public async UniTask<Object> SpawnAddressable(string address, Transform parent = null)
        {
            var handle = Addressables.InstantiateAsync(address, parent);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var instance = handle.Result;
                k_InstanceHandles[instance] = handle;

                if (!k_ReferenceCount.ContainsKey(address))
                    k_ReferenceCount[address] = 0;
                k_ReferenceCount[address]++;

                return instance;
            }

            Debug.LogError("Resource",$"Không instantiate được: {address}");
            return null;
        }

        public void UnloadAddressable(GameObject instance)
        {
            if (instance == null) return;

            if (!k_InstanceHandles.TryGetValue(instance, out var handle))
            {
                Debug.LogWarning("Resource",$"Instance {instance.name} không được quản lý bởi AddressableManager!");
                Destroy(instance); // fallback
                return;
            }

            Addressables.ReleaseInstance(handle); // <- ĐÚNG CÁCH
            k_InstanceHandles.Remove(instance);
        }

        public void UnloadAllByAddress(string address)
        {
            var instancesToRemove = new List<GameObject>();

            foreach (var kv in k_InstanceHandles)
            {
                var handle = kv.Value;
                if (handle.DebugName == address)
                {
                    Addressables.ReleaseInstance(handle);
                    instancesToRemove.Add(kv.Key);
                }
            }

            foreach (var obj in instancesToRemove)
                k_InstanceHandles.Remove(obj);

            k_ReferenceCount.Remove(address);
        }
#endif
    }
}