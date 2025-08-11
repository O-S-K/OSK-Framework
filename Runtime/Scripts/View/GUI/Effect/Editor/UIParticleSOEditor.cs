using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace OSK
{
    [CustomEditor(typeof(UIParticleSO))]
    public class UIParticleSOEditor : Editor
    {
        private SerializedProperty _effectSettings;

        private void OnEnable()
        {
            _effectSettings = serializedObject.FindProperty("_effectSettings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GUILayout.Space(6);

            for (int i = 0; i < _effectSettings.arraySize; i++)
            {
                var element = _effectSettings.GetArrayElementAtIndex(i);
                string settingName = GetSettingName(element, i);
                GetTile(element, i, settingName);

                if (!element.isExpanded) continue;

                EditorGUILayout.BeginVertical("box");
                {
                    DrawSetupGroup(element);

                    if (element.FindPropertyRelative("isDrop").boolValue)
                    {
                        GUILayout.Space(4);
                        DrawDropGroup(element);
                    }

                    GUILayout.Space(4);
                    DrawMoveGroup(element);

                    GUILayout.Space(6);
                    if (GUILayout.Button("❌ Remove Effect", GUILayout.Height(20)))
                    {
                        _effectSettings.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
                EditorGUILayout.EndVertical();

                GUILayout.Space(8);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("➕ Add New Effect Setting", GUILayout.Height(28)))
                _effectSettings.InsertArrayElementAtIndex(_effectSettings.arraySize);

            serializedObject.ApplyModifiedProperties();
        }

        private static void GetTile(SerializedProperty element, int i, string settingName)
        {
            Color yellow = new Color(1f, 0.85f, 0f); // vàng nhạt

            GUIStyle bigYellowFoldout = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            bigYellowFoldout.normal.textColor = yellow;
            bigYellowFoldout.onNormal.textColor = yellow;
            bigYellowFoldout.focused.textColor = yellow;
            bigYellowFoldout.onFocused.textColor = yellow;

            element.isExpanded = EditorGUILayout.Foldout(
                element.isExpanded,
                $"[{i + 1}]: {settingName}",
                true,
                bigYellowFoldout
            );
        }

        private string GetSettingName(SerializedProperty element, int index)
        {
            string name = element.FindPropertyRelative("name").stringValue;
            string displayName = string.IsNullOrEmpty(name) ? $"Effect Setting {index + 1}" : name;
            return displayName;
        }

        private void DrawSetupGroup(SerializedProperty element)
        {
            DrawSection("⚙ Setup", Color.white, () =>
            {
                EditorGUILayout.PropertyField(element.FindPropertyRelative("name"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("numberOfEffects"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("isDrop"));
            });
        }

        private void DrawDropGroup(SerializedProperty element)
        {
            DrawSection("💧 Drop", new Color(0.4f, 1f, 0.4f), () =>
            {
                EditorGUILayout.PropertyField(element.FindPropertyRelative("sphereRadius"));
                
                // Delay Drop (Min/Max)
                DrawMinMaxField(element.FindPropertyRelative("delayDrop"), "Delay Drop");

                // Time Drop (Min/Max)
                DrawMinMaxField(element.FindPropertyRelative("timeDrop"), "Time Drop");

                EditorGUILayout.PropertyField(element.FindPropertyRelative("typeAnimationDrop"));
                var typeAnimDrop = (TypeAnimation)element.FindPropertyRelative("typeAnimationDrop").enumValueIndex;
                if (typeAnimDrop == TypeAnimation.Ease)
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("easeDrop"));
                else if (typeAnimDrop == TypeAnimation.Curve)
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("curveDrop"));

                EditorGUILayout.PropertyField(element.FindPropertyRelative("isScaleDrop"));
                if (element.FindPropertyRelative("isScaleDrop").boolValue)
                {
                    DrawTwoFields(
                        element.FindPropertyRelative("scaleDropStart"),
                        element.FindPropertyRelative("scaleDropEnd"),
                        "Start", "End"
                    );
                }
            });
        }

        private void DrawMoveGroup(SerializedProperty element)
        {
            DrawSection("➡ Move", new Color(0.4f, 1f, 1f), () =>
            {
                EditorGUILayout.PropertyField(element.FindPropertyRelative("typeMove"));
                var typeMove = (TypeMove)element.FindPropertyRelative("typeMove").enumValueIndex;

                switch (typeMove)
                {
                    case TypeMove.Path:
                    case TypeMove.Beziers:
                    case TypeMove.CatmullRom:
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("paths"));
                        break;
                    case TypeMove.Straight:
                        DrawMinMaxField(element.FindPropertyRelative("height"), "Height");
                        break;
                    case TypeMove.DoJump:
                        DrawMinMaxField(element.FindPropertyRelative("jumpPower"), "Jump Power",0);
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("jumpNumber"));
                        break;
                    case TypeMove.Around:
                        DrawMinMaxField(element.FindPropertyRelative("midPointOffsetX"), "midPointOffsetX");
                        DrawMinMaxField(element.FindPropertyRelative("midPointOffsetZ"), "midPointOffsetZ");
                        DrawMinMaxField(element.FindPropertyRelative("height"), "Height");
                        break;
                    case TypeMove.Sin:
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("pointsCount"));
                        DrawMinMaxField(element.FindPropertyRelative("height"), "Height");
                        break;
                }

                EditorGUILayout.PropertyField(element.FindPropertyRelative("typeAnimationMove"));
                var typeAnimMove = (TypeAnimation)element.FindPropertyRelative("typeAnimationMove").enumValueIndex;
                if (typeAnimMove == TypeAnimation.Ease)
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("easeMove"));
                else if (typeAnimMove == TypeAnimation.Curve)
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("curveMove"));

                // Time Move (Min/Max)
                DrawMinMaxField(element.FindPropertyRelative("timeMove"), "Time Move");

                // Delay Move (Min/Max)
                DrawMinMaxField(element.FindPropertyRelative("delayMove"), "Delay Move");

                EditorGUILayout.PropertyField(element.FindPropertyRelative("isScaleMove"));
                if (element.FindPropertyRelative("isScaleMove").boolValue)
                {
                    DrawTwoFields(
                        element.FindPropertyRelative("scaleMoveStart"),
                        element.FindPropertyRelative("scaleMoveTarget"),
                        "Start", "Target"
                    );
                }
            });
        } 

        private void DrawSection(string title, Color color, System.Action drawContent)
        {
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = color * 0.6f;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = defaultColor;
            drawContent?.Invoke();
            EditorGUILayout.EndVertical();
        }

        private void DrawTwoFields(SerializedProperty propA, SerializedProperty propB, string labelA, string labelB)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(propA, new GUIContent(labelA));
            GUILayout.Space(10);
// End
            EditorGUILayout.LabelField(labelB, GUILayout.Width(050));
            propB.floatValue = EditorGUILayout.FloatField(propB.floatValue);
            EditorGUILayout.EndHorizontal();
            
        }

        private void DrawMinMaxField(SerializedProperty property, string label, float minLimit = -1000f, float maxLimit = 1000)
        {
            SerializedProperty minProp = property.FindPropertyRelative("min");
            SerializedProperty maxProp = property.FindPropertyRelative("max");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 3));

            minProp.floatValue = Mathf.Clamp(
                EditorGUILayout.FloatField(minProp.floatValue, GUILayout.MinWidth(30)),
                minLimit, maxLimit);

            maxProp.floatValue = Mathf.Clamp(
                EditorGUILayout.FloatField(maxProp.floatValue, GUILayout.MinWidth(30)),
                minLimit, maxLimit);

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
