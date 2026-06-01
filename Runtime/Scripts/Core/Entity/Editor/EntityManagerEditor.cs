#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace OSK
{
    [CustomEditor(typeof(EntityManager))]
    public class EntityManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var manager = (EntityManager)target;

            // Draw default inspector fields
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pure ECS Info", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see ECS data.", MessageType.Info);
                return;
            }

            // Dùng Reflection lấy private field để hiển thị thông tin
            var fieldId = typeof(EntityManager).GetField("_nextEntityId", BindingFlags.NonPublic | BindingFlags.Instance);
            int nextId = (int)(fieldId?.GetValue(manager) ?? 0);
            EditorGUILayout.LabelField($"Total Entities Created: {nextId}");

            var systemsField = typeof(EntityManager).GetField("_systems", BindingFlags.NonPublic | BindingFlags.Instance);
            var systems = systemsField?.GetValue(manager) as List<EntitySystem>;
            
            if (systems != null)
            {
                EditorGUILayout.LabelField($"Active Systems: {systems.Count}");
                foreach (var sys in systems)
                {
                    EditorGUILayout.LabelField($"- {sys.GetType().Name} (Entities: {sys.Entities.Count})");
                }
            }

            // Repaint the editor for live updates
            Repaint();
        }
    }
}
#endif