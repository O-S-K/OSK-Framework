#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using OSK;

[CustomEditor(typeof(SingletonManager))]
public class SingletonManagerEditor : Editor
{
    private Vector2 _scrollPos;
    private string _searchFilter = "";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (SingletonManager.Instance == null) return;

        EditorGUILayout.Space();
        _searchFilter = EditorGUILayout.TextField("Filter (Type Name):", _searchFilter);
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        // Global
        var globals = SingletonManager.Instance.GetGlobalSingletons();
        EditorGUILayout.LabelField($"Global Singletons: {globals.Count}", EditorStyles.boldLabel);
        DrawSingletonList(globals);
        EditorGUILayout.Space();

        // Scene
        var scenes = SingletonManager.Instance.GetSceneSingletons();
        EditorGUILayout.LabelField($"Scene Singletons: {scenes.Count}", EditorStyles.boldLabel);
        DrawSingletonList(scenes);

        EditorGUILayout.Space();
        if (GUILayout.Button("Log All Singletons to Console"))
        {
            LogConsole(globals, scenes);
        }
        
        EditorGUILayout.EndScrollView();
    }

    private static void LogConsole(Dictionary<Type, SingletonInfo> globals, Dictionary<Type, SingletonInfo> scenes)
    {
        Debug.Log("=== Global Singletons ===");
        foreach (var kvp in globals)
        {
            Debug.Log($"{kvp.Key.Name} - InstanceID: {(kvp.Value.instance != null ? kvp.Value.instance.GetInstanceID().ToString() : "NULL")}");
        }

        Debug.Log("=== Scene Singletons ===");
        foreach (var kvp in scenes)
        {
            Debug.Log($"{kvp.Key.Name} - InstanceID: {(kvp.Value.instance != null ? kvp.Value.instance.GetInstanceID().ToString() : "NULL")}");
        }
    }


#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void OnEditorPlaymodeChanged()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (SingletonManager.Instance != null)
                {
                    SingletonManager.Instance.CleanAllSingletons();
                }
            }
        };
    }
#endif

    private void DrawSingletonList(Dictionary<Type, SingletonInfo> list)
    {
        if (list == null || list.Count == 0)
        {
            EditorGUILayout.LabelField("No singletons.");
            return;
        }

        var keysToRemove = new List<Type>();
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        foreach (var kvp in list)
        {
            var info = kvp.Value;
            bool isNull = info.instance == null;

            // Kiểm tra singleton có đang hợp lệ không
            bool isAllowedScene = info.IsGlobal || (info.allowedScenes?.Contains(activeScene) ?? false);

            // Màu nền: Xanh = hợp lệ, Vàng = null, Đỏ = không hợp lệ
            Color bgColor;
            if (isNull) bgColor = new Color(1f, 0.5f, 0.5f);
            else if (!isAllowedScene) bgColor = new Color(1f, 0.8f, 0.4f);
            else bgColor = new Color(0.6f, 1f, 0.6f);

            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = Color.white;

            // Danh sách scene
            string sceneList = info.IsGlobal ? "Global" :
                (info.allowedScenes != null && info.allowedScenes.Count > 0
                    ? string.Join(", ", info.allowedScenes)
                    : "<No Scene Restriction>");

            string label = kvp.Key.Name +
                           (info.IsGlobal ? " (Global)" : $" (Scenes: {sceneList})") +
                           $" [Created: {info.createdTime:HH:mm:ss}]";

            if (isNull)
                label += " [NULL]";
            else if (!isAllowedScene)
                label += " [INVALID SCENE]";

            // Tooltip
            string tooltip = $"Type: {kvp.Key.Name}\n" +
                             $"Created: {info.createdTime}\n" +
                             $"Scenes: {sceneList}\n" +
                             $"InstanceID: {(info.instance != null ? info.instance.GetInstanceID().ToString() : "NULL")}\n" +
                             $"Current Scene: {activeScene}";

            EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(320));

            if (!isNull)
            {
                EditorGUILayout.ObjectField(info.instance, typeof(MonoBehaviour), true);
                if (GUILayout.Button("Destroy", GUILayout.Width(60)))
                    keysToRemove.Add(kvp.Key);
            }

            EditorGUILayout.EndHorizontal();
        }

        // Xóa instance
        foreach (var key in keysToRemove)
        {
            var info = list[key];
            if (info.instance != null)
                DestroyImmediate(info.instance.gameObject);
            list.Remove(key);
        }
    }
}

#endif
