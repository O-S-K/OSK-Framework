# ⏱️ MonoManager (Centralized Tick System)

`MonoManager` is the high-performance update loop coordinator of the OSK Framework. In Unity, having hundreds of individual `MonoBehaviour` scripts implementing native update methods (`Update`, `FixedUpdate`, `LateUpdate`) incurs heavy CPU overhead due to the C++ to C# engine boundary translation. `MonoManager` eliminates this overhead by running a single native Unity lifecycle loop and driving all C# tick updates manually via optimized interface calls.

---

## 🌟 Key Features

* **Zero Engine Boundary Overhead**: Consolidates all lifecycle updates into a single central `MonoBehaviour` driver.
* **Granular Tick Interfaces**: Direct implementations for frame-rate updates (`IUpdate`), physics updates (`IFixedUpdate`), and post-frame updates (`ILateUpdate`).
* **Auto-Registration**: Attribute-based (`[AutoRegisterUpdate]`) or base-class (`AutoRegisterMono`) automated lifecycle hookups.
* **Global Time & Physics Control**: Pause, slow down, or accelerate custom tick processes without affecting native Unity time systems.
* **Main Thread Dispatcher**: Marshal actions from background threads to execute safely in Unity's main thread loop.
* **Centralized Coroutine Host**: Spawns and kills coroutines from non-`MonoBehaviour` classes.

---

## 🛠️ API Reference

### Centralized Registration
```csharp
// Registers a class instance to receive lifecycle ticks (auto-detects IUpdate, IFixedUpdate, ILateUpdate).
public void Register(object obj);

// Unregisters a class instance to stop receiving ticks.
public void UnRegister(object obj);

// Clears all registered tick processes.
public void RemoveAllTickProcess();
```

### Time and Speed Configuration
```csharp
// Sets the custom game update speed factor (multiplies deltaTime/fixedDeltaTime passed to ticks).
public MonoManager SetSpeed(float speed = 1f);

// Sets the global Time.timeScale and synchronizes the framework's time systems.
public MonoManager SetTimeScale(float timeScale);

// Pauses or resumes the custom update loops (toggles update execution instantly).
public MonoManager SetPause(bool isPause);

// Getters for current states
public bool IsPause { get; }
public float TimeScale { get; }
public float SpeedGame { get; }
```

### Threading & Coroutines
```csharp
// Runs an Action on the main thread in the next frame update.
public void RunOnMainThreadImpl(Action action);

// Wraps an Action to guarantee execution on the main thread.
public Action ToMainThreadImpl(Action action);
public Action<T> ToMainThreadImpl<T>(Action<T> action);

// Centralized Coroutine execution (ideal for plain C# classes)
public Coroutine StartCoroutineImpl(IEnumerator routine);
public void StopCoroutineImpl(Coroutine routine);
public void StopCoroutineImpl(IEnumerator routine);
public void StopAllCoroutinesImpl();
```

---

## 📖 Usage Examples

### 1. Manual Lifecycle Ticking via Interfaces
To receive ticks, a class must implement `IUpdate`, `IFixedUpdate`, or `ILateUpdate` and register itself:

```csharp
using UnityEngine;
using OSK;

public class BulletMover : MonoBehaviour, IUpdate, IFixedUpdate
{
    private void OnEnable()
    {
        // Registers both Update and FixedUpdate ticks
        Main.Mono.Register(this);
    }

    private void OnDisable()
    {
        // Unregister to prevent memory leaks and missing references
        Main.Mono.UnRegister(this);
    }

    // Tick replaces Update
    public void Tick(float deltaTime)
    {
        transform.Translate(Vector3.forward * (5f * deltaTime));
    }

    // FixedTick replaces FixedUpdate
    public void FixedTick(float fixedDeltaTime)
    {
        // Physics logic goes here
    }
}
```

### 2. Streamlined Auto-Registration using `AutoRegisterMono`
If a component subclasses `AutoRegisterMono`, it is registered and unregistered in `OnEnable`/`OnDisable` automatically:

```csharp
using UnityEngine;
using OSK;

public class EnemyController : AutoRegisterMono, IUpdate
{
    public void Tick(float deltaTime)
    {
        // Automatically ticked, no manual Register/UnRegister calls required!
        Patrol(deltaTime);
    }

    private void Patrol(float deltaTime)
    {
        // Patrol pathing
    }
}
```

### 3. Attribute-Based Auto-Registration (`[AutoRegisterUpdate]`)
Decorate a standard `MonoBehaviour` with the `[AutoRegisterUpdate]` attribute to auto-detect and register it during initial setup:

```csharp
using UnityEngine;
using OSK;

[AutoRegisterUpdate]
public class AmbientRotator : MonoBehaviour, IUpdate
{
    public void Tick(float deltaTime)
    {
        transform.Rotate(Vector3.up, 30f * deltaTime);
    }
}
```

### 4. Running Code on the Main Thread from a Thread / UniTask
Ensure thread-unsafe Unity API calls run on the main thread:

```csharp
using System.Threading.Tasks;
using UnityEngine;
using OSK;

public class NetworkDataProcessor
{
    public void ProcessDataInBackground()
    {
        Task.Run(() =>
        {
            // Perform heavy computations on background thread...
            string status = "Calculation Complete";

            // Return to main thread to edit Unity game objects safely
            Main.Mono.RunOnMainThreadImpl(() =>
            {
                var label = GameObject.Find("StatusText").GetComponent<TMPro.TMP_Text>();
                label.text = status;
            });
        });
    }
}
```

---

## ⚡ Performance & Best Practices

1. **Unregister Obligation**: Always match a `Register(this)` with an `UnRegister(this)` inside `OnDisable()` or `OnDestroy()`. Leaving dead references in lists forces the manager to perform safety checks and risks memory leaks.
2. **Do Not Mix Native and Centralized Loops**: If a class implements `IUpdate`, avoid creating native Unity `Update()` callbacks in that same file to prevent double-execution confusion and unnecessary overhead.
3. **Use the Multiplying DeltaTime**: Rely on the `deltaTime` or `fixedDeltaTime` passed directly to the `Tick(float deltaTime)` parameters. These values are pre-multiplied by `SpeedGame`, ensuring your movement/physics respects the framework's custom time scaling and pause settings.
