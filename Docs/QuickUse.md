# OSK Quick Use Guide

Use this as the first map before reading the full module docs.

## First Setup

1. Open `OSK-Framework -> Setup -> Quick Setup`.
2. Choose `UI Game` for most projects.
3. Click `Setup Current Scene`.
4. Click `Sync Selected Modules` after changing module presets.
5. Use the `Generators` foldout to create common files.

## Main

Use when you need access to OSK modules from gameplay code.

Common APIs:

```csharp
Main.Inject(this);
Main.GetModule<UIManager>();
Main.SetPause(true);
```

Common issue:

`Module 'UIManager' is not ready.`

Fix:

Open `OSK-Framework -> Setup -> Quick Setup`, choose a preset that includes the module, then click `Sync Selected Modules`.

## UIManager

Use when opening screens, popups, overlays, alerts, and tutorials.

Setup needed:

- `RootUI` in scene.
- `UIManager` enabled.
- View prefab added to `ListViewSO`.

Common APIs:

```csharp
Main.UI.Open<MyView>();
Main.UI.Open<MyView>(data);
Main.UI.Hide(Main.UI.Get<MyView>());
Main.UI.HideAll();
```

Common issue:

View does not open because the prefab was not added to `ListViewSO`.

## SoundManager

Use for music, SFX, ambience, and voice playback.

Setup needed:

- `SoundManager` enabled.
- AudioClip added to `ListSoundSO`.

Common APIs:

```csharp
Main.Sound.Play("ButtonClick");
Main.Sound.Play("BGM_Lobby", loop: true);
Main.Sound.StopWithFade(SoundType.MUSIC, 0.5f);
```

Common issue:

Sound ID not found. Add the selected AudioClip with `Generators -> Add Selected AudioClip To ListSoundSO`.

## PoolManager

Use for repeated objects such as bullets, enemies, VFX, floating text, and UI items.

Setup needed:

- `PoolManager` enabled.
- Prefab registered in `PoolConfig` or spawned manually.

Common APIs:

```csharp
GameObject enemy = Main.Pool.SpawnByKey<GameObject>("Enemy");
Main.Pool.Despawn(enemy);
Main.Pool.Despawn(enemy, delay: 2f);
```

Common issue:

Pool key not found. Select the prefab and use `Generators -> Add Selected Prefab To PoolConfig`.

## DataManager

Use for save files, player progress, settings, and local data.

Setup needed:

- `DataManager` enabled.

Common APIs:

```csharp
PlayerSaveData data = Main.Data.Load(SaveType.Json, "player_save_data.json", PlayerSaveData.Default());
data.gold += 10;
Main.Data.Save(SaveType.Json, "player_save_data.json", data);
```

Common issue:

Save class does not exist yet. Use `Generators -> Create Save Data Class`.

## EventBusManager

Use for typed gameplay events.

Setup needed:

- `EventBusManager` enabled.
- Event class inherits `GameEvent`.

Common APIs:

```csharp
Main.Event.Subscribe<GoldChangedEvent>(OnGoldChanged);
Main.Event.Publish(new GoldChangedEvent(10));
Main.Event.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
```

Common issue:

Forgetting to unsubscribe in `OnDisable`, causing callbacks to fire after the object is disabled.

## ProcedureManager

Use for high-level game flow: boot, login, menu, gameplay, result.

Setup needed:

- `ProcedureManager` enabled.
- Procedure class inherits `ProcedureNode`.

Common APIs:

```csharp
Main.Procedure.AddProcedureNodes(new BootProcedure());
Main.Procedure.StartProcedure<BootProcedure>();
```

Common issue:

Procedure exists but never starts. Add nodes first, then call `StartProcedure<T>()`.

## MonoManager

Use to centralize update logic instead of spreading many `Update()` methods.

Setup needed:

- `MonoManager` enabled.

Common APIs:

```csharp
Main.Mono.Register(ticker);
Main.Mono.UnRegister(ticker);
Main.Mono.SetPause(false);
```

Common issue:

Forgetting to unregister updateable objects when they are destroyed.
