#if UNITY_EDITOR
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OSK
{
    public static class EditorAudioSourcePlayer
    {
        private static GameObject _tempGO;
        private static AudioSource _source;
        private static bool _isPlaying;

        public static bool IsPlaying => _isPlaying && _source != null && _source.isPlaying;
        public static float CurrentTime => _source != null ? _source.time : 0f;

        public static void Play(AudioClip clip, float startTime, bool loop)
        {
            Stop();

            if (clip == null) return;

            _tempGO = new GameObject("OSK_EditorAudioPlayer");
            // keep editor hierarchy clean; do not hide (developer choice)
            _source = _tempGO.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = loop;
            _source.clip = clip;
            _source.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.001f));
            _source.Play();

            _isPlaying = true;
            EditorApplication.update += Update;
        }

        public static void Pause()
        {
            if (_source != null && _source.isPlaying)
            {
                _source.Pause();
                _isPlaying = false;
            }
        }

        public static void Resume()
        {
            if (_source != null && !_source.isPlaying)
            {
                _source.Play();
                _isPlaying = true;
            }
        }

        public static void Stop()
        {
            _isPlaying = false;
            EditorApplication.update -= Update;
            if (_source != null)
            {
                _source.Stop();
            }
            if (_tempGO != null)
            {
                GameObject.DestroyImmediate(_tempGO);
            }
            _source = null;
            _tempGO = null;
        }

        private static void Update()
        {
            if (_source == null)
            {
                Stop();
                return;
            }

            if (!_source.isPlaying && !_source.loop)
            {
                Stop();
            }
        }
    }
}
#endif

#endif