# 📦 PoolManager (O(1) Object Pooling Engine)

`PoolManager` handles high-performance object caching and reuse to eliminate CPU-intensive `Instantiate` and `Destroy` calls. By maintaining reusable pools of GameObjects, Components, or generic Assets, it completely prevents runtime garbage collection (GC) spikes and frame-rate hitching.

---

## 🌟 Key Features

* **High-Performance Lookup**: Leverages dictionary-based $O(1)$ lookups to spawn and despawn resources.
* **Key-Based and Group-Based Management**: Retrieve pooled objects either by direct Prefab references inside groups, or by unique string identifier keys.
* **Component-Level Spawning**: Spawn components directly (e.g. `Bullet`) without doing slow manual `GetComponent()` lookups.
* **Initial Preload Modes**:
  * `Immediate`: Instantiates all objects during initialization.
  * `Spread`: Spreads instantiation over multiple frames to avoid loading-screen freezes.
  * `Lazy`: Instantiates instances on-demand when first requested.
* **Limit and Recycling Modes**:
  * `RecycleOldest`: Recycles the oldest active instance if the pool hits its maximum size (ideal for particle effects or indicators).
  * `RejectNew`: Rejects further spawn requests if the pool is full.
* **Lifecycle Callbacks**: Automatically invokes `IPoolable` interfaces on objects when they exit or return to the pool.

---

## 🛠️ API Reference

### Preloading & Expansion
```csharp
// Configures and initializes a pool using structural configuration data.
public void Preload(PoolItemData data);

// Manually expands an existing pool capacity.
public void ExpandPool(string groupName, Object prefab, int amount);
```

### Spawning (Retrieving from Pool)
```csharp
// Spawns using direct prefab references (creates the pool group if missing).
public T Spawn<T>(string groupName, T prefab, Transform parent = null, int maxSize = -1, LimitMode limit = LimitMode.RecycleOldest) where T : Object;

// Overloads for position, rotation, and parent anchors
public T Spawn<T>(string groupName, T prefab, Transform parent, Transform transform, int maxSize = -1, LimitMode limit = LimitMode.RecycleOldest) where T : Object;
public T Spawn<T>(string groupName, T prefab, Transform parent, Vector3 position, int maxSize = -1, LimitMode limit = LimitMode.RecycleOldest) where T : Object;
public T Spawn<T>(string groupName, T prefab, Transform parent, Vector3 position, Quaternion rotation, int maxSize = -1, LimitMode limit = LimitMode.RecycleOldest) where T : Object;

// Spawns using a unique pre-registered string key
public T SpawnByKey<T>(string key, Transform parent = null) where T : Object;

// Registers a runtime-created template/prefab with a key.
public bool RegisterFactory<T>(string key, Func<T> factory, string groupName = "Default") where T : Object;

// Registers the factory once if needed, then spawns with the normal pool flow.
public PoolBuilder<T> BuildOrCreate<T>(string key, Func<T> factory, string groupName = "Default") where T : Object;
```

### Despawning (Returning to Pool)
```csharp
// Returns an instance back to its pool (accepts GameObjects, Components, or transforms).
public void Despawn(Object instance);

// Returns an instance back to its pool after a delayed time.
public void Despawn(Object instance, float delay, bool unscaleTime = false);

// Despawns all active instances mapped to a key.
public void DespawnByKey(string key);
public void DespawnByKey(string key, float delay, bool unscaleTime = false);

// Despawns all active instances inside a specific group.
public void DespawnAllInGroup(string groupName);

// Despawns all active instances managed by PoolManager across all groups.
public void DespawnAllActive();
```

---

## 📖 Usage Examples

### 1. Implementing `IPoolable` on a Prefab
Attach this script to a projectile prefab. When spawned, it resets its physics states, fires, and schedules an auto-despawn:

```csharp
using UnityEngine;
using OSK;

public class Bullet : MonoBehaviour, IPoolable
{
    [SerializeField] private float speed = 20f;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // Called automatically when the object is pulled from the pool
    public void OnSpawn()
    {
        _rb.velocity = transform.forward * speed;
        
        // Auto-despawn back to pool after 3 seconds
        Main.Pool.Despawn(this, 3f);
    }

    // Called automatically when the object returns to the pool
    public void OnDespawn()
    {
        // Always reset physics velocities and states to avoid carrying over velocities
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }
}
```

### 2. Spawning with Prefabs (Group Mode)
Use the group-based system to spawn objects on-demand:

```csharp
using UnityEngine;
using OSK;

public class Weapon : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint;

    public void Fire()
    {
        // Spawns and triggers OnSpawn() automatically
        Bullet bulletInstance = Main.Pool.Spawn(
            groupName: "Projectiles",
            prefab: bulletPrefab,
            parent: null,
            position: firePoint.position,
            rotation: firePoint.rotation
        );
    }
}
```

### 3. Registering and Spawning by Key
You can register item data inside Unity's inspector config sheets and spawn objects solely using string keys:

```csharp
using UnityEngine;
using OSK;

public class CoinSpawner : MonoBehaviour
{
    private void Start()
    {
        // Dynamic registration example
        PoolItemData coinData = new PoolItemData
        {
            GroupName = "Pickups",
            Key = "GoldCoin",
            Prefab = Resources.Load<GameObject>("Prefabs/Coin"),
            Size = 20,
            MaxSize = 50,
            LoadMode = PreloadMode.Spread,
            LimitMode = LimitMode.RecycleOldest
        };
        
        Main.Pool.Preload(coinData);
    }

    public void SpawnCoinAt(Vector3 position)
    {
        // Spawn by unique key
        GameObject coin = Main.Pool.SpawnByKey<GameObject>("GoldCoin");
        if (coin != null)
        {
            coin.transform.position = position;
        }
    }
}
```

---

## ⚡ Performance & Best Practices

1. **Leverage Preloading**: Always pre-warm high-frequency pools (bullets, hit particles, floating damage popups) using `PreloadMode.Spread` or `Immediate` during scene loading.
2. **Never Call `Destroy()`**: Manually calling Unity's `Destroy()` on a pooled object breaks pool mapping tracking, leading to critical runtime errors inside `InstanceLookup`. Always use `Main.Pool.Despawn(instance)`.
3. **Reset Internal States in `OnDespawn`**: Reset local positions, particle system states, trail renderers, tweens, and event subscriptions within the `OnDespawn()` callback to ensure they start completely clean upon the next spawn.
4. **Prefer Component Spawning**: Directly request the target component via `Spawn<T>()` instead of spawning a `GameObject` and calling `GetComponent<T>()` which allocates memory.
