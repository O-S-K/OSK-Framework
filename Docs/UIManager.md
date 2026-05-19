# 🎨 UIManager & UniversalBinder (UI System & MVVM Data Binding)

OSK UI is a multi-layered, performance-optimized, and data-bound interface management system. It features **canvas layout isolation** to prevent layout rebuild spikes, and an **MVVM data-binding engine** to bind Model/ViewModel data to UI targets with zero-reflection runtime overhead.

---

## 🌟 Key Features

### 1. Multi-Canvas Isolation
To minimize Unity's UI rebuild performance penalties, `RootUI` splits the hierarchy into five separate canvases, sorted by rendering order:
1. **Screen**: For full-screen UI views (Lobby, Settings, Shop).
2. **Popup**: Modal views layered on top of screens (Confirmations, Alerts).
3. **Notification**: Floating elements, notifications, and alerts.
4. **Overlay**: Persistent elements rendering above all popups (Loading indicators, Toast alerts).
5. **Lock**: Screen blocking overlay to prevent inputs during transitions.

### 2. UniversalBinder (MVVM Engine)
Provides declarative data binding via the Unity Inspector or through code:
* **OneWay Binding**: Listens to a `BindableProperty<T>` on the source and updates a target property/field/method on a UI Component when the source changes.
* **TwoWay Binding**: Binds a `BindableProperty<T>` to an interactive target (e.g. `TMP_InputField`, `Toggle`, `Slider`, `Dropdown`). Updates propagate in both directions.
* **Event Binding**: Listens to a target event (like `Button.onClick`), triggering a source method or invoking a `BindableTrigger` with optional parameter routing (None, constant values, or dynamic event parameters).

---

## 🛠️ API Reference (UIManager)

```csharp
// Opens a View from cache or preloaded resources. Auto-spawns if it doesn't exist.
public T Open<T>(object data = null, bool hidePrev = false) where T : View;

// Opens a View by instance reference.
public void Open(View view, object data = null, bool hidePrev = false);

// Queues a View to be opened sequentially after previous views in the queue close.
public void EnqueueView<T>(object data = null, bool hidePrev = false, Action<T> onOpened = null) where T : View;

// Hides/Closes a specific View instance.
public void Hide(View view);

// Closes the top View and re-opens the previous View in the navigation history.
public View OpenPrevious(bool hideCurrent = false);

// Locks/unlocks user input interactions globally.
public void LockInput(bool isLock);
```

---

## 📖 How to Setup Data Bindings (MVVM)

### Step 1: Declare a ViewModel
Create a component/class to hold game state data. Use `BindableProperty<T>` for values and `BindableTrigger` or methods for events.

```csharp
using UnityEngine;
using OSK;

public class ProfileViewModel : MonoBehaviour
{
    // Bindable properties (OneWay or TwoWay)
    public BindableProperty<string> PlayerName = new BindableProperty<string>("Hero");
    public BindableProperty<float> XPPercent = new BindableProperty<float>(0.5f);
    public BindableProperty<bool> NotificationToggle = new BindableProperty<bool>(true);

    // Bindable trigger (Stateless signals - click actions, alerts, or SFX)
    public BindableTrigger LevelUpTrigger = new BindableTrigger();

    public void OnClickUpgrade()
    {
        XPPercent.Value += 0.1f;
        if (XPPercent.Value >= 1.0f)
        {
            XPPercent.Value = 0f;
            LevelUpTrigger.Trigger(); // Triggers target action directly
        }
    }
}
```

### Step 2: Bind in the Inspector
Add a `UniversalBinder` component to a UI GameObject. The Inspector UI is dynamically organized:
1. **Binding Mode**: Select `OneWay`, `TwoWay`, or `Event`.
2. **Source Configuration**:
   * **Resolution Mode**:
     * `AssignManual`: Drag-and-drop the source script instance manually.
     * `FindInScene`: Search for the source by type name when the view initializes.
     * `SearchInParent`: Search upwards in parent GameObjects to resolve the source.
   * **Source Type Name**: If using `FindInScene` or `SearchInParent`, type the type name (e.g. `ProfileViewModel`). Once recognized, the fields below populate.
   * **Source Property / Trigger**: Dropdown menu that automatically filters and displays only compatible fields/properties (e.g., `PlayerName`, `XPPercent`).
3. **Target Configuration**:
   * **Target GameObject / Component**: Assign the target component (e.g. `TMP_Text`, `Slider`, `Button`).
   * **Target Property / Method / Event**: Dropdown menu showing compatible target members (e.g. `text`, `value`, `onClick`).

---

## 💡 Q&A: Handling Late-Initialized Views & Stateless Signals

### Q1: Can I bind views that are initialized later than their models?
**Yes!** If you have data in your scene but spawn the UI view later, you have three options to establish the binding:
1. **`FindInScene` Resolution Mode**: Set the binder's Resolution Mode to `FindInScene` and enter the model's type in `Source Type Name`. When the view is instantiated, its `Awake()` method automatically locates the model in the scene and establishes the connection.
2. **`SearchInParent` Resolution Mode**: If your view is spawned as a child of a UI container that holds the model reference, set the Resolution Mode to `SearchInParent`. It will scan up the hierarchy to find the model automatically.
3. **Programmatic Injection (`SetSource`)**: View components can dynamically inject models into child binders using `SetSource()`:

```csharp
using UnityEngine;
using OSK;

public class ProfileCardView : View
{
    public void SetProfileData(ProfileViewModel viewModel)
    {
        // Grab all child binders and inject the newly spawned model source
        var binders = GetComponentsInChildren<UniversalBinder>(true);
        foreach (var binder in binders)
        {
            binder.SetSource(viewModel);
        }
    }
}
```

### Q2: Does triggering a view event require toggling a boolean?
**No!** You do not need to create boolean toggles that you set to `true` and then manually reset to `false`. Instead, use **`BindableTrigger`** or **`BindableTrigger<T>`**:
* They represent **stateless, one-off signals** (such as triggering level-up animations, playing transient sound effects, or displaying popups).
* Subscribe to a trigger using a one-way binding in `UniversalBinder`. When you call `LevelUpTrigger.Trigger()` on your ViewModel, the target transition or method fires immediately without modifying persistent state.

---

## ⚡ Performance & Best Practices

1. **Zero-Reflection Runtime**: `UniversalBinder` caches field, property, and method metadata during `Awake()`. Subsequent updates use direct delegate invocations, avoiding performance-heavy reflection calls.
2. **Targeted Canvas Rebuilds**: Isolate high-frequency UI updates (like health bars, timers, or coordinate maps) in their own sub-canvas layer to avoid redrawing the entire screen.
3. **Unbind Safely**: `UniversalBinder` automatically cleans up event listeners when the view is deactivated or destroyed to prevent memory leaks.
