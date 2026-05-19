# 💾 DataManager & PrefData (Persistence & Encryption Engine)

The OSK persistence system is divided into two primary subsystems:
1. **`DataManager` (Main.Data)**: A structured file/stream serializer supporting multiple formats (JSON, Binary File, XML, PlayerPrefs) and asynchronous operations via `UniTask`.
2. **`PrefData`**: A high-level, static, EasySave3-style key-value wrapper for local storage with optional dynamic AES-128 encryption.

---

## 🌟 Key Features

### 1. DataManager (`Main.Data`)
* **Multi-Format Serialization**: Synchronous and asynchronous read/write interfaces across four storage backends (`Json`, `File`, `Xml`, `PlayerPrefs`).
* **Global Encryption Toggle**: Toggle `isEncrypt` to secure JSON, XML, or raw Binary File streams.
* **Extensible Storage Mappings**: Register custom serialization backends at runtime using the `IFile` interface.
* **WebGL-Compatible Out-of-the-Box**: Automatically maps JSON, File, and XML operations to a dedicated WebGL storage system (`WebJsonSystem`) when running inside browser builds.

### 2. PrefData
* **Boxing-Free Primitives**: Dedicated memory-safe overloads for `int`, `float`, `bool`, and `string`.
* **Unity Structural Serialization**: Native support for storing `Vector2`, `Vector3`, `Quaternion`, and `Color`.
* **Collections Mapping**: Supports direct saving/loading of generic `List<T>` and `Dictionary<string, int>` types (with AOT-safety to prevent IL2CPP runtime issues on iOS, WebGL, or consoles).
* **Optional AES-128 Security**: Auto-encrypts key-value pairs using AES-128 symmetric cryptography to block local save hacking.

---

## 🛠️ API Reference

### DataManager (`Main.Data`)
```csharp
// Mapped Storage Backends
public enum SaveType { Json, File, Xml, PlayerPrefs }

// Global encryption state configuration
public bool isEncrypt;

// Core Synchronous Saving & Loading
public void Save<T>(SaveType type, string fileName, T data);
public T Load<T>(SaveType type, string fileName, T defaultValue = default);

// Core Asynchronous Saving & Loading (UniTask)
public UniTask SaveAsync<T>(SaveType type, string fileName, T data);
public UniTask<T> LoadAsync<T>(SaveType type, string fileName, T defaultValue = default);

// File Utilities
public bool Exists(SaveType type, string fileName);
public void Delete(SaveType type, string fileName);
public void WriteAllText(string fileName, string[] lines);

// Runtime Storage Backend Registration
public void Register(SaveType key, IFile impl);
public void Unregister(SaveType key);
```

### PrefData
```csharp
// Global Encryption Switch for Key-Value Pairs
public static bool IsEncrypt;

// Standard Save APIs (Auto-infers types)
public static void Save(string key, int value);
public static void Save(string key, float value);
public static void Save(string key, bool value);
public static void Save(string key, string value);
public static void Save(string key, Vector3 value);
public static void Save(string key, Color value);
public static void Save<TItem>(string key, List<TItem> list);
public static void Save<T>(string key, T value); // Generic object save

// Load APIs
public static T Load<T>(string key, T defaultValue = default);

// Maintenance Methods
public static bool HasKey(string key);
public static void DeleteKey(string key);
public static void DeleteAll();
public static void SaveAll(); // Forces PlayerPrefs to flush to disk
```

---

## 📖 Usage Examples

### 1. Saving Player Profiles via DataManager (JSON)
Save structured game progress to a JSON save file:

```csharp
using System;
using UnityEngine;
using OSK;

[Serializable]
public class ProfileData
{
    public string username;
    public int currentLevel;
    public int highscore;
}

public class SaveController : MonoBehaviour
{
    private const string ProfileFile = "profile_save.json";

    public void SaveProfile(string name, int level, int score)
    {
        ProfileData data = new ProfileData
        {
            username = name,
            currentLevel = level,
            highscore = score
        };

        // Enable global encryption flag if desired
        Main.Data.isEncrypt = true;

        // Saves to e.g. Application.persistentDataPath/profile_save.json
        Main.Data.Save(SaveType.Json, ProfileFile, data);
    }

    public ProfileData LoadProfile()
    {
        // Load, decrypt, and parse profile data. Returns defaults if missing.
        return Main.Data.Load<ProfileData>(SaveType.Json, ProfileFile, new ProfileData
        {
            username = "NewPlayer",
            currentLevel = 1,
            highscore = 0
        });
    }
}
```

### 2. Async Loading via DataManager (UniTask)
Load configuration databases asynchronously to prevent frame hitches:

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using OSK;

public class AsyncDataLoader : MonoBehaviour
{
    public async UniTask LoadLargeDataAsync()
    {
        Debug.Log("Loading dataset...");

        // Load data on a background thread mapping
        LargeDataset data = await Main.Data.LoadAsync<LargeDataset>(
            SaveType.File, 
            "huge_database.dat"
        );

        Debug.Log("Dataset loaded successfully!");
        ProcessData(data);
    }

    private void ProcessData(LargeDataset data) { /* ... */ }
}
```

### 3. Key-Value Storage using `PrefData`
Store simple runtime configurations or user settings with ease:

```csharp
using UnityEngine;
using OSK;
using System.Collections.Generic;

public class GameSettingsManager : MonoBehaviour
{
    private void Awake()
    {
        // Encrypt all settings entries in PlayerPrefs
        PrefData.IsEncrypt = true;
    }

    public void SaveUserPreferences()
    {
        // Primitive values
        PrefData.Save("VolumeBGM", 0.75f);
        PrefData.Save("IsMuted", false);
        PrefData.Save("PlayerName", "Jack Sparrow");

        // Unity Structures
        PrefData.Save("ThemeColor", Color.green);

        // Lists
        List<int> unlockedItems = new List<int> { 101, 204, 305 };
        PrefData.Save("UnlockedItemIDs", unlockedItems);

        // Flush values to disk
        PrefData.SaveAll();
    }

    public void LoadUserPreferences()
    {
        float bgmVolume = PrefData.Load<float>("VolumeBGM", 1.0f);
        bool isMuted = PrefData.Load<bool>("IsMuted", false);
        Color color = PrefData.Load<Color>("ThemeColor", Color.white);
        List<int> items = PrefData.Load<List<int>>("UnlockedItemIDs", new List<int>());

        Debug.Log($"Loaded: Vol={bgmVolume}, Color={color}, ItemsCount={items.Count}");
    }
}
```

---

## ⚡ Performance & Best Practices

1. **Avoid High-Frequency Writes**: Saving files to disk requires slow physical disk I/O. Never call `DataManager.Save` or `PrefData.Save` inside `Update()` loops. Commit saves only during loading screens, level endings, or when closing panels.
2. **Prefer `PrefData` for Config Settings**: Do not create a separate file save for simple configurations (e.g., graphics levels, audio volumes, mute toggles). Rely on `PrefData` since it utilizes RAM-based player registries.
3. **Toggle Encryption in Production Only**: Encryption adds computation overhead to serialization pipelines. Keep encryption disabled (`isEncrypt = false`) during debug iterations in the editor for easy file inspections, and set `isEncrypt = true` in production builds to protect against player exploits.
