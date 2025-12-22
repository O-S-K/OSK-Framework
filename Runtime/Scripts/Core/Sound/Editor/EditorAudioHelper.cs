#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OSK
{
    [ExecuteInEditMode]
    public static class EditorAudioHelper
    {
        private static AudioSource _editorAudioSource;
        private static GameObject audioObject;

        public static void PlayClip(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("AudioClip is null.");
                return;
            }

            // Create an AudioSource if it doesn't exist
            _editorAudioSource = Object.FindObjectOfType<AudioSource>();
            if (_editorAudioSource == null)
            {
                 audioObject = new GameObject("EditorAudioSourceTest");
                //audioObject.hideFlags = HideFlags.HideAndDontSave;
                _editorAudioSource = audioObject.GetOrAdd<AudioSource>();
            }

            // Set the clip and play
            _editorAudioSource.clip = clip;
            _editorAudioSource.Play();
          
            float playTime = (float)EditorApplication.timeSinceStartup;
            float duration = clip.length;

            EditorApplication.update += CheckPlaybackDone;

            void CheckPlaybackDone()
            {
                if ((float)EditorApplication.timeSinceStartup - playTime >= duration)
                {
                    EditorApplication.update -= CheckPlaybackDone;

                    if (audioObject != null)
                    {
                        GameObject.DestroyImmediate(audioObject);
                        audioObject = null;
                    }
                }
            }
            MyLogger.Log($"Playing clip: {clip.name} + pitch: {_editorAudioSource.pitch} + volume: {_editorAudioSource.volume}");
        }

        public static void StopClip(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("AudioClip is null.");
                return;
            }

            // Stop the clip
            _editorAudioSource.Stop();
            if (audioObject)
            {
                GameObject.DestroyImmediate(audioObject.gameObject);
            }
        }

        // Check if a specific clip is currently playing
        public static bool IsClipPlaying(AudioClip clip)
        {
            return _editorAudioSource != null && _editorAudioSource.isPlaying && _editorAudioSource.clip == clip;
        }

        public static void SetVolume(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                Debug.LogWarning("AudioClip is null.");
                return;
            }

            // Set the volume
            if(_editorAudioSource != null)
               _editorAudioSource.volume = volume;
        }
        
        public static void SetPitch(AudioClip clip, float pitch)
        {
            if (clip == null)
            {
                Debug.LogWarning("AudioClip is null.");
                return;
            }

            // Set the pitch
            if(_editorAudioSource != null)
               _editorAudioSource.pitch = pitch;
        }
    }
}
#endif