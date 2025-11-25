#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OSK
{
    public static class OSKProjectMenu
    {
        private const string MenuPath = "Assets/Open in OSK Audio Editor";

        [MenuItem(MenuPath, true)]
        private static bool ValidateOpenInEditor()
        {
            // enable only when single AudioClip selected
            if (Selection.objects == null || Selection.objects.Length != 1) return false;
            return Selection.activeObject is AudioClip;
        }

        [MenuItem(MenuPath)]
        public static void OpenInEditor(bool isOpenEditor = false)
        {
            var clip = Selection.activeObject as AudioClip;
            if (clip == null && !isOpenEditor) return;

            // open window and assign
            var w = EditorWindow.GetWindow<ClipEditorWindow>();
            w.titleContent = new GUIContent("Audio Clip Editor");
            // communicate via public API on window - we search for method SetClip if exists
            // If you want direct field set, adapt accordingly (we assume ClipEditorWindow has public method SetClip)
            var method = typeof(ClipEditorWindow).GetMethod("SetClipFromProjectSelection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (method != null)
            {
                method.Invoke(w, new object[] { clip });
            }
            else
            {
                // fallback: try set via reflection field _clip
                var field = typeof(ClipEditorWindow).GetField("_clip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(w, new OSKAudioClip(clip));
                }
            }
            w.Repaint();
            w.Show();
        }
    }
}
#endif