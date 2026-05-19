# 🛠️ Utility & Infrastructure Managers

This document covers the supplementary utility and infrastructure components that complete the OSK Framework.

---

## 1. 🔍 ObserverManager (Main.Observer)

Provides a lightweight, string-topic-based observer pattern for decoupling system interactions.

### API Reference
```csharp
// Delegate type for event listeners
public delegate void CallBackObserver(object data);

// Register a listener for a specific topic
public void Add(string topicName, CallBackObserver callbackObserver);

// Unregister a listener
public void Remove(string topicName, CallBackObserver callbackObserver);

// Dispatches a message payload to all subscribers of a topic
public void Notify<OData>(string topicName, OData Data);

// Dispatches a parameterless message (passes null to observers)
public void Notify(string topicName);

// Clears all topics and active listeners
public void RemoveAllListeners();
```

### Usage Example
```csharp
using UnityEngine;
using OSK;

public class ObserverExample : MonoBehaviour
{
    private void OnEnable()
    {
        // Listen to game over state updates
        Main.Observer.Add("OnGameOver", OnGameOver);
    }

    private void OnDisable()
    {
        // Clean up observer mapping
        Main.Observer.Remove("OnGameOver", OnGameOver);
    }

    private void OnGameOver(object data)
    {
        int score = (int)data;
        Debug.Log($"Game Over! Final Score: {score}");
    }

    public void TriggerGameOver(int finalScore)
    {
        // Notify all observers of the event
        Main.Observer.Notify("OnGameOver", finalScore);
    }
}
```

---

## 2. 🔀 CommandManager (Main.Command)

Implements the classic Command pattern using named undo/redo command stacks.

### API Reference
```csharp
// Base interface that all commands must implement
public interface ICommand
{
    void Execute();
    void Undo();
}

// Executes a command and adds it to the command history stack mapping
public void Create(string commandName, ICommand command);

// Undoes the top command in the stack mapping
public void Undo(string commandName);

// Undoes all commands in the stack mapping sequentially
public void UndoAll(string commandName);

// Re-executes the top command in the stack and pushes a copy back
public void Redo(string commandName);

// Re-executes all commands in the stack mapping
public void RedoAll(string commandName);

// Stack Management
public bool HasCommand(string commandName);
public Stack<ICommand> GetHistory(string commandName);
public void ClearHistory(string commandName);
public void ClearAllHistory();
```

### Usage Example
```csharp
using UnityEngine;
using OSK;

public class MoveUnitCommand : ICommand
{
    private Transform _target;
    private Vector3 _offset;

    public MoveUnitCommand(Transform target, Vector3 offset)
    {
        _target = target;
        _offset = offset;
    }

    public void Execute() => _target.position += _offset;
    public void Undo() => _target.position -= _offset;
}

public class CommandExample : MonoBehaviour
{
    [SerializeField] private Transform unit;

    public void MoveUnitUp()
    {
        // Executes the move command and adds it to the stack "CombatActions"
        Main.Command.Create("CombatActions", new MoveUnitCommand(unit, Vector3.up));
    }

    public void UndoMove()
    {
        // Pops and calls Undo() on the last action in the "CombatActions" stack
        Main.Command.Undo("CombatActions");
    }
}
```

---

## 3. 🎬 DirectorManager (Main.Director)

Handles scene loading operations using a builder pattern (`SceneLoadBuilder`) and manages loaded scene caches.

### API Reference
```csharp
// Scene Load Modes
public enum ELoadMode { Single, Additive, Reload }

// Core APIs
public SceneLoadBuilder LoadScene(params DataScene[] scenes);
public void UnloadScene(string sceneName, Action onComplete = null);
public void ReloadSceneForce(string sceneName); // Force unloads and reloads additive scenes
```

### Usage Example (Builder Pattern)
```csharp
using UnityEngine;
using OSK;

public class TransitionController : MonoBehaviour
{
    public void LoadGameScene()
    {
        // Define scenes to load
        DataScene baseScene = new DataScene { sceneName = "GamePlay_Base", loadMode = ELoadMode.Single };
        DataScene envScene = new DataScene { sceneName = "GamePlay_Environment", loadMode = ELoadMode.Additive };

        // Construct the loading sequence via builder pattern
        Main.Director.LoadScene(baseScene, envScene)
            .Async(true)                      // Set asynchronous loading
            .FakeDuration(2.0f)               // Force a minimum loading time for smooth transition UI
            .OnStart(() => {
                Main.UI.Open<LoadingView>();  // Open transition screen
            })
            .OnComplete(() => {
                Main.UI.Hide(Main.UI.Get<LoadingView>()); // Hide transition screen
            })
            .Build();                         // Execute the load operation
    }
}
```

---

## 4. 🗄️ ResourceManager (Main.Res)

Manages standard Resources directory asset loading and caches references to prevent garbage collection spikes.

### API Reference
```csharp
// Loads resources into cache. Optionally registers the prefab to PoolManager.
public T Load<T>(string path, bool usePool = false) where T : Object;

// Instantiates a copy of the resource. Optionally spawns via PoolManager.
public T Spawn<T>(string path, bool usePool = false) where T : Object;

// Releases asset references from cache and calls Resources.UnloadAsset() when references hit 0.
public void Unload(string path);
```

### Usage Example
```csharp
using UnityEngine;
using OSK;

public class ResourceLoader : MonoBehaviour
{
    private GameObject _enemyPrefab;

    private void Start()
    {
        // Load prefab. References are cached in the manager.
        _enemyPrefab = Main.Res.Load<GameObject>("Prefabs/SkeletonEnemy");
    }

    public void SpawnSkeleton()
    {
        // Spawns/instantiates the skeletal prefab using Resources cache lookup
        GameObject skel = Main.Res.Spawn<GameObject>("Prefabs/SkeletonEnemy", usePool: true);
    }

    private void OnDestroy()
    {
        // Release resources from cache when no longer needed
        Main.Res.Unload("Prefabs/SkeletonEnemy");
    }
}
```

---

## 🌐 5. WebRequestManager (Main.WebRequest)

A callback-driven wrapper around Unity's `UnityWebRequest` client, utilizing lazy coroutine utilities for execution.

### API Reference
```csharp
// Header Configs
public void AddDefaultHeader(string key, string value);
public void RemoveDefaultHeader(string key);

// HTTP Actions
public void DownloadCSV(string url, Action<string> callback);
public void Get(string url, Action<string> onSuccess, Action<string> onError);
public void Post(string url, WWWForm formData, Action<string> onSuccess, Action<string> onError);
public void PostJson(string url, string jsonData, Action<string> onSuccess, Action<string> onError);
public void Put(string url, string jsonData, Action<string> onSuccess, Action<string> onError);
public void Delete(string url, Action<string> onSuccess, Action<string> onError);
```

### Usage Example
```csharp
using UnityEngine;
using OSK;

public class NetworkExample : MonoBehaviour
{
    private void Start()
    {
        // Add default authentication token header
        Main.WebRequest.AddDefaultHeader("Authorization", "Bearer token_xyz");
    }

    public void FetchUserProfile()
    {
        string url = "https://api.mygame.com/player/profile";

        Main.WebRequest.Get(url, 
            onSuccess: (jsonResponse) => {
                Debug.Log($"Profile data retrieved: {jsonResponse}");
            },
            onError: (errorMsg) => {
                Debug.LogError($"Network request failed: {errorMsg}");
            }
        );
    }
}
```

---

## ⚙️ 6. GameConfigsManager (Main.Configs)

Manages initial boot settings (`ConfigInit` ScriptableObject) and application version verifications.

### API Reference
```csharp
// Initial boot config data
public ConfigInit Init { get; }

// App version string lookup (returns Application.version)
public string VersionApp { get; }

// Verifies if the app version has changed since the last run. Sets the new version key.
public void CheckVersion(Action onNewVersion);
```

---

## 👾 7. EntityManager (Main.Entity)

Provides central registration, querying, and lifecycle tracking of game entities mapping.

### API Reference
```csharp
// Entity Spawning & Registrations
public Entity Create(string name, int id = -1);
public Entity Create(IEntity entity, int id);
public Entity Create<T>(string name, int id = -1) where T : Component;

// Entity Query Lookups
public bool Has(int id);
public Entity Get(int id);
public Entity GetByID(int id);
public Entity GetEntityWith<T>() where T : Component;
public T GetComponentFromEntity<T>(string name) where T : Component;
public List<Entity> GetAll();

// Entity Deletion & Deregistrations
public void Destroy(int id);
public void Destroy(Entity entity);
public void Remove(Entity entity);
```

---

## 📄 8. DataSheetManager (Main.DataSheet)

Loads and indexes config grids imported from Excel sheets into preconfigured ScriptableObject containers.

### API Reference
```csharp
// Retrieve a sheet ScriptableObject by its class type
public T GetSheet<T>() where T : class;

// Retrieve a sheet ScriptableObject by asset name
public T GetByNameSO<T>(string nameSO) where T : BaseSheet;

// Retrieve the first data element inside a sheet container matching a class type
public TData GetData<TData>() where TData : BaseData;

// Retrieve an data entry by ID from a specific sheet container type
public TData GetDataByID<TSheet, TData>(int id = -1) where TSheet : BaseSheetContainer<TData> where TData : BaseData;

// Retrieve all data records inside a specific sheet container type
public List<TData> GetAllData<TSheet, TData>() where TSheet : BaseSheetContainer<TData> where TData : BaseData;
```

---

## ⌨️ 9. InputDeviceManager (Main.InputDevice)

Centralized input wrapper integrating Touch, Keyboard, and Gyro sensors.

### API Reference
```csharp
// Static callbacks for input changes
public static Action<string> OnActionDown;
public static Action<string> OnActionUp;

// Retrieve the active input action state class
public InputActionRuntime Get(string id);
```

### Usage Example
Query input states within your custom `IUpdate` loops:

```csharp
using UnityEngine;
using OSK;

public class PlayerMove : AutoRegisterMono, IUpdate
{
    public void Tick(float deltaTime)
    {
        // Retrieve movement axes mapping
        InputActionRuntime moveAction = Main.InputDevice.Get("Move");
        if (moveAction != null)
        {
            Vector2 inputDir = moveAction.Axis2D;
            transform.Translate(new Vector3(inputDir.x, 0, inputDir.y) * (5f * deltaTime));
        }

        // Query stateless button states
        InputActionRuntime jumpAction = Main.InputDevice.Get("Jump");
        if (jumpAction != null && jumpAction.IsDown)
        {
            // Execute jump action
            Debug.Log("Jump triggered!");
        }
    }
}
```
