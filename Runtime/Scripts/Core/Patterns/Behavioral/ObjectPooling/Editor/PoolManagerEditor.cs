#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using OSK;

namespace OSK
{
    [CustomEditor(typeof(PoolManager))]
    public class PoolManagerEditor : Editor
    {
        private PoolManager poolManager;

        private void OnEnable()
        {
            poolManager = (PoolManager)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object Pool Hierarchy", EditorStyles.boldLabel);

            if (poolManager.GroupPrefabLookup.Count > 0)
            {
                DisplayGroupedPools();
            }
            else
            {
                EditorGUILayout.LabelField("No pools are currently initialized.");
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private void DisplayGroupedPools()
        {
            // Dictionary to hold prefab counts grouped by their respective groups
            Dictionary<string, Dictionary<string, int>> groupPrefabCounts = new Dictionary<string, Dictionary<string, int>>();

            // Gather prefab counts by group and name
            foreach (var groupEntry in poolManager.GroupPrefabLookup)
            {
                string groupName = groupEntry.Key;
                Dictionary<Object, ObjectPool<Object>> prefabPools = groupEntry.Value;

                // Initialize the dictionary for this group if it doesn't exist
                if (!groupPrefabCounts.ContainsKey(groupName))
                {
                    groupPrefabCounts[groupName] = new Dictionary<string, int>();
                }

                foreach (var prefabEntry in prefabPools)
                {
                    Object prefab = prefabEntry.Key;
                    ObjectPool<Object> pool = prefabEntry.Value;
                    int count = pool.Count;

                    if (groupPrefabCounts[groupName].ContainsKey(prefab.name))
                    {
                        groupPrefabCounts[groupName][prefab.name] += count;
                    }
                    else
                    {
                        groupPrefabCounts[groupName][prefab.name] = count;
                    }
                }
            }

            // Display grouped pools with total counts for each group
            EditorGUILayout.Space();
            if (groupPrefabCounts.Count == 0)
                return;

            foreach (var groupEntry in groupPrefabCounts)
            {
                string groupName = groupEntry.Key;
                EditorGUILayout.LabelField($"Group: {groupName}", EditorStyles.boldLabel);

                foreach (var prefabEntry in groupEntry.Value)
                {
                    string prefabName = prefabEntry.Key;
                    int totalCount = prefabEntry.Value;

                    EditorGUILayout.BeginHorizontal(); // Start a horizontal layout for buttons
                    EditorGUILayout.LabelField($"Prefab: {prefabName} - Total Count: {totalCount}");

                    // Add "Clean" button
                    if (GUILayout.Button("Clean", GUILayout.Width(60)))
                    {
                        poolManager.DestroyAllInGroup(groupName);
                    }

                    EditorGUILayout.EndHorizontal(); // End horizontal layout
                }
            }
        }
    }
}
#endif