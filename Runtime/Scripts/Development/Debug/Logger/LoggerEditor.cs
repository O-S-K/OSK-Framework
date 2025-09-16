#if UNITY_EDITOR
namespace OSK
{
    using UnityEngine;
    using UnityEditor;

    public class LoggerEditor : EditorWindow
    {
        private Vector2 scrollPos;

        [MenuItem("Logging/Logger Window")]
        public static void ShowWindow()
        {
            GetWindow<LoggerEditor>("Logger");
        }

        private void OnGUI()
        {
            GUILayout.Space(5);

            // Global enable/disable logging
            bool globalEnabled = Logg.IsLogEnabled;
            bool newGlobalEnabled = EditorGUILayout.ToggleLeft("Enable Logging", globalEnabled);
            if (newGlobalEnabled != globalEnabled)
                Logg.SetLogEnabled(newGlobalEnabled);

            GUILayout.Space(10);

            // Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Disable all"))
            {
                foreach (var channel in Logg.GetAllChannels())
                    Logg.SetChannelEnabled(channel, false);
            }
            if (GUILayout.Button("Enable all"))
            {
                foreach (var channel in Logg.GetAllChannels())
                    Logg.SetChannelEnabled(channel, true);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Logging Channels", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var channel in Logg.GetAllChannels())
            {
                EditorGUILayout.BeginHorizontal();

                // Toggle active
                bool active = Logg.IsChannelActive(channel);
                bool newActive = EditorGUILayout.ToggleLeft(channel, active, GUILayout.Width(150));
                if (newActive != active)
                    Logg.SetChannelEnabled(channel, newActive);

                // Color picker
                Color oldColor = Logg.GetChannelColor(channel);
                Color newColor = EditorGUILayout.ColorField(oldColor, GUILayout.Width(70));
                if (newColor != oldColor)
                    Logg.SetChannelColor(channel, newColor);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
