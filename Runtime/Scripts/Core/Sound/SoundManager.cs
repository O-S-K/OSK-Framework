using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    public partial class SoundManager : GameFrameworkComponent
    {
        [ReadOnly, SerializeField] private List<SoundData> _listSoundData = new List<SoundData>();
        [ReadOnly, SerializeField] private List<PlayingSound> _listSoundPlayings = new List<PlayingSound>();
        [ReadOnly, SerializeField] private List<SoundData> _pendingMusic = new List<SoundData>();
        private readonly Dictionary<string, Tween> _playingTweens = new Dictionary<string, Tween>();
        
        public List<SoundData> ListSoundData { get { return _listSoundData; } }
        public List<PlayingSound> ListSoundPlayings { get { return _listSoundPlayings; } }
        public List<SoundData> PendingMusic { get { return _pendingMusic; } }
        public Dictionary<string, Tween> PlayingTweens { get { return _playingTweens; } }

        [InfoBox("⚠️ Use maxCapacityMusic / maxCapacitySoundEffects to limit the number of sounds playing.")]
        [SerializeField] private int maxCapacityMusic = 5;
        [SerializeField] private int maxCapacitySoundEffects = 10;

        [InfoBox("⚠️ Use IsEnableMusic / IsEnableSoundSFX OR MusicVolume / SFXVolume. Not both.")]
        public bool IsEnableMusic = true;
        public bool IsEnableSoundSFX = true;

        public float MusicVolume = 1f;
        public float SFXVolume = 1f;

        private Tweener _tweener;
        private Transform _parentGroup;
        private Transform _cameraTransform;
        private AudioSource _templateSource;
        private bool _pauseWhenInBackground;

        //─────────────────────────────────────────────────────────────
        #region Init

        public override void OnInit()
        {
            var soundList = Main.Instance.configInit.data.listSoundSo;
            if (soundList == null)
            {
                MyLogger.LogError("SoundSO is missing in ConfigInit.");
                return;
            }

            _listSoundData = soundList.ListSoundInfos ?? new List<SoundData>();
            if (_listSoundData.Count == 0)
            {
                MyLogger.LogWarning("Sound data list is empty.");
            }

            _templateSource = new GameObject("AudioSource_Template").AddComponent<AudioSource>();
            _templateSource.transform.parent = transform;
            _listSoundPlayings.Clear();

            maxCapacityMusic = soundList.maxCapacityMusic;
            maxCapacitySoundEffects = soundList.maxCapacitySFX;
        }

#if UNITY_EDITOR
        private void OnApplicationPause(bool pause) => _pauseWhenInBackground = pause;
#endif

        private void Update() => CleanupStoppedSounds();

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
                    Main.Pool.Despawn(p.AudioSource);
                    _listSoundPlayings.RemoveAt(i);
                }
            }
        }

        #endregion
        //─────────────────────────────────────────────────────────────
        #region Play Entry

        public AudioSource Play(string id, VolumeFade volume = null, float startTime = 0, bool loop = false,
            float delay = 0, int priority = 128, MinMaxFloat pitch = default,
            Transform target = null, int minDistance = 1, int maxDistance = 500)
        {
            var data = GetSoundInfo(id);
            if (data == null)
            {
                MyLogger.LogError($"[Sound] No sound info with id: {id}");
                return null;
            }

            return InternalPlay(data.audioClip, data.type, volume, startTime, loop, delay, priority, pitch, target,
                minDistance, maxDistance);
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

            bool isMusic = type == SoundType.MUSIC || loop;

            // Handle disabled sound types
            if ((isMusic && !IsEnableMusic) || (!isMusic && !IsEnableSoundSFX))
            {
                if (isMusic)
                {
                    var data = _listSoundData.FirstOrDefault(s => s.audioClip == clip);
                    if (data != null && !_pendingMusic.Contains(data))
                        _pendingMusic.Add(data);
                }
                return null;
            }

            // Check capacity
            int count = _listSoundPlayings.Count(s => s.SoundData.type == type);
            int maxCap = (type == SoundType.MUSIC) ? maxCapacityMusic : maxCapacitySoundEffects;
            if (count >= maxCap)
                RemoveOldestSound(type);

            void PlayNow() => CreateAndPlayAudioSource(clip, type, startTime, volume, loop, priority, pitch, target, minDist, maxDist);

            if (delay > 0)
            {
                var tween = DOVirtual.DelayedCall(delay, PlayNow, false);
                _playingTweens[clip.name] = tween;
            }
            else PlayNow();

            return _listSoundPlayings.LastOrDefault()?.AudioSource;
        }

        #endregion
        //─────────────────────────────────────────────────────────────
        #region Core Logic

        private AudioSource CreateAndPlayAudioSource(AudioClip clip, SoundType type, float startTime, VolumeFade volume,
            bool loop, int priority, MinMaxFloat pitch, Transform target, int minDist, int maxDist)
        {
            var source = Main.Pool.Spawn(KEY_POOL.KEY_AUDIO_SOUND, _templateSource, _parentGroup);
            source.Stop();
            source.name = clip.name;
            source.clip = clip;
            source.loop = loop;

            pitch ??= new MinMaxFloat(1, 1);

            var playing = new PlayingSound
            {
                AudioSource = source,
                SoundData = new SoundData { id = clip.name, audioClip = clip, type = type, pitch = pitch }
            };

            float volumeMult = (type == SoundType.MUSIC ? MusicVolume : SFXVolume);
            volume ??= new VolumeFade(0, 1, 0);
            float targetVol = volume.target;

            // Volume tween
            if (volume.duration > 0)
            {
                _tweener?.Kill();
                _tweener = DOVirtual.Float(volume.init, targetVol, volume.duration, v =>
                {
                    playing.RawVolume = v;
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
            source.minDistance = minDist;
            source.maxDistance = maxDist;
            if (startTime > 0) source.time = startTime;

            // Spatial setup
            if (target == null)
            {
                source.spatialBlend = 0;
            }
            else
            {
                source.spatialBlend = 1;
                source.transform.position = target.position;
            }

            source.Play();
            _listSoundPlayings.Add(playing);
            return source;
        }

        private void RemoveOldestSound(SoundType type)
        {
            var oldest = _listSoundPlayings.FirstOrDefault(s => s.SoundData.type == type);
            if (oldest?.AudioSource != null)
            {
                oldest.AudioSource.Stop();
                Main.Pool.Despawn(oldest.AudioSource);
                _listSoundPlayings.Remove(oldest);
            }
        }

        private IEnumerator DespawnAudioSource(AudioSource src, float delay)
        {
            yield return new WaitForSeconds(delay);
            Main.Pool.Despawn(src);
        }

        #endregion
        //─────────────────────────────────────────────────────────────
        
        #region Control
        public SoundData GetSoundInfo(string id) => _listSoundData.FirstOrDefault(s => s.id == id);
        public SoundData GetSoundInfo(AudioClip clip) => _listSoundData.FirstOrDefault(s => s.audioClip == clip);

        public void SetParentGroup(Transform group, bool dontDestroy)
        {
            _parentGroup = group;
            if (dontDestroy)
            {
                if (!_parentGroup.TryGetComponent(out DontDestroy existing))
                    _parentGroup.gameObject.AddComponent<DontDestroy>().DontDesGOOnLoad();
            }
        } 
        #endregion
    }
}
