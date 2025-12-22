#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using UnityEditor.SceneManagement;

namespace OSK
{
    public class CreateViewEditorWindow : EditorWindow
    {
        private static string scriptName = "NewView";
        private static EViewType viewType = EViewType.Popup;
        private static int depth = 0;
        private static bool isAlertView = false;
        private static bool createShield = true;
        private static string folderPath = "Assets/Scripts";

        private static GameObject createdView;
        private const string PREF_KEY = "CreateViewEditor_";

        [MenuItem("OSK-Framework/Tools/UI/Create View")]
        public static void ShowWindow()
        {
            GetWindow<CreateViewEditorWindow>("Create View");
        }

        private void OnEnable()
        {
            scriptName = EditorPrefs.GetString(PREF_KEY + "ScriptName", "NewView");
            viewType = (EViewType)EditorPrefs.GetInt(PREF_KEY + "ViewType", 0);
            depth = EditorPrefs.GetInt(PREF_KEY + "Depth", 0);
            isAlertView = EditorPrefs.GetBool(PREF_KEY + "IsAlertView", false);
            createShield = EditorPrefs.GetBool(PREF_KEY + "CreateShield", true);
            folderPath = EditorPrefs.GetString(PREF_KEY + "FolderPath", "Assets/Scripts");
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(PREF_KEY + "ScriptName", scriptName);
            EditorPrefs.SetInt(PREF_KEY + "ViewType", (int)viewType);
            EditorPrefs.SetInt(PREF_KEY + "Depth", depth);
            EditorPrefs.SetBool(PREF_KEY + "IsAlertView", isAlertView);
            EditorPrefs.SetBool(PREF_KEY + "CreateShield", createShield);
            EditorPrefs.SetString(PREF_KEY + "FolderPath", folderPath);
        }

        private void OnGUI()
        {
            GUILayout.Label("Create New View", EditorStyles.boldLabel);
            scriptName = EditorGUILayout.TextField("Script Name", scriptName);
            viewType = (EViewType)EditorGUILayout.EnumPopup("View Type", viewType);
            depth = EditorGUILayout.IntField("Depth", depth);
            isAlertView = EditorGUILayout.Toggle("Is Alert View", isAlertView);
            createShield = EditorGUILayout.Toggle("Create Shield", createShield);

            EditorGUILayout.Space();
            GUILayout.Label("Step-by-step Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("1. Create Script"))
            {
                CreateScriptOnly();
            }

            if (GUILayout.Button("2. Create View Prefab"))
            {
                CreateViewPrefabOnly();
            }

            if (GUILayout.Button("3. Attach Script To View"))
            {
                TryAttachScript();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Select Folder Path", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
            if (GUILayout.Button("Select Folder"))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                    {
                        folderPath = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        MyLogger.LogError("Please select a folder inside the Assets folder.");
                    }
                }
            }

            if (GUILayout.Button("Open Folder"))
            {
                EditorUtility.RevealInFinder(folderPath);
            }

            if (GUILayout.Button("Clear"))
            {
                folderPath = "";
            }

            if (GUILayout.Button("Reset Settings"))
            {
                EditorPrefs.DeleteKey(PREF_KEY + "ScriptName");
                EditorPrefs.DeleteKey(PREF_KEY + "ViewType");
                EditorPrefs.DeleteKey(PREF_KEY + "Depth");
                EditorPrefs.DeleteKey(PREF_KEY + "IsAlertView");
                EditorPrefs.DeleteKey(PREF_KEY + "CreateShield");
                EditorPrefs.DeleteKey(PREF_KEY + "FolderPath");

                scriptName = "NewView";
                viewType = EViewType.Popup;
                depth = 0;
                isAlertView = false;
                createShield = true;
                folderPath = "Assets/Scripts";

                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateScriptOnly()
        {
            string scriptPath = Path.Combine(folderPath, scriptName + ".cs");
            if (File.Exists(scriptPath))
            {
                MyLogger.LogWarning("Script already exists.");
                return;
            }

            string viewType = isAlertView ? "AlertView" : "View";
            string scriptTemplate = $@"
using UnityEngine;
using OSK;

public class {scriptName} : {viewType}
{{
    public override void Initialize(RootUI rootUI)
    {{
        base.Initialize(rootUI);
    }}

    public override void Open(object[] data = null)
    {{
        base.Open(data);
    }}

    public override void Hide()
    {{
        base.Hide();
    }}
}}";

            Directory.CreateDirectory(folderPath); // đảm bảo folder tồn tại
            File.WriteAllText(scriptPath, scriptTemplate);
            AssetDatabase.ImportAsset(scriptPath);
            AssetDatabase.Refresh();

            MyLogger.Log($"Created script at: {scriptPath}");
        }

        private void CreateViewPrefabOnly()
        {
            Transform parent = FindObjectOfType<RootUI>()?.ViewContainer?.transform;
            createdView = CreateGORectTransform(scriptName, parent);
            if (createShield)
            {
                CreateShield(createdView);
            }
            CreateContainer(createdView);


            Selection.activeGameObject = createdView;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            MyLogger.Log("Created view GameObject in scene.");
        }

        private void TryAttachScript()
        {
            if (createdView == null)
            {
                MyLogger.LogError("View GameObject not created yet.");
                return;
            }

            string scriptAssetPath = Path.Combine(folderPath, scriptName + ".cs").Replace("\\", "/");
            MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptAssetPath);
            if (monoScript == null)
            {
                MyLogger.LogError("Script not found. Try refreshing or check path.");
                return;
            }

            Type scriptType = monoScript.GetClass();
            if (scriptType == null)
            {
                MyLogger.LogWarning("Script not compiled yet. Will retry after compile.");
                EditorApplication.update += WaitForCompilation;
                return;
            }

            AttachScriptToView(scriptType);
        }

        private static void WaitForCompilation()
        {
            if (EditorApplication.isCompiling)
                return;

            EditorApplication.update -= WaitForCompilation;

            string scriptPath = Path.Combine(folderPath, scriptName + ".cs").Replace("\\", "/");
            MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (monoScript == null)
            {
                MyLogger.LogError($"Failed to load script at {scriptPath}");
                return;
            }

            Type scriptType = monoScript.GetClass();
            if (scriptType == null)
            {
                MyLogger.LogError("Failed to get script class after compilation.");
                return;
            }

            AttachScriptToView(scriptType);
        }

        private static void AttachScriptToView(Type type)
        {
            if (createdView == null)
            {
                MyLogger.LogError("No view to attach script to.");
                return;
            }

            if (createdView.GetComponent(type) == null)
            {
                createdView.AddComponent(type);
                createdView.GetComponent<View>().viewType = viewType;
                createdView.GetComponent<View>().depthEdit = depth;
                MyLogger.Log("Script attached to view.");
            }
            else
            {
                MyLogger.Log("Script already attached.");
            }
        }

        private static void CreateContainer(GameObject view)
        {
            CreateGORectTransform("Container", view.transform);
        }

        private static void CreateShield(GameObject view)
        {
            var shield = CreateGORectTransform("Shield", view.transform);
            UnityEngine.UI.Image img = shield.GetOrAdd<UnityEngine.UI.Image>();
            img.color = new Color(0, 0, 0, 0.9f);
        }

        private static GameObject CreateGORectTransform(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform shield = go.AddComponent<RectTransform>();
            shield.gameObject.AddComponent<CanvasRenderer>();
            shield.SetParent(parent, false);
            shield.transform.SetAsFirstSibling();
            shield.pivot = new Vector2(0.5f, 0.5f);
            shield.localPosition = Vector3.zero;
            shield.localScale = Vector3.one;
            shield.sizeDelta = Vector2.zero;
            shield.anchorMin = Vector2.zero;
            shield.anchorMax = Vector2.one;
            shield.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }
    }
}
#endif