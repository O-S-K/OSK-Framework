# 🔀 ProcedureManager (Game Flow Finite State Machine)

`ProcedureManager` manages high-level game states and flow transitions using a Finite State Machine (FSM) pattern. It decouples separate game sequences (e.g. initialization sequences, splash screens, main menu lobbies, loading phases, and primary gameplay loops) into isolated blocks called **Procedures**.

---

## 🌟 Key Features

* **Finite State Machine Architecture**: Ensures the game executes in a single, predictable state, preventing conflicting systems from running simultaneously.
* **Isolated State Nodes (`ProcedureNode`)**: Encapsulates state-specific logic (e.g. loading screens, lobby interactions, player spawning) inside standalone script components.
* **Complete Lifecycle Pipeline**: Provides granular state lifecycle updates including initialization, enters, ticks (update, fixed, late), exits, and cleanup commands.
* **Pause and Time Scale Controls**: Pause or speed-scale specific state machines without affecting global Unity time systems.
* **Extensible State Management**: Register, query, or remove procedure states dynamically at runtime.

---

## 🛠️ API Reference

### ProcedureManager (Main.Procedure)
```csharp
// Current State Properties
public ProcedureNode CurrentProcedureNode { get; }
public int ProcedureNodeCount { get; }

// State Manipulation
public void AddProcedureNodes(params ProcedureNode[] nodes);
public void StartProcedure<T>() where T : ProcedureNode;
public void RunProcedureNode<T>() where T : ProcedureNode;
public void RunProcedureNode(Type type);

// State Querying
public bool HasProcedureNode<T>() where T : ProcedureNode;
public bool HasProcedureNode(Type type);
public bool PeekProcedureNode<T>(out T node) where T : ProcedureNode;
public bool PeekProcedureNode(Type type, out ProcedureNode node);

// State Removal & Cleanups
public void RemoveProcedureNodes(params Type[] types);
public void RemoveProcedureNode<T>() where T : ProcedureNode;
public void RemoveProcedureNode(Type type);

// Pause & Control
public void SetPause(bool pause);
public void SetTimeScale(float timeScale);
```

### ProcedureNode Lifecycle Overrides
Custom states must inherit from `ProcedureNode` and implement these virtual hooks:
```csharp
// Called once when the state machine first initializes this node.
public virtual void OnInit(ProcedureProcessor processor);

// Called once when transitioning into this procedure state.
public virtual void OnEnter(ProcedureProcessor processor);

// Ticked every frame while this state is active.
public virtual void OnUpdate(ProcedureProcessor processor);

// Ticked at fixed physics steps while this state is active.
public virtual void OnFixedUpdate(ProcedureProcessor processor);

// Ticked at the end of the frame while this state is active.
public virtual void OnLateUpdate(ProcedureProcessor processor);

// Called once when transitioning away from this state.
public virtual void OnExit(ProcedureProcessor processor);

// Called when this state node is removed from the manager.
public virtual void OnRemove(ProcedureProcessor processor);
```

### Transition Call (Inside `ProcedureNode`)
Use this call within overrides to transition to another procedure:
```csharp
// Requests a transition to a new state of type T
protected void ChangeState<T>(ProcedureProcessor processor) where T : ProcedureNode;
protected void ChangeState(ProcedureProcessor processor, Type stateType);
```

---

## 📖 Usage Examples

### 1. Creating Custom Procedure Nodes

#### **ProcedureInit (Initialization State)**
Ensures databases are initialized and assets preloaded before moving to the main menu:

```csharp
using UnityEngine;
using OSK;

public class ProcedureInit : ProcedureNode
{
    public override void OnInit(ProcedureProcessor processor)
    {
        Debug.Log("Initializing game framework systems...");
    }

    public override void OnEnter(ProcedureProcessor processor)
    {
        // Load settings and configuration files
        Main.Sound.LoadSettings();
        Main.Localization.PreloadAssets();

        // Transition to Lobby state
        ChangeState<ProcedureLobby>(processor);
    }
}
```

#### **ProcedureLobby (Main Menu State)**
Manages the menu layout and UI events, transitioning to gameplay when the player clicks play:

```csharp
using UnityEngine;
using OSK;

public class ProcedureLobby : ProcedureNode
{
    public override void OnEnter(ProcedureProcessor processor)
    {
        // Open the lobby menu view
        Main.UI.Open<LobbyView>();

        // Play lobby ambiance
        Main.Sound.Play("BGM_LobbyTheme", loop: true);
    }

    public void OnClickStartGame(ProcedureProcessor processor)
    {
        // Trigger transition to the main gameplay loop
        ChangeState<ProcedureGameplay>(processor);
    }

    public override void OnExit(ProcedureProcessor processor)
    {
        // Clean up Lobby UI before entering gameplay
        Main.UI.Hide(Main.UI.Get<LobbyView>());
        
        // Stop lobby background music with a fade
        Main.Sound.StopWithFade(SoundType.MUSIC, fadeDuration: 1.0f);
    }
}
```

#### **ProcedureGameplay (Primary Combat/Gameplay Loop)**
Ticks active combat mechanics and player controllers:

```csharp
using UnityEngine;
using OSK;

public class ProcedureGameplay : ProcedureNode
{
    private PlayerController _player;

    public override void OnEnter(ProcedureProcessor processor)
    {
        // Open the primary HUD view
        Main.UI.Open<GameplayHUD>();

        // Instantiate or spawn the player entity
        _player = Instantiate(Resources.Load<PlayerController>("Prefabs/Player"));

        // Play battle background music
        Main.Sound.Play("BGM_BattleTheme", loop: true);
    }

    public override void OnUpdate(ProcedureProcessor processor)
    {
        // Read input and update game logic
        if (_player != null)
        {
            _player.UpdateMovement();
        }

        // Return to Lobby if player presses ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ChangeState<ProcedureLobby>(processor);
        }
    }

    public override void OnExit(ProcedureProcessor processor)
    {
        // Clean up active entities
        if (_player != null)
        {
            Destroy(_player.gameObject);
        }

        // Hide HUD
        Main.UI.HideAll();
    }
}
```

---

## ⚡ Performance & Best Practices

1. **Rigorous Cleanup in `OnExit`**: Always clean up objects spawned during the state's lifecycle (UI panels, sound instances, event subscriptions, instantiated prefabs) inside the `OnExit()` override. Failing to do so causes leaks across state changes.
2. **Never Call `ChangeState` Outside Lifecycle Hooks**: State transitions must be requested using the active `ProcedureProcessor` argument passed directly to lifecycle callbacks like `OnUpdate` or `OnEnter`.
3. **Decouple Managers via Procedures**: Avoid referencing other procedures directly in your gameplay scripts. Gameplay scripts should communicate state requirements to `ProcedureManager`, letting the active `ProcedureNode` coordinate the rest of the systems.
