#if UNITY_EDITOR
//
// Copyright (c) 2023 Warped Imagination. All rights reserved. 
//

using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WarpedImagination.AudioPreviewTool
{

    /// <summary>
    /// Audio Preview Tool plays audio in the project view when an Audio Clip is double clicked
    /// </summary>
    public static class AudioPreviewTool
    {

        static int? _lastPlayedAudioClipId;

        /// The code is split between Unity versions the reason is
        /// Unity changed the names under the AudioUtil class 
#if UNITY_2019

        /// <summary>
        /// Runs when someone double clicks on an audio file in the project window
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        [OnOpenAsset]
        public static bool OnOpenAssetCallback(int instanceId, int line)
        {
            // check that the tool is enabled under preferences
            if (!AudioPreviewToolSettings.Enabled)
                return false;

            UnityEngine.Object obj = EditorUtility.InstanceIDToObject(instanceId);

            if (obj is AudioClip audioClip)
            {
                if (IsClipPlaying(audioClip))
                {
                    StopAllClips();

                    if (_lastPlayedAudioClipId.HasValue &&
                        _lastPlayedAudioClipId.Value != instanceId)
                    {
                        PlayClip(audioClip, 0, false);
                    }
                }
                else
                {
                    PlayClip(audioClip, 0, false);
                }

                _lastPlayedAudioClipId = instanceId;

                return true;
            }

            return false;
        }

        public static void PlayClip(AudioClip clip, int startSample, bool loop)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "PlayClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new System.Type[] {
                typeof(AudioClip),
                typeof(Int32),
                typeof(Boolean)
                }, 
                null );
            method.Invoke( null, new object[] { clip, startSample, loop } );
        }

        public static bool IsClipPlaying(AudioClip clip)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "IsClipPlaying",
                BindingFlags.Static | BindingFlags.Public );

            bool playing = (bool)method.Invoke( null, new object[] { clip });

            return playing;
        }

        public static void StopAllClips()
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "StopAllClips",
                BindingFlags.Static | BindingFlags.Public);

            method.Invoke(null, null);
        }

#elif UNITY_2020_1_OR_NEWER

        /// <summary>
        /// Runs when someone double clicks on an audio file in the project window
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        [OnOpenAsset]
        public static bool OnOpenAssetCallback(int instanceId, int line)
        {
            // check that the tool is enabled under preferences
            if (!AudioPreviewToolSettings.Enabled)
                return false;

            Object obj = EditorUtility.InstanceIDToObject(instanceId);

            if (obj is AudioClip audioClip)
            {
                if (IsPreviewClipPlaying())
                {
                    StopAllPreviewClips();

                    if (_lastPlayedAudioClipId.HasValue &&
                        _lastPlayedAudioClipId.Value != instanceId)
                    {
                        PlayPreviewClip(audioClip);
                    }
                }
                else
                {
                    PlayPreviewClip(audioClip);
                }

                _lastPlayedAudioClipId = instanceId;

                return true;
            }

            return false;
        }

        public static void PlayPreviewClip(AudioClip audioClip)
        {
            Assembly unityAssembly = typeof(AudioImporter).Assembly;
            Type audioUtil = unityAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo methodInfo = audioUtil.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(AudioClip), typeof(Int32), typeof(Boolean) },
                null);

            methodInfo.Invoke(null, new object[] { audioClip, 0, false });
        }

        public static bool IsPreviewClipPlaying()
        {
            Assembly unityAssembly = typeof(AudioImporter).Assembly;
            Type audioUtil = unityAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo methodInfo = audioUtil.GetMethod(
                "IsPreviewClipPlaying",
                BindingFlags.Static | BindingFlags.Public);

            bool isPlaying = (bool)methodInfo.Invoke(null, null);

            return isPlaying;
        }

        public static void StopAllPreviewClips()
        {
            Assembly unityAssembly = typeof(AudioImporter).Assembly;
            Type audioUtil = unityAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo methodInfo = audioUtil.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);

            methodInfo.Invoke(null, null);
        }

#endif

    }
}
#endif