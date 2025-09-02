using UnityEngine;
using UnityEditor;
using System.Linq;
using Sirenix.OdinInspector.Editor;

namespace OSK
{
    [CustomEditor(typeof(DirectorManager))]
    public class DirectorManagerEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            // Lấy reference tới script
            DirectorManager dm = (DirectorManager)target;

            // Inspector mặc định
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("=== DEBUG SCENES ===", EditorStyles.boldLabel);

            // Hiển thị danh sách scene đã load
            EditorGUILayout.LabelField("Loaded Scenes:");
            if (dm.LoadedScenes != null && dm.LoadedScenes.Count > 0)
            {
                foreach (var scene in dm.LoadedScenes.ToList())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(scene);
                    if (GUILayout.Button("Reload", GUILayout.Width(60)))
                    {
                        dm.ReloadSceneForce(scene);
                    }

                    if (GUILayout.Button("Unload", GUILayout.Width(60)))
                    {
                        dm.UnloadScene(scene);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("(none)");
            }

            EditorGUILayout.Space();
        } 
    }
}