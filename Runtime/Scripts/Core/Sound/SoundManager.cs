using System.Linq;
using DG.Tweening;
using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace OSK
{
    public partial class SoundManager : GameFrameworkComponent
    {
        [ShowInInspector] private List<SoundData> _listSoundData = new List<SoundData>();
        [ShowInInspector] private List<PlayingSound> _listSoundPlayings = new List<PlayingSound>();
        private Dictionary<string, Tween> _playingTweens = new Dictionary<string, Tween>();

        public List<SoundData> GetListSoundData => _listSoundData;
        public List<PlayingSound> GetListSoundPlayings => _listSoundPlayings;
        public Dictionary<string, Tween> GetPlayingTweens => _playingTweens;
        
        [InfoBox("⚠️ Warning: Use maxCapacityMusic / maxCapacitySoundEffects to limit the number of music and sound effects playing at the same time.", InfoMessageType.Warning)]
        [SerializeField] private int maxCapacityMusic = 5;
        [SerializeField] private int maxCapacitySoundEffects = 10;

        [InfoBox("⚠️ Warning: Use IsEnableMusic / IsEnableSoundSFX OR MusicVolume / SFXVolume. Don't use both.", InfoMessageType.Warning)]
        public bool IsEnableMusic = true;
        public bool IsEnableSoundSFX = true;
        
        public float MusicVolume = 1;
        public float SFXVolume = 1;

        private Tweener _tweener;
        private Transform _parentGroup;


        private Transform _cameraTransform;

        public Transform CameraTransform
        {
            get
            {
                if (_cameraTransform)
                    return _cameraTransform;

                var cam = Camera.main;
                if (cam == null)
                {
                    OSK.Logg.LogError("Camera.main is null");
                    return null;
                }

                return _cameraTransform = cam.transform;
            }
            set => _cameraTransform = value;
        }

        private AudioSource _soundObject;
        private bool pauseWhenInBackground = false;


        public override void OnInit()
        {
            if (Main.Instance.configInit.data.listSoundSo == null)
                return;

            _listSoundData = Main.Instance.configInit.data.listSoundSo.ListSoundInfos;
            if (_listSoundData == null || _listSoundData.Count == 0)
            {
                OSK.Logg.LogError("SoundInfos is empty");
                return;
            }

            _soundObject = new GameObject("AudioSource").AddComponent<AudioSource>();
            _soundObject.transform.parent = transform;
            _listSoundPlayings = new List<PlayingSound>();

            maxCapacityMusic = Main.Instance.configInit.data.listSoundSo.maxCapacityMusic;
            maxCapacitySoundEffects = Main.Instance.configInit.data.listSoundSo.maxCapacitySFX;
        }

#if UNITY_EDITOR
        private void OnApplicationPause(bool pause)
        {
            pauseWhenInBackground = pause;
        }
#endif


        private void Update() => CheckForStoppedMusic();

        private void CheckForStoppedMusic()
        {
            if (_listSoundPlayings == null || _listSoundPlayings.Count == 0)
                return;

#if UNITY_EDITOR
            if (pauseWhenInBackground)
                return;
#endif

            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                var playing = _listSoundPlayings[i];
                if (playing.AudioSource != null && !playing.AudioSource.isPlaying && !playing.IsPaused)
                {
                    Main.Pool.Despawn(playing.AudioSource);
                    _listSoundPlayings.RemoveAt(i--);
                }
            }
        }

        public AudioSource PlayAudioClip(AudioClip clip, SoundType soundType = SoundType.SFX, VolumeFade volume = null,
            float startTime = 0,
            bool loop = false, float delay = 0, int priority = 128, MinMaxFloat pitch = null,
            Transform target = null, int minDistance = 1, int maxDistance = 500)
        {
            if ((loop && !IsEnableMusic) || !IsEnableSoundSFX) return null;
            if (_listSoundPlayings.Count >= maxCapacitySoundEffects) RemoveOldestSound(SoundType.SFX);

            void PlayNow()
            {
                if (volume == null)
                {
                    var v = _listSoundData.FirstOrDefault(s => s.audioClip == clip).volume;
                    volume = new VolumeFade(target: v);
                }
                
                if(pitch == null)
                    pitch =  _listSoundData.FirstOrDefault(s => s.audioClip == clip).pitch;
                

                var source = CreateAudioSource(clip.name, clip, soundType, startTime, volume, loop,
                    priority, pitch, target, minDistance, maxDistance);
            }

            if (delay > 0)
            {
                var tween = DOVirtual.DelayedCall(delay, PlayNow, ignoreTimeScale: false);
                _playingTweens[clip.name] = tween;
            }
            else PlayNow();

            return _listSoundPlayings.LastOrDefault()?.AudioSource;
        }

        public AudioSource Play(string id, VolumeFade volume = null, float startTime = 0,
            bool loop = false, float delay = 0, int priority = 128, MinMaxFloat pitch = default,
            Transform target = null, int minDistance = 1, int maxDistance = 500)
        {
            if (!IsEnableMusic && !IsEnableSoundSFX) return null;

            var data = GetSoundInfo(id);
            if (data == null)
            {
                Logg.LogError("[Sound] No Sound Info with id: " + id);
                return null;
            }

            if ((data.type == SoundType.MUSIC && !IsEnableMusic) ||
                (data.type == SoundType.SFX && !IsEnableSoundSFX)) return null;

            if (data.type == SoundType.MUSIC &&
                _listSoundPlayings.Count(s => s.SoundData.type == SoundType.MUSIC) >= maxCapacityMusic)
                RemoveOldestSound(SoundType.MUSIC);
            else if (data.type == SoundType.SFX && _listSoundPlayings.Count(s => s.SoundData.type == SoundType.SFX) >=
                     maxCapacitySoundEffects)
                RemoveOldestSound(SoundType.SFX);

            void PlayNow()
            {
                if (volume == null)
                {
                    var v = _listSoundData.FirstOrDefault(s => s.id == id).volume;
                    volume = new VolumeFade(target: v);
                }
                
                pitch ??= _listSoundData.FirstOrDefault(s => s.id == id).pitch;
                
                var source = CreateAudioSource(id, data.audioClip, data.type, startTime, volume, loop, priority, pitch,
                    target,
                    minDistance, maxDistance);
            }

            if (delay > 0)
            {
                var tween = DOVirtual.DelayedCall(delay, PlayNow, ignoreTimeScale: false);
                _playingTweens[data.id] = tween;
            }
            else PlayNow();

            return _listSoundPlayings.LastOrDefault()?.AudioSource;
        }

        private void RemoveOldestSound(SoundType type)
        {
            var oldest = _listSoundPlayings.FirstOrDefault(s => s.SoundData.type == type);
            if (oldest != null && oldest.AudioSource != null)
            {
                oldest.AudioSource.Stop();
                Destroy(oldest.AudioSource.gameObject);
                _listSoundPlayings.Remove(oldest);
            }
        }

        private IEnumerator DespawnAudioSource(AudioSource audioSource, float delay)
        {
            yield return new WaitForSeconds(delay);
            Main.Pool.Despawn(audioSource);
        }


        private AudioSource CreateAudioSource(string id, AudioClip clip, SoundType soundType, float startTime,
            VolumeFade volume, bool loop,
            int priority, MinMaxFloat pitch, Transform transform, int minDist, int maxDist)
        {
            var source = Main.Pool.Spawn(KeyGroupPool.KEY_AUDIO_SOUND, _soundObject, _parentGroup);
            source.Stop();
            source.name = id;
            source.clip = clip;
            source.loop = loop;
            pitch ??= new MinMaxFloat(1, 1);

            var playing = new PlayingSound
            {
                AudioSource = source,
                SoundData = new SoundData { id = clip.name, audioClip = clip, type = soundType, pitch = pitch }
            };
            float volumeMultiplier = soundType == SoundType.MUSIC ? MusicVolume : SFXVolume;
            volume ??= new VolumeFade(1);
            float finalTargetVolume = volume.target;

            if (volume.duration > 0)
            {
                _tweener?.Kill();
                _tweener = DOVirtual.Float(volume.init, finalTargetVolume, volume.duration, v =>
                {
                    playing.RawVolume = v;
                    source.volume = v * volumeMultiplier;
                }).OnUpdate(() => { source.volume = playing.RawVolume * volumeMultiplier; });
            }
            else
            {
                playing.RawVolume = finalTargetVolume;
                source.volume = finalTargetVolume * volumeMultiplier;
            }

            source.pitch = playing.SoundData.pitch.RandomValue;
            source.priority = priority;
            source.minDistance = minDist;
            source.maxDistance = maxDist;

            if (startTime > 0) source.time = startTime;

            if (transform == null)
            {
                source.spatialBlend = 0;
                //source.transform.position = CameraTransform.position;
            }
            else
            {
                source.spatialBlend = 1;
                source.transform.position = transform.position;
            }

            /*source.outputAudioMixerGroup = data.mixerGroup;
            source.mute = data.mute;
            source.bypassEffects = data.bypassEffects;
            source.bypassListenerEffects = data.bypassListenerEffects;
            source.bypassReverbZones = data.bypassReverbZones;
            source.panStereo = data.panStereo;
            source.reverbZoneMix = data.reverbZoneMix;
            source.dopplerLevel = data.dopplerLevel;
            source.spread = data.spread;
            source.ignoreListenerVolume = data.ignoreListenerVolume;
            source.ignoreListenerPause = data.ignoreListenerPause;#1#*/

            source.Play();
            _listSoundPlayings.Add(playing);
            return source;
        }


        public SoundData GetSoundInfo(string id) => _listSoundData.FirstOrDefault(s => s.id == id);

        public SoundData GetSoundInfo(AudioClip audioClip) =>
            _listSoundData.FirstOrDefault(s => s.audioClip == audioClip);


        public void SetCameraTransform(Transform cam) => CameraTransform = cam;

        public void SetParentGroup(Transform group, bool setDontDestroy)
        {
            _parentGroup = group;
            SetGroupDontDestroyOnLoad(setDontDestroy);
        }

        public void SetGroupDontDestroyOnLoad(bool enable)
        {
            if (_parentGroup == null)
            {
                OSK.Logg.LogError("ParentGroup Sound is null. Please set it before calling this method.");
                return;
            }

            var existing = _parentGroup.GetComponent<DontDestroy>();

            if (enable)
            {
                if (existing == null)
                    existing = _parentGroup.gameObject.AddComponent<DontDestroy>();
                existing.DontDesGOOnLoad();
            }
            else
            {
                if (existing != null)
                    UnityEngine.Object.Destroy(existing);
            }
        }

        public void LogStatus()
        {
            Logg.Log("SoundManager Status");
            Logg.Log($"1.Main.Configs.init.data.listSoundSo: {Main.Instance.configInit.data.listSoundSo}");
            Logg.Log($"2.KeyGroupPool.AudioSound: {KeyGroupPool.KEY_AUDIO_SOUND}");
            Logg.Log($"3.CameraTransform: {CameraTransform}");
            Logg.Log($"4.ParentGroup: {_parentGroup}");

            Logg.Log($"AudioListener: {AudioListener.pause}");
            Logg.Log($"5.IsEnableMusic: {IsEnableMusic}");
            Logg.Log($"6.IsEnableSoundSFX: {IsEnableSoundSFX}");

            Logg.Log($"7.MaxCapacityMusic: {maxCapacityMusic}");
            Logg.Log($"8.MaxCapacitySoundEffects: {maxCapacitySoundEffects}");

            Logg.Log($"9.ListSoundInfos: {_listSoundData.Count}");
            Logg.Log($"10.ListMusicInfos: {_listSoundPlayings.Count}");

            for (int i = 0; i < _listSoundPlayings.Count; i++)
            {
                Logg.Log($"_listMusicInfos[{i}]: {_listSoundPlayings[i].SoundData.id}");
            }

            Logg.Log("End SoundManager Status");
        }
    }
}