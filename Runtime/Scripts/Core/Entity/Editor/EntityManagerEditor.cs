#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OSK
{
    [CustomEditor(typeof(EntityManager))]
    public class EntityManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var manager = (EntityManager)target;
            List<Entity> entities = manager.GetAll();

            // Draw default inspector fields
            DrawDefaultInspector();

            if (entities != null && entities.Count > 0)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    Entity entity = entities[i];
                    EditorGUILayout.BeginHorizontal();

                    // Show entity name
                    EditorGUILayout.LabelField(entity.gameObject.name);

                    // Enable the "Show" button if the entity is inactive
                    GUI.enabled = !entity.gameObject.activeSelf;
                    if (GUILayout.Button("Show"))
                    {
                        entity.gameObject.SetActive(true);
                    }

                    // Enable the "Hide" button if the entity is active
                    GUI.enabled = entity.gameObject.activeSelf;
                    if (GUILayout.Button("Hide"))
                    {
                        entity.gameObject.SetActive(false);
                    }

                    // Re-enable buttons for delete functionality
                    GUI.enabled = true;

                    // Delete button
                    if (GUILayout.Button("Delete"))
                    {
                        manager.Destroy(entity);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No active entities.");
            }

            // Repaint the editor for live updates
            if (GUI.changed)
            {
                Repaint();
            }
        }
    }
}
#endif