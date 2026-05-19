# 🔊 SoundManager (High-Performance Audio Coordinator)

`SoundManager` controls background music (BGM), sound effects (SFX), atmospheric ambiance, and character voice lines. Integrated with `PoolManager` to reuse audio source objects and utilizing `DataManager` for automated preference saving, it handles smooth volume transitions, spatial audio blends, randomized pitch shifts, and BroAudio-style sequential/random clip playlists.

---

## 🌟 Key Features

* **Multi-Channel Categorization**: Separates audio logic into four distinct layers: `MUSIC`, `SFX`, `AMBIENCE`, and `VOICE`.
* **Integrated Persistence**: Save and load volume and mute settings automatically across game runs.
* **Volume and Toggle Control**: Set volume levels (0f to 1f) and enable/disable channels independently.
* **Complex Spatial Audio Support**: Seamlessly transitions between 2D flat stereo and 3D positional spatial audio.
* **Robust Fading Systems**: Fade volumes over time or fade out and stop clips using built-in tween systems.
* **Playlist & Multi-Clip Playback**: Play primary clips, cycle sequences, or select random variations from a predefined list.
* **Dynamic Audio Mixer Routing**: Routes specific sound types to their corresponding Unity AudioMixer groups.

---

## 🛠️ API Reference

### Volume & Toggle Control
```csharp
// Category State Controls (directly persistent)
public bool IsEnableMusic;
public bool IsEnableSoundSFX;
public bool IsEnableAmbience;
public bool IsEnableVoice;

// Category Volume Controls (normalized 0.0f to 1.0f)
public float MusicVolume;
public float SFXVolume;
public float AmbienceVolume;
public float VoiceVolume;

// Core Settings Sync Methods
public void SaveSettings();
public void LoadSettings();
public void SetVolumeForType(SoundType type, float volume);
public void SetSoundTypeEnabled(SoundType type, bool enabled);
```

### Sound Playback Methods
```csharp
// Primary playback entry using preconfigured ID
public AudioSource Play(
    string id, 
    VolumeFade volume = null, 
    float startTime = 0, 
    bool? loop = null,
    float delay = 0, 
    int priority = -1, 
    MinMaxFloat pitch = default,
    Transform target = null, 
    int minDistance = -1, 
    int maxDistance = -1
);

// Directly plays an raw AudioClip reference
public AudioSource PlayAudioClip(
    AudioClip clip, 
    SoundType soundType = SoundType.SFX, 
    VolumeFade volume = null,
    float startTime = 0, 
    bool loop = false, 
    float delay = 0, 
    int priority = 128, 
    MinMaxFloat pitch = null,
    Transform target = null, 
    int minDistance = 1, 
    int maxDistance = 500
);

// Advanced playback wrappers using the SoundSetup data structures
public AudioSource PlayID(SoundSetup soundSetup);
public AudioSource PlayClip(SoundSetup soundSetup);
```

### Fading & Stopping Audio
```csharp
// Instant termination calls
public void StopAll();
public void Stop(string id);
public void Stop(AudioClip clip);
public void Stop(SoundType type);

// Smooth Fade-out and stop
public void StopWithFade(string id, float fadeDuration = 0.5f);
public void StopWithFade(SoundType type, float fadeDuration = 0.5f);
public void StopAllWithFade(float fadeDuration = 0.5f);

// Category fade transition
public Tween FadeVolumeForType(SoundType type, float targetVolume, float duration);
```

---

## 📖 Usage Examples

### 1. Simple Sound Playback (2D Sound Effects)
Play a transient interface sound effect on click:

```csharp
using UnityEngine;
using OSK;

public class ClickButton : MonoBehaviour
{
    public void OnClick()
    {
        // Simple 2D play call by register configuration id
        Main.Sound.Play("SFX_ClickButton");
    }
}
```

### 2. 3D Spatial Audio & Randomized Pitches
Play an explosion sound in 3D space tracking a target transform, with randomized pitches to avoid repetitive sound fatigue:

```csharp
using UnityEngine;
using OSK;

public class Bomb : MonoBehaviour
{
    public void Explode()
    {
        // Setup pitch variation range between 0.85 and 1.15
        MinMaxFloat pitchRange = new MinMaxFloat(0.85f, 1.15f);

        // Spawn a 3D audio source positioned at the bomb's location
        Main.Sound.Play(
            id: "SFX_Explosion",
            volume: new VolumeFade(init: 0f, target: 1f, duration: 0.1f), // Quick fade-in
            startTime: 0f,
            loop: false,
            delay: 0f,
            priority: 64, // High priority
            pitch: pitchRange,
            target: transform, // Mapped in 3D space
            minDistance: 5,
            maxDistance: 100
        );
    }
}
```

### 3. Playing Background Music with Fades
Play background music that fades in when entering gameplay, and fade out the lobby music:

```csharp
using UnityEngine;
using OSK;

public class GameFlowController : MonoBehaviour
{
    public void StartGame()
    {
        // Fade out any currently playing MUSIC over 1.5 seconds
        Main.Sound.StopWithFade(SoundType.MUSIC, fadeDuration: 1.5f);

        // Start BGM with a smooth 2.0s fade-in
        VolumeFade bgmVolumeSettings = new VolumeFade(init: 0f, target: 0.8f, duration: 2.0f);
        
        Main.Sound.Play(
            id: "BGM_GameplayTheme",
            volume: bgmVolumeSettings,
            loop: true
        );
    }
}
```

### 4. Adjusting & Persisting Settings (Settings Panel)
A standard implementation for a settings slider updating BGM volume:

```csharp
using UnityEngine;
using UnityEngine.UI;
using OSK;

public class VolumeSettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle musicToggle;

    private void Start()
    {
        // Load configurations automatically from local cache
        Main.Sound.LoadSettings();

        // Initialize UI values
        musicSlider.value = Main.Sound.MusicVolume;
        musicToggle.isOn = Main.Sound.IsEnableMusic;

        // Bind update callbacks
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
    }

    private void OnMusicSliderChanged(float value)
    {
        // Set and synchronize active playback volume
        Main.Sound.SetVolumeForType(SoundType.MUSIC, value);
        Main.Sound.SaveSettings();
    }

    private void OnMusicToggleChanged(bool isEnabled)
    {
        // Toggle the entire category
        Main.Sound.SetSoundTypeEnabled(SoundType.MUSIC, isEnabled);
        Main.Sound.SaveSettings();
    }
}
```

---

## ⚡ Performance & Best Practices

1. **Leverage Audio Pooling**: `SoundManager` spawns its playing sources from `PoolManager` (`KEY_POOL.KEY_AUDIO_SOUND`). You don't need to create your own `AudioSource` components.
2. **Utilize Multi-Clip Assets (BroAudio Style)**: Configure multiple audio clips under a single `SoundData` ID in the configurations script. You can set the `PlaybackMode` to `Sequence` (cycles clips) or `Random` (selects clip variations randomly) to add organic diversity.
3. **Always Clean Up with Fades**: If transition sequences are frequent, use `StopWithFade()` to avoid immediate snaps in BGM/Ambiance which breaks immersion.
4. **Volume Levels vs Audio Mixer**: For complex setups, route BGM, SFX, etc., to Unity `AudioMixerGroups` using the mixer parameters inside `SoundData`. Control parameters using `SetMixerVolume()` or `SetMixerGroupVolume()`.
