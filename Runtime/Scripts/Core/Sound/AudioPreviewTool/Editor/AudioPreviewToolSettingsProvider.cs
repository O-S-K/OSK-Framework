#if UNITY_EDITOR
//
// Copyright (c) 2023 Warped Imagination. All rights reserved. 
//

using UnityEditor;
using UnityEngine;

namespace WarpedImagination.AudioPreviewTool
{
    /// <summary>
    /// Displays entries for the Audio Preview Tool settings under the preferences window
    /// </summary>
    public class AudioPreviewToolSettingsProvider : SettingsProvider
    {
        GUIContent _content;

        public AudioPreviewToolSettingsProvider(string path, SettingsScope scope)
            : base(path, scope)
        { }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            GUILayout.Space(20f);

            if(_content == null)
                _content = new GUIContent("Enabled", "Switch this to enable the preview tool");

            bool enabled = EditorGUILayout.Toggle(_content, AudioPreviewToolSettings.Enabled);
            if(enabled != AudioPreviewToolSettings.Enabled)
            {
                AudioPreviewToolSettings.Enabled = enabled;
            }
        }


        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            // Note: change the first argument path to move these settings elsewhere under Preferences window
            // Note: change second argument if you prefer the settings to be under Player Settings
            return new AudioPreviewToolSettingsProvider("Preferences/Tools/Audio Preview Tool", SettingsScope.User);
        }

    }
}
#endif