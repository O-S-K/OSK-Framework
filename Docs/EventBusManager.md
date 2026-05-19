# ✉️ EventBusManager (Global Decoupled Event System)

`EventBusManager` is the central communications hub of the OSK Framework. By implementing the publisher-subscriber pattern with strict type safety, it allows modules, gameplay systems, and UI scripts to communicate without holding direct references to each other. It supports both synchronous and asynchronous (`UniTask`-based) subscribers, as well as automatic caching of the last fired events to accommodate late-joining observers.

---

## 🌟 Key Features

* **Strict Type Safety**: All event payloads must inherit from the `GameEvent` base class, preventing typos and runtime string matching bugs.
* **Synchronous & Asynchronous Support**: Listeners can bind standard synchronous callbacks (`Action<T>`) or asynchronous tasks (`Func<T, UniTask>`) to execute complex sequential logic.
* **Late Subscriber Catch-up**: Observers can request the last cached instance of an event type immediately upon subscription if it exists (`receiveLastIfExists = true`).
* **Safe Asynchronous Execution**: Built-in exception handling for async subscribers prevents a failing listener from breaking the event broadcast chain.
* **GC-Friendly Structuring**: Events carry automatic UTC timestamps (`TimeStamp`) to help track lifecycle durations.

---

## 🛠️ API Reference

### Event Base Class
```csharp
public abstract class GameEvent
{
    // UTC Timestamp set automatically on event creation
    public DateTime TimeStamp { get; } = DateTime.UtcNow;
}
```

### Subscribing & Unsubscribing
```csharp
// Synchronous subscription
public void Subscribe<T>(Action<T> callback, bool receiveLastIfExists = false) where T : GameEvent;

// Synchronous unsubscription
public void Unsubscribe<T>(Action<T> callback) where T : GameEvent;

// Asynchronous subscription (binds a task-returning method)
public void SubscribeAsync<T>(Func<T, UniTask> callback, bool receiveLastIfExists = false) where T : GameEvent;

// Asynchronous unsubscription
public void UnsubscribeAsync<T>(Func<T, UniTask> callback) where T : GameEvent;
```

### Event Publishing
```csharp
// Publishes an event payload. Fires synchronous subscribers first, and launches async subscribers in fire-and-forget mode.
public void Publish<T>(T gameEvent) where T : GameEvent;

// Publishes an event payload and awaits all asynchronous subscribers sequentially.
public async UniTask PublishAsync<T>(T gameEvent) where T : GameEvent;
```

---

## 📖 Usage Examples

### 1. Defining custom Event Types
To define custom event types, subclass `GameEvent`:

```csharp
using OSK;

// Define custom event class containing data payload
public class GoldChangedEvent : GameEvent
{
    public int OldGold { get; }
    public int NewGold { get; }

    public GoldChangedEvent(int oldGold, int newGold)
    {
        OldGold = oldGold;
        NewGold = newGold;
    }
}
```

### 2. Publishing Events
Publish game events from core controllers when state transitions occur:

```csharp
using UnityEngine;
using OSK;

public class PlayerInventory : MonoBehaviour
{
    private int _gold = 100;

    public void AddGold(int amount)
    {
        int oldGold = _gold;
        _gold += amount;

        // Publish the event to notify UI and Achievements systems
        Main.Event.Publish(new GoldChangedEvent(oldGold, _gold));
    }
}
```

### 3. Subscribing to Events (Synchronous)
Attach UI scripts to display player gold changes:

```csharp
using UnityEngine;
using TMPro;
using OSK;

public class GoldHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text goldLabel;

    private void OnEnable()
    {
        // Subscribe to gold changes and request the last value if already fired
        Main.Event.Subscribe<GoldChangedEvent>(OnGoldChanged, receiveLastIfExists: true);
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks and missing reference exceptions
        Main.Event.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
    }

    private void OnGoldChanged(GoldChangedEvent data)
    {
        goldLabel.text = $"Gold: {data.NewGold}";
    }
}
```

### 4. Asynchronous Event Pipeline (Sequential Await)
Coordinate complex sequences (e.g. executing a battle result sequence where we wait for UI animations to finish before saving files):

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using OSK;

public class LevelEndEvent : GameEvent
{
    public bool IsVictory { get; }
    public LevelEndEvent(bool isVictory) => IsVictory = isVictory;
}

public class UIAnimationController : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to async events
        Main.Event.SubscribeAsync<LevelEndEvent>(OnLevelEndedAsync);
    }

    private void OnDisable()
    {
        Main.Event.UnsubscribeAsync<LevelEndEvent>(OnLevelEndedAsync);
    }

    private async UniTask OnLevelEndedAsync(LevelEndEvent data)
    {
        Debug.Log("Playing victory animation...");
        // Wait for a 2-second UI animation to complete
        await UniTask.Delay(System.TimeSpan.FromSeconds(2));
        Debug.Log("Animation finished!");
    }
}

public class GameStateManager : MonoBehaviour
{
    public async UniTask TriggerLevelEnd()
    {
        Debug.Log("Level ended! Notifying listeners...");

        // Awaits until all async subscribers finish their tasks sequentially
        await Main.Event.PublishAsync(new LevelEndEvent(true));

        Debug.Log("All animations finished! Proceeding to next level loading screens.");
    }
}
```

---

## ⚡ Performance & Best Practices

1. **Mandatory Unsubscription**: Always clean up event subscriptions inside `OnDisable()` or `OnDestroy()`. Neglecting to call `Unsubscribe()` maintains strong references to the listening components, preventing garbage collection and throwing exceptions when dead objects are triggered.
2. **Utilize `receiveLastIfExists` for Late Loading Views**: For persistent states (like Player Profile details or current level index), pass `receiveLastIfExists: true` on subscription. This retrieves the current value immediately without waiting for the next state change.
3. **Prefer Structs inside GameEvent wrapper (Optional)**: If you are building high-frequency triggers, keep payload class instances simple, or pass reusable subclasses containing values. Avoid publishing events inside `Update()` loops.
