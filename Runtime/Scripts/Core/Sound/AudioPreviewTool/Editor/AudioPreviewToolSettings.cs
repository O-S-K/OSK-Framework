#if UNITY_EDITOR
//
// Copyright (c) 2023 Warped Imagination. All rights reserved. 
//

using UnityEditor;

namespace WarpedImagination.AudioPreviewTool
{
    /// <summary>
    /// Holds the settings for the Audio Preview Tool
    /// </summary>
    public static class AudioPreviewToolSettings
    {
        // Note: change this if you want to save the preference under a different name
        const string ENABLED_EDITOR_PREF = "AudioPreviewToolEnabled";

        static bool? _enabled;

        /// <summary>
        /// Whether or not the Audio Preview Tool is enabled
        /// </summary>
        public static bool Enabled
        {
            get 
            {
                if(!_enabled.HasValue)
                    _enabled = EditorPrefs.GetBool(ENABLED_EDITOR_PREF, true);
                return _enabled.Value;
            }
            set 
            {
                _enabled = value;
                EditorPrefs.SetBool(ENABLED_EDITOR_PREF, _enabled.Value); 
            }
        }
    }
}
#endif