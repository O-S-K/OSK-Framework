# 📓 BlackboardManager (Shared Context Data Store)

`BlackboardManager` provides a centralized system of named key-value databases called **Blackboards**. Primarily used to facilitate communication within AI decision pipelines (such as Behavior Trees or Utility AI) and decoupled state machines, Blackboards allow dynamic data storage, priorities, read-only locks, and observer patterns.

---

## 🌟 Key Features

* **Named Blackboard System**: Create, retrieve, and isolate data context structures for individual actors or subsystems (e.g. `Enemy_Guard_A`, `Mission_Quest_3`).
* **Value Overwrites with Priority**: Set values with customizable priorities (`priority` parameter). Lower-priority values cannot overwrite higher-priority ones.
* **Write Protection (Read-Only Locks)**: Lock variables using `isReadOnly` to prevent unexpected overwrites from other systems.
* **Dynamic Property Watchers (Subscription)**: Bind callback delegates that execute automatically whenever a specific key's value changes.
* **Global Persistence Syncing**: Bulk save or load all active blackboard values dynamically utilizing the framework's JSON storage system.

---

## 🛠️ API Reference

### BlackboardManager (Main.Blackboard)
```csharp
// Blackboard Lifecycle
public Blackboard Create(string name, BlackboardData data, GameObject pingObject);
public void Remove(string name);
public bool Has(string name);
public Blackboard Get(string name);
public void ClearAll();

// Bulk Storage Sync
public void SaveAll();
public void LoadAll();
```

### Blackboard Instance Operations
Retrieve a specific `Blackboard` instance to call these operations:
```csharp
// Key Setter (Supports values, priority levels, and write locks)
public void SetValue<T>(string key, T value, int priority = 0, bool isReadOnly = false);

// Key Getters
public T GetValue<T>(string key);
public bool TryGetValue<T>(string key, out T value);

// Key Inquiries
public bool HasKey(string key);
public int GetPriority(string key);
public bool IsReadOnly(string key);

// Key Deletion & Resetting
public void RemoveKey(string key);
public void Clear(); // Clears all except predefined read-only default values

// Observer Subscriptions
public void Subscribe(string key, Action callback);
public void Subscribe<T>(string key, Action<T> callback);
public void Unsubscribe(string key);
```

---

## 📖 Usage Examples

### 1. Creating and Accessing a Blackboard
Initialize a Blackboard for an AI agent using predefined `BlackboardData` (configured via a ScriptableObject) and register the agent's GameObject as the ping target:

```csharp
using UnityEngine;
using OSK;

public class GuardAgent : MonoBehaviour
{
    [SerializeField] private BlackboardData defaultData;
    private Blackboard _agentBlackboard;

    private void Start()
    {
        string blackboardName = $"Guard_{gameObject.GetInstanceID()}";

        // Create a dedicated blackboard for this Guard instance
        _agentBlackboard = Main.Blackboard.Create(blackboardName, defaultData, gameObject);

        // Populate initial values
        _agentBlackboard.SetValue("Health", 100);
        _agentBlackboard.SetValue("IsAlerted", false);
    }
}
```

### 2. Setting Values with Priority and Write Protection
Set health and alert values with custom priorities:

```csharp
using UnityEngine;
using OSK;

public class GuardCombat : MonoBehaviour
{
    private Blackboard _blackboard;

    private void Start()
    {
        string blackboardName = $"Guard_{gameObject.GetInstanceID()}";
        _blackboard = Main.Blackboard.Get(blackboardName);
    }

    public void OnDamageTaken(int damageAmount)
    {
        int currentHealth = _blackboard.GetValue<int>("Health");
        int newHealth = Mathf.Max(0, currentHealth - damageAmount);

        // Update health value
        _blackboard.SetValue("Health", newHealth);

        if (newHealth < 30)
        {
            // Enter alert status with HIGH priority (e.g. Priority 10)
            // Lower-priority attempts to change IsAlerted back to false will fail.
            _blackboard.SetValue("IsAlerted", true, priority: 10);
            
            // Mark combat stance as Read-Only to lock the state
            _blackboard.SetValue("CombatStance", "Flee", priority: 10, isReadOnly: true);
        }
    }
}
```

### 3. Watching Blackboard Key Changes
Subscribe to key changes to react to state updates:

```csharp
using UnityEngine;
using OSK;

public class GuardAnimationHelper : MonoBehaviour
{
    private Blackboard _blackboard;
    private Animator _animator;

    private void Start()
    {
        string blackboardName = $"Guard_{gameObject.GetInstanceID()}";
        _blackboard = Main.Blackboard.Get(blackboardName);
        _animator = GetComponent<Animator>();

        // Subscribe to IsAlerted changes
        _blackboard.Subscribe<bool>("IsAlerted", OnAlertedChanged);
    }

    private void OnDestroy()
    {
        if (_blackboard != null)
        {
            // Unsubscribe on destroy to prevent memory leaks
            _blackboard.Unsubscribe("IsAlerted");
        }
    }

    private void OnAlertedChanged(bool isAlerted)
    {
        // Update animator parameters based on the new value
        _animator.SetBool("AlertStance", isAlerted);
    }
}
```

---

## ⚡ Performance & Best Practices

1. **Unsubscribe Watchers**: Always call `Unsubscribe(key)` inside the `OnDestroy()` or `OnDisable()` methods of components that subscribe to Blackboard events. Leaving dead callbacks can cause errors during scene cleanup.
2. **Utilize Priorities Strategically**: Assign high priorities (e.g., 10+) to critical gameplay-overriding states (like stun locks, critical health flee statuses, or cinematic mode triggers) to prevent standard AI behaviors from overwriting these settings.
3. **Use Namespace-Style Keys**: Keep keys organized across large teams by using structured naming patterns (e.g. `Enemy_Target`, `Enemy_PatrolPoint`, `System_IsMuted`).
