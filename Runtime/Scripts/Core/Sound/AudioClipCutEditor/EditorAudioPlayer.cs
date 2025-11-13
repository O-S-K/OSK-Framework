#if UNITY_EDITOR 
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OSK
{
    public static class EditorAudioPlayer
    {
        private static MethodInfo _playClipMethod;
        private static MethodInfo _stopAllClipsMethod;
        private static MethodInfo _stopClipMethod;
        private static MethodInfo _isClipPlayingMethod;
        private static MethodInfo _getPositionMethod;
        private static MethodInfo _pauseClipMethod;
        private static bool _initialized;

        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var asm = typeof(EditorWindow).Assembly;
            var audioUtil = asm.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null)
            {
                Debug.LogWarning("[OSK] AudioUtil type not found (Unity internal API changed).");
                return;
            }

            var methods = audioUtil.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            _playClipMethod = methods.FirstOrDefault(m =>
            {
                var ps = m.GetParameters();
                return ps.Length >= 2 && ps[0].ParameterType == typeof(AudioClip)
                       && (ps[1].ParameterType == typeof(int) || ps[1].ParameterType == typeof(float) || ps[1].ParameterType == typeof(double));
            });

            _stopAllClipsMethod = methods.FirstOrDefault(m => m.GetParameters().Length == 0 && m.Name.ToLower().Contains("stop"));
            _stopClipMethod = methods.FirstOrDefault(m => m.Name.ToLower().Contains("stop") && m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(AudioClip));
            _isClipPlayingMethod = methods.FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(AudioClip)
                                                               && (m.Name.ToLower().Contains("isclipplaying") || m.Name.ToLower().Contains("isplaying")));
            _getPositionMethod = methods.FirstOrDefault(m => (m.ReturnType == typeof(int) || m.ReturnType == typeof(long)) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(AudioClip));
            _pauseClipMethod = methods.FirstOrDefault(m => m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(AudioClip) && m.Name.ToLower().Contains("pause"));

            Debug.LogFormat("[OSK] AudioUtil discovery: Play={0} StopAll={1} StopClip={2} IsPlaying={3} Pos={4} Pause={5}",
                _playClipMethod?.Name ?? "null",
                _stopAllClipsMethod?.Name ?? "null",
                _stopClipMethod?.Name ?? "null",
                _isClipPlayingMethod?.Name ?? "null",
                _getPositionMethod?.Name ?? "null",
                _pauseClipMethod?.Name ?? "null");
        }

        public static void PlayClip(AudioClip clip, float startSeconds, bool loop)
        {
            Initialize();
            if (clip == null || _playClipMethod == null) return;

            var ps = _playClipMethod.GetParameters();
            object[] args = new object[ps.Length];
            args[0] = clip;

            Type t = ps[1].ParameterType;
            if (t == typeof(int))
                args[1] = Mathf.Clamp((int)(startSeconds * clip.frequency), 0, Mathf.Max(0, clip.samples - 1));
            else if (t == typeof(float))
                args[1] = startSeconds;
            else if (t == typeof(double))
                args[1] = (double)startSeconds;
            else
                args[1] = (int)(startSeconds * clip.frequency);

            if (ps.Length >= 3 && ps[2].ParameterType == typeof(bool))
                args[2] = loop;

            try
            {
                _playClipMethod.Invoke(null, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[OSK] PlayClip invoke failed: " + e.Message);
            }
        }

        public static void StopAllClips()
        {
            Initialize();
            try
            {
                if (_stopAllClipsMethod != null)
                {
                    _stopAllClipsMethod.Invoke(null, null);
                    return;
                }

                if (_stopClipMethod != null)
                {
                    _stopClipMethod.Invoke(null, new object[] { (AudioClip)null });
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[OSK] StopAllClips failed: " + e.Message);
            }
        }

        public static bool IsPlaying(AudioClip clip)
        {
            Initialize();
            if (_isClipPlayingMethod == null || clip == null) return false;
            try
            {
                return (bool)_isClipPlayingMethod.Invoke(null, new object[] { clip });
            }
            catch { return false; }
        }

        public static int GetCurrentSamplePosition(AudioClip clip)
        {
            Initialize();
            if (_getPositionMethod == null || clip == null) return -1;
            try
            {
                var r = _getPositionMethod.Invoke(null, new object[] { clip });
                if (r is int i) return i;
                if (r is long l) return (int)l;
            }
            catch { }
            return -1;
        }

        public static bool TryPause(AudioClip clip)
        {
            Initialize();
            if (clip == null) return false;
            if (_pauseClipMethod == null) return false;
            try
            {
                _pauseClipMethod.Invoke(null, new object[] { clip });
                return true;
            }
            catch { return false; }
        }
    }
}

#endif