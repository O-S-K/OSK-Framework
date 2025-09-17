#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace OSK
{
    [CustomEditor(typeof(BlackboardManager))]
    public class BlackboardManagerInspector : Editor
    {
        private bool[] foldouts;

        public override void OnInspectorGUI()
        {
            BlackboardManager blackboardManager = (BlackboardManager)target;

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Blackboard Details", EditorStyles.boldLabel);

            var blackboards = blackboardManager.blackboards;
            if (foldouts == null || foldouts.Length != blackboards.Count)
            {
                foldouts = new bool[blackboards.Count];
            }

            if (blackboards.Count > 0)
            {
                int index = 0;
                foreach (var kvp in blackboards)
                {
                    string name = kvp.Key;
                    Blackboard blackboard = kvp.Value;

                    if (blackboard != null)
                    {
                        var g = new GUIStyle(EditorStyles.foldout)
                        {
                            fontStyle = FontStyle.Bold,
                            fontSize = 13,
                            fixedHeight = 30,
                            normal = new GUIStyleState() {textColor = Color.cyan},
                        };
                        EditorGUILayout.Space(3);
                        foldouts[index] = EditorGUILayout.Foldout(foldouts[index],
                            $"Blackboard {index + 1}: {name} in {blackboard.GetPingObject().name}", g);

                        if (foldouts[index])
                        {
                            EditorGUILayout.BeginVertical("box");

                            int keyIndex = 0;
                            var values = blackboard.GetAllValuesOfType<object>();
                            foreach (var kvpValue in values)
                            { 
                                keyIndex++;
                                Color lerpColor = Color.Lerp(Color.yellow, Color.green, keyIndex / (float)values.Count);
                                EditorGUILayout.LabelField($"Key: {kvpValue.Key}", $"Value: {kvpValue.Value}", new GUIStyle()
                                {
                                    normal = new GUIStyleState() {textColor = lerpColor},
                                });
                            }

                            // Nút Ping Blackboard
                            if (GUILayout.Button("Ping Blackboard", GUILayout.Width(150)))
                            {
                                GameObject pingObject = blackboard.GetPingObject();
                                if (pingObject != null)
                                {
                                    EditorGUIUtility.PingObject(pingObject);
                                }
                                else
                                {
                                    OSKLogger.LogWarning("Blackboard",$"No ping object assigned for Blackboard '{name}'");
                                }
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }

                    index++;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Blackboards found.", MessageType.Info);
            }

            EditorGUILayout.Space(20);
        } 
    }
}
#endif