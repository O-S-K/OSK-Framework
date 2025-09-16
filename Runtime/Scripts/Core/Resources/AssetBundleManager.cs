using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public partial class ResourceManager
    {
        private readonly Dictionary<string, AssetBundle> k_AssetBundleCache = new();

        public IEnumerator LoadAssetFromBundle<T>(string bundlePath, string assetName, System.Action<T> onLoaded)
            where T : Object
        {
            if (!k_AssetBundleCache.TryGetValue(bundlePath, out var bundle))
            {
                var bundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);
                yield return bundleRequest;

                bundle = bundleRequest.assetBundle;
                if (bundle == null)
                {
                    OSK.Logg.LogError("Resource",$"[ResourceManager] Không load được AssetBundle: {bundlePath}");
                    onLoaded?.Invoke(null);
                    yield break;
                }

                k_AssetBundleCache[bundlePath] = bundle;
            }

            var assetRequest = bundle.LoadAssetAsync<T>(assetName);
            yield return assetRequest;

            T asset = assetRequest.asset as T;
            if (asset != null)
            {
                string cacheKey = $"{bundlePath}/{assetName}";
                k_ResourceCache[cacheKey] = asset;
                k_ReferenceCount[cacheKey] = 1;
            }

            onLoaded?.Invoke(asset);
        }

        public void UnloadAssetBundle(string bundlePath, bool unloadAllLoadedObjects = false)
        {
            if (!k_AssetBundleCache.TryGetValue(bundlePath, out var bundle)) return;

            bundle.Unload(unloadAllLoadedObjects);
            k_AssetBundleCache.Remove(bundlePath);
        }
    }
}