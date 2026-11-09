using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace OSK
{
    public partial class SoundManager : GameFrameworkComponent, IUpdateable
    {
        [ReadOnly, SerializeField] private List<SoundData> _listSoundData = new List<SoundData>();
        [ReadOnly, SerializeField] private List<PlayingSound> _listSoundPlayings = new List<PlayingSound>();
        [ReadOnly, SerializeField] private List<SoundData> _pendingMusic = new List<SoundData>();
        private readonly Dictionary<string, Tween> _playingTweens = new Dictionary<string, Tween>();

        private Dictionary<string, SoundData> _soundDataById;
        private Dictionary<AudioClip, SoundData> _soundDataByClip;
        private readonly Stack<PlayingSound> _playingSoundPool = new Stack<PlayingSound>(16);

        public List<SoundData> ListSoundData => _listSoundData;
        public List<PlayingSound> ListSoundPlayings => _listSoundPlayings;
        public List<SoundData> PendingMusic => _pendingMusic;
        public Dictionary<string, Tween> PlayingTweens => _playingTweens;

        [InfoBox("⚠️ Use maxCapacityMusic / maxCapacitySoundEffects to limit the number of sounds playing.")]
        [SerializeField]
        private int maxCapacityMusic = 5;

        [SerializeField] private int maxCapacitySoundEffects = 10;

        [InfoBox("⚠️ Use IsEnable* OR Volume fields to control each sound type.")]
        public bool IsEnableMusic = true;

        public bool IsEnableSoundSFX = true;
        public bool IsEnableAmbience = true;
        public bool IsEnableVoice = true;

        public float MusicVolume = 1f;
        public float SFXVolume = 1f;
        public float AmbienceVolume = 1f;
        public float VoiceVolume = 1f;

        private Transform _parentGroup;
        private AudioSource _templateSource;
        private bool _pauseWhenInBackground;

        //─────────────────────────────────────────────────────────────

        #region Init

        public override void OnInit()
        {
            var soundList = Main.Instance.configInit?.data?.listSoundSo;
            if (soundList == null)
            {
                soundList = Resources.Load<ListSoundSO>("Data/ListSoundSO");
                if (soundList != null && Main.Instance.configInit?.data != null)
                {
                    Main.Instance.configInit.data.listSoundSo = soundList;
                }
            }

            if (soundList == null)
            {
                MyLogger.LogError("SoundSO is missing in ConfigInit.");
            }

            _listSoundData = soundList != null ? (soundList.ListSoundInfos ?? new List<SoundData>()) : new List<SoundData>();
            if (soundList != null && _listSoundData.Count == 0)
            {
                MyLogger.LogWarning("Sound data list is empty.");
            }

            RebuildLookupCaches();

            if (_templateSource == null)
            {
                _templateSource = new GameObject("AudioSource_Template").AddComponent<AudioSource>();
                _templateSource.transform.parent = transform;
            }
            _listSoundPlayings.Clear();

            if (soundList != null)
            {
                maxCapacityMusic = soundList.maxCapacityMusic;
                maxCapacitySoundEffects = soundList.maxCapacitySFX;
            }

            LoadSettings();
        }

        public void SaveSettings()
        {
            Main.Data.Save<float>(SaveType.PlayerPrefs, KEY_SAVE.KEY_MUSIC_VOLUME, MusicVolume);
            Main.Data.Save<float>(SaveType.PlayerPrefs, KEY_SAVE.KEY_SFX_VOLUME, SFXVolume);
            Main.Data.Save<float>(SaveType.PlayerPrefs, KEY_SAVE.KEY_AMBIENCE_VOLUME, AmbienceVolume);
            Main.Data.Save<float>(SaveType.PlayerPrefs, KEY_SAVE.KEY_VOICE_VOLUME, VoiceVolume);

            Main.Data.Save<int>(SaveType.PlayerPrefs, KEY_SAVE.KEY_MUSIC_ENABLED, IsEnableMusic ? 1 : 0);
            Main.Data.Save<int>(SaveType.PlayerPrefs, KEY_SAVE.KEY_SFX_ENABLED, IsEnableSoundSFX ? 1 : 0);
            Main.Data.Save<int>(SaveType.PlayerPrefs, KEY_SAVE.KEY_AMBIENCE_ENABLED, IsEnableAmbience ? 1 : 0);
            Main.Data.Save<int>(SaveType.PlayerPrefs, KEY_SAVE.KEY_VOICE_ENABLED, IsEnableVoice ? 1 : 0);
        }

        public void LoadSettings()
        {
            MusicVolume =    Main.Data.Load<float>(SaveType.PlayerPrefs, KEY_SAVE.KEY_MUSIC_VOLUME, 1f);
            SFXVolume =      Main.Data.Load<float>(SaveType.PlayerPrefs, KEY_SAVE.KEY_SFX_VOLUME, 1f);
            AmbienceVolume = Main.Data.Load<float>(SaveType.PlayerPrefs, KEY_SAVE.KEY_AMBIENCE_VOLUME, 1f);
            VoiceVolume =    Main.Data.Load<float>(SaveType.PlayerPrefs, KEY_SAVE.KEY_VOICE_VOLUME, 1f);

            IsEnableMusic =    Main.Data.Load<int>(SaveType.PlayerPrefs, KEY_SAVE.KEY_MUSIC_ENABLED, 1) == 1;
            IsEnableSoundSFX = Main.Data.Load<int>(SaveType.PlayerPrefs, KEY_SAVE.KEY_SFX_ENABLED, 1) == 1;
            IsEnableAmbience = Main.Data.Load<int>(SaveType.PlayerPrefs, KEY_SAVE.KEY_AMBIENCE_ENABLED, 1) == 1;
            IsEnableVoice =    Main.Data.Load<int>(SaveType.PlayerPrefs, KEY_SAVE.KEY_VOICE_ENABLED, 1) == 1;

            SyncAllVolumes();
        }

        public void RebuildLookupCaches()
        {
            _soundDataById = new Dictionary<string, SoundData>(_listSoundData.Count);
            _soundDataByClip = new Dictionary<AudioClip, SoundData>(_listSoundData.Count);

            for (int i = 0; i < _listSoundData.Count; i++)
            {
                var data = _listSoundData[i];
                if (!string.IsNullOrEmpty(data.id) && !_soundDataById.ContainsKey(data.id))
                    _soundDataById[data.id] = data;
                if (data.audioClip != null && !_soundDataByClip.ContainsKey(data.audioClip))
                    _soundDataByClip[data.audioClip] = data;
            }
        }

#if UNITY_EDITOR
        private void OnApplicationPause(bool pause) => _pauseWhenInBackground = pause;
#endif

        public void OnUpdate() => CleanupStoppedSounds();

        private void CleanupStoppedSounds()
        {
#if UNITY_EDITOR
            if (_pauseWhenInBackground) return;
#endif
            for (int i = _listSoundPlayings.Count - 1; i >= 0; i--)
            {
                var p = _listSoundPlayings[i];
                if (p.AudioSource == null || (!p.AudioSource.isPlaying && !p.IsPaused))
                {
                    p.KillTween();
                    Main.Pool.Despawn(p.AudioSource);
                    _listSoundPlayings.RemoveAt(i);
                    ReturnPlayingSound(p);
                }
            }
        }

        #endregion

        //─────────────────────────────────────────────────────────────

        #region Helpers

        /// <summary>
        /// Get the volume multiplier for a given SoundType.
        /// </summary>
        public float GetVolumeMultiplier(SoundType type)
        {
            switch (type)
            {
                case SoundType.MUSIC: return MusicVolume;
                case SoundType.SFX: return SFXVolume;
                case SoundType.AMBIENCE: return AmbienceVolume;
                case SoundType.VOICE: return VoiceVolume;
                default: return 1f;
            }
        }

        /// <summary>
        /// Check if a SoundType is enabled.
        /// </summary>
        public bool IsSoundTypeEnabled(SoundType type)
        {
            switch (type)
            {
                case SoundType.MUSIC: return IsEnableMusic;
                case SoundType.SFX: return IsEnableSoundSFX;
                case SoundType.AMBIENCE: return IsEnableAmbience;
                case SoundType.VOICE: return IsEnableVoice;
                default: return true;
            }
        }

        /// <summary>
        /// Set the enable state for a SoundType.
        /// </summary>
        public void SetSoundTypeEnabled(SoundType type, bool enabled)
        {
            switch (type)
            {
                case SoundType.MUSIC: IsEnableMusic = enabled; break;
                case SoundType.SFX: IsEnableSoundSFX = enabled; break;
                case SoundType.AMBIENCE: IsEnableAmbience = enabled; break;
                case SoundType.VOICE: IsEnableVoice = enabled; break;
            }
        }

        /// <summary>
        /// Set the volume for a SoundType.
        /// </summary>
        public void SetVolumeForType(SoundType type, float volume)
        {
            switch (type)
            {
                case SoundType.MUSIC: MusicVolume = volume; break;
                case SoundType.SFX: SFXVolume = volume; break;
                case SoundType.AMBIENCE: AmbienceVolume = volume; break;
                case SoundType.VOICE: VoiceVolume = volume; break;
            }

            SyncAllVolumes();
        }

        public void SyncAllVolumes()
        {
            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var p = _listSoundPlayings[i];
                if (p == null || p.AudioSource == null) continue;

                float mult = GetVolumeMultiplier(p.SoundData.type);
                if (p.VolumeTween == null || !p.VolumeTween.IsActive())
                {
                    p.AudioSource.volume = p.RawVolume * mult;
                }
            }
        }

        #endregion

        #region Play Entry

        public AudioSource Play(string id, VolumeFade volume = null, float startTime = 0, bool? loop = null,
            float delay = 0, int priority = -1, MinMaxFloat pitch = default,
            Transform target = null, int minDistance = -1, int maxDistance = -1)
        {
            var data = GetSoundInfo(id);
            if (data == null)
            {
                MyLogger.LogError($"[Sound] No sound info with id: {id}");
                return null;
            }

            bool actualLoop = loop ?? data.loop;
            int actualPriority = priority >= 0 ? priority : data.priority;
            int actualMinDist = minDistance >= 0 ? minDistance : data.minDistance;
            int actualMaxDist = maxDistance >= 0 ? maxDistance : data.maxDistance;

            return InternalPlay(data.GetNextClip(), data.type, volume, startTime, actualLoop, delay, actualPriority,
                pitch, target,
                actualMinDist, actualMaxDist);
        }

        public AudioSource PlayAudioClip(AudioClip clip, SoundType soundType = SoundType.SFX, VolumeFade volume = null,
            float startTime = 0, bool loop = false, float delay = 0, int priority = 128, MinMaxFloat pitch = null,
            Transform target = null, int minDistance = 1, int maxDistance = 500)
        {
            return InternalPlay(clip, soundType, volume, startTime, loop, delay, priority, pitch, target,
                minDistance, maxDistance);
        }

        //─────────────────────────────────────────────────────────────
        private AudioSource InternalPlay(AudioClip clip, SoundType type, VolumeFade volume, float startTime, bool loop,
            float delay, int priority, MinMaxFloat pitch, Transform target, int minDist, int maxDist)
        {
            if (clip == null)
            {
                MyLogger.LogError("[Sound] Missing AudioClip.");
                return null;
            }

            var data = GetSoundInfo(clip) ?? new SoundData
                { audioClip = clip, id = clip.name, type = type, volume = 1f };
            bool isMusic = type == SoundType.MUSIC || loop;
            if (!IsSoundTypeEnabled(type) && !(isMusic && IsEnableMusic))
            {
                if (isMusic)
                {
                    if (!_pendingMusic.Contains(data))
                        _pendingMusic.Add(data);
                }

                return null;
            }

            int count = 0;
            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                if (_listSoundPlayings[i].SoundData != null && _listSoundPlayings[i].SoundData.type == type)
                    count++;
            }

            int maxCap = (type == SoundType.MUSIC) ? maxCapacityMusic : maxCapacitySoundEffects;
            if (count >= maxCap)
                RemoveOldestSound(type);

            void PlayNow() => CreateAndPlayAudioSource(clip, data, startTime, volume, loop, priority, pitch, target,
                minDist, maxDist);

            if (delay > 0)
            {
                var tween = DOVirtual.DelayedCall(delay, PlayNow, false);
                _playingTweens[clip.name] = tween;
            }
            else PlayNow();

            // Return the last added AudioSource (the one we just created)
            int lastIdx = _listSoundPlayings.Count - 1;
            return lastIdx >= 0 ? _listSoundPlayings[lastIdx].AudioSource : null;
        }

        #endregion

        //─────────────────────────────────────────────────────────────

        #region Core Logic

        private AudioSource CreateAndPlayAudioSource(AudioClip clip, SoundData data, float startTime, VolumeFade volume,
            bool loop, int priority, MinMaxFloat pitch, Transform target, int minDist, int maxDist)
        {
            if (_templateSource == null)
            {
                _templateSource = new GameObject("AudioSource_Template").AddComponent<AudioSource>();
                _templateSource.transform.parent = transform;
            }

            var source = Main.Pool.Spawn(KEY_POOL.KEY_AUDIO_SOUND, _templateSource, _parentGroup);
            if (source == null)
            {
                MyLogger.LogError("[Sound] Failed to spawn AudioSource from Pool.");
                return null;
            }

            source.Stop();
            source.name = clip.name;
            source.clip = clip;
            source.loop = loop;

            if (data != null && data.mixerGroup != null)
            {
                source.outputAudioMixerGroup = data.mixerGroup;
            }

            pitch ??= new MinMaxFloat(1, 1);
            var playing = RentPlayingSound();
            playing.AudioSource = source;
            playing.SoundData = data;
            playing.RawVolume = data.volume;

            float volumeMult = GetVolumeMultiplier(data.type);
            volume ??= new VolumeFade(0, 1, 0);
            float targetVol = volume.target;

            if (volume.duration > 0)
            {
                playing.KillTween();
                playing.VolumeTween = DOVirtual.Float(volume.init, targetVol, volume.duration, v =>
                {
                    playing.RawVolume = v;
                    if (source != null)
                        source.volume = v * volumeMult;
                });
            }
            else
            {
                playing.RawVolume = targetVol;
                source.volume = targetVol * volumeMult;
            }

            source.pitch = pitch.RandomValue;
            source.priority = priority;
            if (startTime > 0) source.time = startTime;

            // Spatial setup – use SoundData spatial settings when no target Transform is provided
            if (target != null)
            {
                source.spatialBlend = 1;
                source.transform.position = target.position;
                source.minDistance = minDist;
                source.maxDistance = maxDist;
            }
            else
            {
                source.spatialBlend = data.spatialBlend;
                source.minDistance = data.spatialBlend > 0 ? data.minDistance : minDist;
                source.maxDistance = data.spatialBlend > 0 ? data.maxDistance : maxDist;
            }

            source.Play();
            _listSoundPlayings.Add(playing);
            return source;
        }

        private void RemoveOldestSound(SoundType type)
        {
            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var p = _listSoundPlayings[i];
                if (p.SoundData == null || p.SoundData.type != type) continue;

                if (p.AudioSource != null)
                {
                    p.AudioSource.Stop();
                    Main.Pool.Despawn(p.AudioSource);
                }

                p.KillTween();
                _listSoundPlayings.RemoveAt(i);
                ReturnPlayingSound(p);
                return;
            }
        }

        private IEnumerator DespawnAudioSource(AudioSource src, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (src != null)
                Main.Pool.Despawn(src);
        }

        #endregion

        //─────────────────────────────────────────────────────────────

        #region Lookup & Pool

        public SoundData GetSoundInfo(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_soundDataById != null && _soundDataById.TryGetValue(id, out var data))
                return data;
            for (int i = 0; i < _listSoundData.Count; i++)
            {
                if (_listSoundData[i].id == id) return _listSoundData[i];
            }

            return null;
        }

        public SoundData GetSoundInfo(AudioClip clip)
        {
            if (clip == null) return null;
            if (_soundDataByClip != null && _soundDataByClip.TryGetValue(clip, out var data))
                return data;
            for (int i = 0; i < _listSoundData.Count; i++)
            {
                if (_listSoundData[i].audioClip == clip) return _listSoundData[i];
            }

            return null;
        }

        public void SetParentGroup(Transform group, bool dontDestroy)
        {
            _parentGroup = group;
            if (dontDestroy)
            {
                if (!_parentGroup.TryGetComponent(out DontDestroy existing))
                    _parentGroup.gameObject.AddComponent<DontDestroy>().DontDesGOOnLoad();
            }
        }

        public void SetMixerVolume(AudioMixer mainMixer, string parameterName, float value)
        {
            if (mainMixer == null)
            {
                MyLogger.LogWarning("Main Mixer is not assigned in SoundManager.");
                return;
            }

            // Chuyển đổi từ 0.0001-1 sang -80dB đến 0dB
            // Công thức: dB = 20 * log10(Linear) 
            float dB = value > 0 ? Mathf.Log10(value) * 20 : -80f;
            mainMixer.SetFloat(parameterName, dB);
        }

        public void SetMixerGroupVolume(AudioMixerGroup mixerGroup, float value)
        {
            if (mixerGroup == null || mixerGroup.audioMixer == null)
            {
                MyLogger.LogWarning("Mixer Group or its AudioMixer is null.");
                return;
            }

            float dB = value > 0 ? Mathf.Log10(value) * 20 : -80f;
            mixerGroup.audioMixer.SetFloat(mixerGroup.name + "_Volume", dB);
        }

        private PlayingSound RentPlayingSound()
        {
            if (_playingSoundPool.Count > 0)
            {
                var ps = _playingSoundPool.Pop();
                ps.IsPaused = false;
                ps.RawVolume = 1f;
                return ps;
            }

            return new PlayingSound();
        }

        private void ReturnPlayingSound(PlayingSound ps)
        {
            if (ps == null) return;
            ps.Reset();
            _playingSoundPool.Push(ps);
        }

        #endregion
    }
}