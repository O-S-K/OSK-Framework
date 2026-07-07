# 🌌 **OSK Framework**

OSK Framework is a lightweight, modular, and high-performance Unity architectural framework designed for professional game development. It features **zero runtime GC allocations** in core loops, an isolated **Multi-Canvas UI system**, and a modern **MVVM-like Universal Data Binding** engine.

---

## 🌟 **Key Features & Sub-Managers**

Click on any module to view its detailed documentation, API reference, code examples, and performance guidelines:

1. ⏱️ [**Centralized Tick System (MonoManager)**](Docs/MonoManager.md) — centralizes Update, FixedUpdate, and LateUpdate loops to eliminate native C++/C# boundaries.
2. 📦 [**Object Pooling (PoolManager)**](Docs/PoolManager.md) — high-performance $O(1)$ object and component recycling system with `IPoolable` callbacks.
3. ✉️ [**Event Bus System (EventBusManager)**](Docs/EventBusManager.md) — decoupled, type-safe global publish-subscribe event hub.
4. 🔊 [**Sound Manager (SoundManager)**](Docs/SoundManager.md) — handles BGM, SFX, UI audio, cross-fade transitions, and automatic volume persistence.
5. 🎨 [**UI Manager & UniversalBinder (UIManager)**](Docs/UIManager.md) — multi-canvas layout isolation (Screen, Popup, Notif, Overlay, Lock) coupled with a zero-reflection MVVM Data Binding engine.
6. 💾 [**Local Storage (DataManager)**](Docs/DataManager.md) — encrypted game save storage and optimized PlayerPrefs alternatives.
7. 🔀 [**Game FSM (ProcedureManager)**](Docs/ProcedureManager.md) — structured game loop flow states using finite state machine nodes.
8. 🌐 [**Localization System (LocalizationManager)**](Docs/LocalizationManager.md) — multi-language translation and dynamic language swapping.
9. 📓 [**Shared Blackboard (BlackboardManager)**](Docs/BlackboardManager.md) — key-value database for AI decision-making (Behavior Trees) and shared parameters.
10. 🛠️ [**Utility Managers (Command, Observer, Director, Resources...)**](Docs/OtherManagers.md) — auxiliary managers for scene transitions, Undo/Redo commands, resource caching, and input handling.

---

## 🚀 **Quick Start**

### **1. Prerequisites & Dependencies**
Ensure the following packages/assets are installed:
* **Odin Inspector & Serializer** ([Required for premium Editor UI](https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041))
* **DOTween** ([O-S-K Fork](https://github.com/O-S-K/DOTween))
* **Newtonsoft.Json** (`com.unity.nuget.newtonsoft-json` via Package Manager)
* **UniTask** ([High-performance async helper](https://github.com/Cysharp/UniTask))

### **2. Setup Framework**
Recommended first-time setup:
1. In the Unity Editor top menu, go to: **OSK-Framework -> Setup -> Quick Setup**.
2. Choose a preset:
   - `CoreOnly`: core loop, event, data, resource, config, procedure.
   - `UIBasedGame`: common game setup with UI, sound, pool, input, localization.
   - `FullFramework`: all OSK modules.
3. Click **Setup Current Scene**.
4. Select the generated `Main` object, review modules/config references, then press Play.
5. Use the **Generators** foldout to create first UI View, Save Data, Game Event, Procedure, Pool config entry, and Sound ID.
6. Open [Quick Use Guide](Docs/QuickUse.md) when you need the shortest module-by-module usage map.

Manual setup is still available:
1. Go to **OSK-Framework -> Create -> Framework**.
2. Select `Main` in the hierarchy.
3. Choose modules in **Main Modules** and click **Sync Modules (Hierarchy)**.

---

## ⚡ **Core Performance Guidelines**
* **Zero GC Spawning**: Avoid instantiating entities or particle effects. Recycle them using the [PoolManager](Docs/PoolManager.md).
* **Avoid Update Loops**: Centralize updates in game loops using the [Centralized Tick System](Docs/MonoManager.md) to save CPU instructions.
* **Layout Rebuild Isolation**: Keep dynamic UI elements inside independent canvases to limit canvas redrawing, as detailed in the [UIManager Guide](Docs/UIManager.md).

---

## 📞 **Support & Community**
* **Facebook Group**: [OSK Framework Community](https://www.facebook.com/xOskx/)
* **Email Contact**: gamecoding1999@gmail.com
