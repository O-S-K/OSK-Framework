#if UNITY_EDITOR
namespace OSK
{
    using UnityEngine;
    using UnityEditor;

    public class LoggerEditor : EditorWindow
    {
        private Vector2 scrollPos;

        [MenuItem("OSK Logger/Logger Window")]
        public static void ShowWindow()
        {
            GetWindow<LoggerEditor>("Logger");
        }

        private void OnGUI()
        {
            GUILayout.Space(5);

            // Global enable/disable logging
            bool globalEnabled = OSKLogger.IsLogEnabled;
            bool newGlobalEnabled = EditorGUILayout.ToggleLeft("Enable Logging", globalEnabled);
            if (newGlobalEnabled != globalEnabled)
                OSKLogger.SetLogEnabled(newGlobalEnabled);

            GUILayout.Space(10);

            // Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Disable all"))
            {
                foreach (var channel in OSKLogger.GetAllChannels())
                    OSKLogger.SetChannelEnabled(channel, false);
            }
            if (GUILayout.Button("Enable all"))
            {
                foreach (var channel in OSKLogger.GetAllChannels())
                    OSKLogger.SetChannelEnabled(channel, true);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Logging Channels", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var channel in OSKLogger.GetAllChannels())
            {
                EditorGUILayout.BeginHorizontal();

                // Toggle active
                bool active = OSKLogger.IsChannelActive(channel);
                bool newActive = EditorGUILayout.ToggleLeft(channel, active, GUILayout.Width(150));
                if (newActive != active)
                    OSKLogger.SetChannelEnabled(channel, newActive);

                // Color picker
                Color oldColor = OSKLogger.GetChannelColor(channel);
                Color newColor = EditorGUILayout.ColorField(oldColor, GUILayout.Width(70));
                if (newColor != oldColor)
                    OSKLogger.SetChannelColor(channel, newColor);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
