#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace OSK
{
    [CustomEditor(typeof(BlackboardData))]
    public class BlackboardDataEditor : Editor
    {
        private SerializedProperty entriesProperty;
        private bool showAddEntry = false;
        private string newEntryKey = "";
        private BlackboardData.BlackboardValueType newEntryType;
        private int newEntryPriority = 0;
        private string newEntryCategory = "";
        private bool newEntryReadOnly = false;
        private string newEntryDescription = "";
        private Vector2 scrollPosition;

        private readonly Color readOnlyColor = new Color(1, 0.92f, 0.84f);
        private readonly Color highPriorityColor = new Color(0.85f, 1f, 0.85f);

        private void OnEnable()
        {
            entriesProperty = serializedObject.FindProperty("entries");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Blackboard Entries", EditorStyles.boldLabel);
            var blackboardData = (BlackboardData)target;

            /*// Categories filter
            var categories = blackboardData.entries
                .Select(e => e.category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            if (categories.Any())
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Filter by Category", EditorStyles.boldLabel);
                foreach (var category in categories)
                {
                    if (GUILayout.Button(category))
                    {
                        // TODO: Implement category filtering
                    }
                }
            }*/

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Display existing entries
            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                EditorGUILayout.Space(5);
                DrawEntryProperty(entriesProperty.GetArrayElementAtIndex(i), i);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(10);

            // Add new entry section
            DrawAddEntrySection();

            if (GUILayout.Button("Sort Entries"))
            {
                blackboardData.SortEntries();
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEntryProperty(SerializedProperty entryProperty, int index)
        {
            var keyProp = entryProperty.FindPropertyRelative("key");
            var typeProp = entryProperty.FindPropertyRelative("valueType");
            var priorityProp = entryProperty.FindPropertyRelative("priority");
            var categoryProp = entryProperty.FindPropertyRelative("category");
            var readOnlyProp = entryProperty.FindPropertyRelative("isReadOnly");
            var descriptionProp = entryProperty.FindPropertyRelative("description"); 

            // Set background color based on priority and readonly status
            var originalColor = GUI.backgroundColor;
            if (readOnlyProp.boolValue)
                GUI.backgroundColor = readOnlyColor;
            else if (priorityProp.intValue > 0)
                GUI.backgroundColor = highPriorityColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            // Header with delete button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Entry {index + 1}: {keyProp.stringValue}", EditorStyles.boldLabel);

            // Priority badge
            if (priorityProp.intValue > 0)
            {
                GUILayout.Label($"Priority: {priorityProp.intValue}", EditorStyles.miniLabel);
            }

            if (readOnlyProp.boolValue)
            {
                GUILayout.Label("Read Only", EditorStyles.miniLabel);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Entry",
                        $"Are you sure you want to delete the entry '{keyProp.stringValue}'?",
                        "Yes", "No"))
                {
                    entriesProperty.DeleteArrayElementAtIndex(index);
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            // Description
            if (!string.IsNullOrEmpty(descriptionProp.stringValue))
            {
                EditorGUILayout.HelpBox(descriptionProp.stringValue, MessageType.Info);
            }

            // Key field
            EditorGUI.BeginChangeCheck();
            string newKey = EditorGUILayout.TextField("Key", keyProp.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (IsKeyUnique(newKey, index))
                {
                    keyProp.stringValue = newKey;
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Key",
                        "This key already exists in the blackboard.", "OK");
                }
            }

            categoryProp.stringValue = EditorGUILayout.TextField("Category", categoryProp.stringValue);
            priorityProp.intValue = EditorGUILayout.IntField("Priority", priorityProp.intValue);
            readOnlyProp.boolValue = EditorGUILayout.Toggle("Read Only", readOnlyProp.boolValue);
            descriptionProp.stringValue = EditorGUILayout.TextField("Description", descriptionProp.stringValue); 
            
            

            // Type field (read-only)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(typeProp);
            EditorGUI.EndDisabledGroup();

            // Value field based on type
            DrawValueField(entryProperty, (BlackboardData.BlackboardValueType)typeProp.enumValueIndex);

            EditorGUILayout.EndVertical();
        }

        private void DrawValueField(SerializedProperty entryProperty, BlackboardData.BlackboardValueType type)
        {
            switch (type)
            {
                case BlackboardData.BlackboardValueType.Int:
                    EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("intValue"),
                        new GUIContent("Value"));
                    break;
                case BlackboardData.BlackboardValueType.Float:
                    EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("floatValue"),
                        new GUIContent("Value"));
                    break;
                case BlackboardData.BlackboardValueType.Bool:
                    EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("boolValue"),
                        new GUIContent("Value"));
                    break;
                case BlackboardData.BlackboardValueType.String:
                    EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("stringValue"),
                        new GUIContent("Value"));
                    break;
                case BlackboardData.BlackboardValueType.Vector3:
                    EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("vector3Value"),
                        new GUIContent("Value"));
                    break;
                case BlackboardData.BlackboardValueType.Color:
                    EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("colorValue"),
                        new GUIContent("Value"));
                    break;
                case BlackboardData.BlackboardValueType.Object:
                    EditorGUILayout.PropertyField(entryProperty.FindPropertyRelative("objectValue"),
                        new GUIContent("Value"));
                    break;
            }
        }

        private void DrawAddEntrySection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showAddEntry = EditorGUILayout.Foldout(showAddEntry, "Add New Entry", true);
            if (showAddEntry)
            {
                newEntryKey = EditorGUILayout.TextField("Key", newEntryKey);
                newEntryType = (BlackboardData.BlackboardValueType)EditorGUILayout.EnumPopup("Type", newEntryType);
                newEntryPriority = EditorGUILayout.IntField("Priority", newEntryPriority);
                newEntryCategory = EditorGUILayout.TextField("Category", newEntryCategory);
                newEntryReadOnly = EditorGUILayout.Toggle("Read Only", newEntryReadOnly);
                newEntryDescription = EditorGUILayout.TextField("Description", newEntryDescription);

                EditorGUILayout.Space(10);
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newEntryKey) || !IsKeyUnique(newEntryKey, -1));
                if (GUILayout.Button("Add Entry"))
                {
                    AddNewEntry();
                }

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
        }

        private void AddNewEntry()
        {
            entriesProperty.InsertArrayElementAtIndex(entriesProperty.arraySize);
            var newEntry = entriesProperty.GetArrayElementAtIndex(entriesProperty.arraySize - 1);

            newEntry.FindPropertyRelative("key").stringValue = newEntryKey;
            newEntry.FindPropertyRelative("valueType").enumValueIndex = (int)newEntryType;
            newEntry.FindPropertyRelative("priority").intValue = newEntryPriority;
            newEntry.FindPropertyRelative("category").stringValue = newEntryCategory;
            newEntry.FindPropertyRelative("isReadOnly").boolValue = newEntryReadOnly;
            newEntry.FindPropertyRelative("description").stringValue = newEntryDescription;

            // Reset input fields
            newEntryKey = "";
            newEntryPriority = 0;
            newEntryCategory = "";
            newEntryReadOnly = false;
            newEntryDescription = "";
            showAddEntry = false;
        }

        private bool IsKeyUnique(string key, int excludeIndex)
        {
            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                if (i == excludeIndex) continue;

                var entryKey = entriesProperty.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("key").stringValue;
                if (entryKey == key)

                {
                    OSKLogger.LogWarning("Blackboard",$"Key '{key}' already exists in the blackboard.");
                    return false;
                }
            }

            return true;
        }
    }
}
#endif