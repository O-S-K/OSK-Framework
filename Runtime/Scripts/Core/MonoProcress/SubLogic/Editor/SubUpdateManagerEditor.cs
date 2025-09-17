#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace OSK
{
    [CustomEditor(typeof(MonoBehaviour), true)] 
    public class SubUpdateManagerEditor : Editor
    {
         private IHasSubUpdateContainer containerObject;
       
        private void OnEnable()
        {
            OSKLogger.Log("MonoTick",$"[Editor] Checking target: {target?.GetType().Name}");

            if (target is IHasSubUpdateContainer hasContainer)
            {
                containerObject = hasContainer;
                OSKLogger.Log("MonoTick",$"[Editor] ✅ Found SubUpdateContainer in {target.GetType().Name}");
            }
            else
            {
                    OSKLogger.Log("MonoTick",$"[Editor] ❌ {target.GetType().Name} does NOT implement IHasSubUpdateContainer");
            }
            
            EditorApplication.delayCall += () =>
            {
                OSKLogger.Log("Force refresh Editor...");
                Repaint(); 
            };
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); 
            if (containerObject == null) return;

            SubUpdateContainer container = containerObject.GetSubUpdateContainer();
            if (container == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("📦 Sub Update Container", EditorStyles.boldLabel);

            DrawTableHeader();
            DrawUpdateRow("🔄 Updates", container.GetUpdates());
            DrawUpdateRow("⚙️ FixedUpdates", container.GetFixedUpdates());
            DrawUpdateRow("⏳ LateUpdates", container.GetLateUpdates());

            EditorGUILayout.Space();
        }

        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("Count", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Elements", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawUpdateRow<T>(string typeName, IReadOnlyList<T> updates)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(typeName, GUILayout.Width(120));
            EditorGUILayout.LabelField(updates.Count.ToString(), GUILayout.Width(50));

            if (updates.Count > 0)
            {
                string elements = "";
                foreach (var update in updates)
                {
                    elements += $"{update.GetType().Name}, ";
                }

                elements = elements.TrimEnd(',', ' ');
                EditorGUILayout.LabelField(elements);
            }
            else
            {
                EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif