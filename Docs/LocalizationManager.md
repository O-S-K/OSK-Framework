# 🌐 LocalizationManager (Multi-Language Asset & Translation System)

`LocalizationManager` translates text assets and resolves localized asset resources (Sprites, AudioClips) dynamically. Swapping languages changes translation keys, swaps visual assets, and updates active UI labels instantly without reloading scenes.

---

## 🌟 Key Features

* **Localized Strings, Sprites, and Audio**: Localize textual text blocks, user interface sprites, and audio narrative voice-overs in a single interface.
* **On-the-Fly Swapping**: Switch language settings dynamically at runtime; all registered components automatically refresh.
* **Asset Preloading**: Preload localized resources into memory (`PreloadAssets()`) to prevent frame hitches during gameplay language swaps.
* **Declarative UI Components**: Attach helper scripts (`LocalizedText`, `LocalizedSprite`, `LocalizedAudio`) to elements for zero-code automatic translation updates.
* **Odin Inspector Editor Utility**: Bulk populate, find missing localized objects, or automatically generate translation keys from GameObject hierarchies.

---

## 🛠️ API Reference

### Configuration & Lookups
```csharp
// Returns the localized translation string for a given key.
public string GetKey(string key);

// Returns a formatted localized string matching parameter inputs.
public string GetKey(string key, params object[] args);

// Resolves a localized sprite asset loaded from language resource paths.
public TSprite GetSprite<TSprite>(string key) where TSprite : Sprite; // Shorthand GetSprite(key)

// Resolves a localized audio clip asset.
public AudioClip GetAudioClip(string key);

// Caches localized resource graphics and audio clips in advance.
public void PreloadAssets();
```

### Language Control
```csharp
// Sets the current language silently during initialization (does not broadcast update notifications).
public void SetLanguage(SystemLanguage languageCode);

// Switches the active language and triggers the global language update notification (KEY_OBSERVER.KEY_UPDATE_LANGUAGE).
public void SwitchLanguage(SystemLanguage language);

// Gets the active SystemLanguage.
public SystemLanguage GetCurrentLanguage();

// Gets an array of all languages available in the CSV localization sheet.
public SystemLanguage[] GetAllLanguages();
```

---

## 📖 Usage Examples

### 1. Translating Text in Code
Format messages dynamically with player names and numerical indexes:

```csharp
using UnityEngine;
using TMPro;
using OSK;

public class MissionPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text missionText;

    public void SetMissionProgress(string objectiveName, int current, int target)
    {
        // Key "MSG_MISSION_PROGRESS" translates to: "Objective {0}: Progress {1}/{2} completed."
        missionText.text = Main.Localization.GetKey("MSG_MISSION_PROGRESS", objectiveName, current, target);
    }
}
```

### 2. Swapping Languages in a Settings Panel
Instantly swap language profiles across the entire application:

```csharp
using UnityEngine;
using OSK;

public class LanguageSettings : MonoBehaviour
{
    public void SelectEnglish()
    {
        // Switches language and triggers global updates on all UI text/sprites
        Main.Localization.SwitchLanguage(SystemLanguage.English);
    }

    public void SelectVietnamese()
    {
        Main.Localization.SwitchLanguage(SystemLanguage.Vietnamese);
    }
}
```

### 3. Declarative Localizers (Zero-Code Translation)
Instead of writing manual scripts, attach localizer scripts directly to GameObjects in prefabs:

* **`LocalizedText`**: Attach this to standard `Text` or `TextMeshProUGUI` objects. Set the `Key` property (e.g. `LBL_START_GAME`) in the Inspector. It automatically hooks into `Main.Observer` and translates the label when the language switches.
* **`LocalizedSprite`**: Attach this to UI `Image` objects. It queries `GetSprite(key)` and updates the displayed graphic upon language swaps.
* **`LocalizedAudio`**: Attach this to an `AudioSource`. It resolves `GetAudioClip(key)` to translate character voice-overs instantly.

---

## 🛠️ Editor Localization Tools

To simplify localization management for large UI canvases, the OSK framework provides a bulk editor window:

1. Open the window via the menu path: **`OSK-Framework ➔ Localization ➔ Add LocalizedText`**.
2. **Bulk Operations**:
   * Select a parent GameObject in your hierarchy.
   * Click **Add LocalizedText to Children** to scan child transforms containing `Text` or `TMP_Text` components and automatically append a `LocalizedText` script if missing.
   * Check **Auto fill key by name rules** to auto-generate translation keys based on GameObject names (e.g., a GameObject named `Txt_StartButton` gets assigned key `TXT_STARTBUTTON`).
   * Click **Find Missing LocalizedText** to list UI components in the scene that are missing translation scripts.
3. Click **Show Localized Value** on any `LocalizedText` component's Inspector to preview how the text translates in other languages.

---

## ⚡ Performance & Best Practices

1. **Avoid Frequent CSV Lookups**: While string dictionaries are optimized, calling `GetKey()` inside `Update()` loops should be avoided. Use `LocalizedText` components or fetch translations once during state transitions.
2. **Preload Voice and Graphic Assets**: If your game contains large sets of translated audio dialogues or language-specific UI textures, call `Main.Localization.PreloadAssets()` during loading screens to load assets in advance.
3. **Establish Translation Key Conventions**: Adopt clear prefixes to organize your CSV sheets:
   * `BTN_...` for Buttons (e.g., `BTN_CONFIRM`).
   * `LBL_...` for static Labels (e.g., `LBL_SETTINGS_TITLE`).
   * `MSG_...` for dynamic formatted Messages (e.g., `MSG_LEVEL_FAILED`).
