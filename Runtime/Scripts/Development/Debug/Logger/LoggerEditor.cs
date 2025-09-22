#if UNITY_EDITOR
namespace OSK
{
    using UnityEngine;
    using UnityEditor;

    public class LoggerEditor : EditorWindow
    {
        private Vector2 scrollPos;

        [MenuItem("OSK-Framework/Logger Window")]
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
        }
    }
}
#endif
