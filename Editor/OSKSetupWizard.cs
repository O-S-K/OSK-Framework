using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    public class OSKSetupWizard : EditorWindow
    {
        private const string WindowTitle = "OSK Quick Setup";
        private const string ProjectFolder = "Assets/_OSK";
        private const string ConfigFolder = ProjectFolder + "/Configs";
        private const string ExampleFolder = ProjectFolder + "/Examples";
        private const string ScriptFolder = ProjectFolder + "/Scripts";
        private const string UIScriptFolder = ScriptFolder + "/UI";
        private const string DataScriptFolder = ScriptFolder + "/Data";
        private const string EventScriptFolder = ScriptFolder + "/Events";
        private const string ProcedureScriptFolder = ScriptFolder + "/Procedures";
        private const string SceneFolder = ProjectFolder + "/Scenes";
        private const string PrefabFolder = ProjectFolder + "/Prefabs";
        private const string UIPrefabFolder = PrefabFolder + "/UI";
        private const string PoolPrefabFolder = PrefabFolder + "/Pool";
        private const string DataFolder = ProjectFolder + "/Data";
        private const string AudioFolder = ProjectFolder + "/Audio";
        private const string LocalizationFolder = ProjectFolder + "/Localization";
        private const string SheetFolder = ProjectFolder + "/Sheets";
        private const string ListViewPath = ConfigFolder + "/ListViewSO.asset";
        private const string ListSoundPath = ConfigFolder + "/ListSoundSO.asset";
        private const string InputConfigPath = ConfigFolder + "/InputConfigSO.asset";
        private const string QuickStartPath = ExampleFolder + "/OSKQuickStartExample.cs";
        private const string ModuleApiCheatSheetPath = ExampleFolder + "/OSKModuleApiCheatSheet.cs";
        private const string QuickUseGuidePath = "Assets/OSK-Framework/Docs/QuickUse.md";

        private SetupPreset _preset = SetupPreset.UIBasedGame;
        private Vector2 _scroll;
        private Main _cachedMain;
        private RootUI _cachedRootUI;
        private double _nextRefreshTime;
        private bool _showOneClickSetup = true;
        private bool _showGenerators = true;
        private bool _showRuntimeTools;
        private string _viewName = "MainMenuView";
        private string _saveDataName = "PlayerSaveData";
        private string _eventName = "GoldChangedEvent";
        private string _procedureName = "BootProcedure";
        private string _poolGroupName = "Gameplay";
        private string _poolKey = "Enemy";
        private string _soundId = "ButtonClick";
        private EViewType _viewType = EViewType.Popup;

        private enum SetupPreset
        {
            CoreOnly,
            UIBasedGame,
            FullFramework
        }

        [MenuItem("OSK-Framework/Setup/Quick Setup", false, 0)]
        public static void ShowWindow()
        {
            OSKSetupWizard window = GetWindow<OSKSetupWizard>(WindowTitle);
            window.minSize = new Vector2(560f, 640f);
            window.Show();
        }

        private void OnGUI()
        {
            RefreshCacheIfNeeded();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            DrawStatus();
            DrawModuleDashboard();
            DrawSetupActions();
            DrawGenerators();
            DrawRuntimeTools();
            DrawOpenTools();

            EditorGUILayout.EndScrollView();
        }

        private void OnFocus()
        {
            RefreshCache(true);
        }

        // Draws the window header.
        private void DrawHeader()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("OSK Framework Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose a preset, sync the Main hierarchy, and keep the useful setup/runtime controls close without filling the window.",
                MessageType.Info);
        }

        // Draws lightweight cached setup status.
        private void DrawStatus()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            DrawStatusPill("Main", _cachedMain != null);
            DrawStatusPill("RootUI", _cachedRootUI != null);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
            {
                RefreshCache(true);
            }
            EditorGUILayout.EndHorizontal();
        }

        // Draws preset controls and the selected module summary.
        private void DrawModuleDashboard()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);

            if (_cachedMain == null)
            {
                EditorGUILayout.HelpBox("Create Main first, then choose which modules this project uses.", MessageType.Warning);
                if (GUILayout.Button("Create OSK Framework", GUILayout.Height(32f)))
                {
                    _cachedMain = EnsureMain();
                    Selection.activeObject = _cachedMain;
                }
                return;
            }

            if (_cachedMain.mainModules == null)
            {
                _cachedMain.mainModules = new MainModules();
                EditorUtility.SetDirty(_cachedMain);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Core"))
            {
                ApplyPresetAndRefresh(SetupPreset.CoreOnly);
            }

            if (GUILayout.Button("UI Game"))
            {
                ApplyPresetAndRefresh(SetupPreset.UIBasedGame);
            }

            if (GUILayout.Button("Full"))
            {
                ApplyPresetAndRefresh(SetupPreset.FullFramework);
            }

            if (GUILayout.Button("None"))
            {
                ApplyModules(_cachedMain, ModuleType.None);
                RefreshCache(true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Selected", GetModuleSummary(_cachedMain.mainModules.Modules), EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync Selected Modules", GUILayout.Height(30f)))
            {
                SyncModulesOnMain();
                RefreshCache(true);
            }

            if (GUILayout.Button("Select Main", GUILayout.Height(30f)))
            {
                Selection.activeObject = _cachedMain;
                EditorGUIUtility.PingObject(_cachedMain);
            }
            EditorGUILayout.EndHorizontal();
        }

        // Draws runtime controls used while the scene is playing.
        private void DrawRuntimeTools()
        {
            EditorGUILayout.Space(12f);
            _showRuntimeTools = EditorGUILayout.Foldout(_showRuntimeTools, "Runtime", true);
            if (!_showRuntimeTools)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = EditorApplication.isPlaying;
            if (GUILayout.Button("Pause Framework"))
            {
                Main.SetPause(true);
            }

            if (GUILayout.Button("Resume Framework"))
            {
                Main.SetPause(false);
            }

            GUI.enabled = previousEnabled;
            if (GUILayout.Button("Select Main"))
            {
                Selection.activeObject = _cachedMain;
                EditorGUIUtility.PingObject(_cachedMain);
            }

            EditorGUILayout.EndHorizontal();
        }

        // Refreshes cached scene data at a controlled rate to keep OnGUI responsive.
        private void RefreshCacheIfNeeded()
        {
            if (EditorApplication.timeSinceStartup < _nextRefreshTime)
            {
                return;
            }

            RefreshCache(false);
        }

        // Refreshes cached scene data.
        private void RefreshCache(bool force)
        {
            if (!force && EditorApplication.timeSinceStartup < _nextRefreshTime)
            {
                return;
            }

            _cachedMain = FindMain();
            _cachedRootUI = FindObjectOfType<RootUI>();
            _nextRefreshTime = EditorApplication.timeSinceStartup + 0.75d;
        }

        // Draws a compact status badge.
        private static void DrawStatusPill(string label, bool isOk)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = isOk ? new Color(0.45f, 0.75f, 0.45f) : new Color(0.9f, 0.55f, 0.45f);
            GUILayout.Box((isOk ? "OK " : "NO ") + label, GUILayout.Width(120f), GUILayout.Height(22f));
            GUI.backgroundColor = previous;
        }

        // Applies a preset and refreshes the dashboard.
        private void ApplyPresetAndRefresh(SetupPreset preset)
        {
            _preset = preset;
            ApplyModulePreset(_cachedMain, preset);
            RefreshCache(true);
        }

        // Sets module flags through serialization because MainModules keeps the field private.
        private static void ApplyModules(Main main, ModuleType modulesValue)
        {
            if (main == null)
            {
                return;
            }

            if (main.mainModules == null)
            {
                main.mainModules = new MainModules();
            }

            SerializedObject serializedMain = new SerializedObject(main);
            SerializedProperty modules = serializedMain.FindProperty("mainModules._modules");
            if (modules != null)
            {
                modules.intValue = (int)modulesValue;
                serializedMain.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(main);
            EditorSceneManager.MarkSceneDirty(main.gameObject.scene);
        }

        // Draws setup controls.
        private void DrawSetupActions()
        {
            EditorGUILayout.Space(12f);
            _showOneClickSetup = EditorGUILayout.Foldout(_showOneClickSetup, "One Click Setup", true);
            if (!_showOneClickSetup)
            {
                return;
            }

            _preset = (SetupPreset)EditorGUILayout.EnumPopup("Preset", _preset);
            DrawPresetPreview(_preset);

            if (GUILayout.Button("Setup Current Scene", GUILayout.Height(34f)))
            {
                RunSetup(_preset);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create / Find Main"))
            {
                Selection.activeObject = EnsureMain();
            }

            if (GUILayout.Button("Create / Find RootUI"))
            {
                Selection.activeObject = EnsureRootUI();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Config Assets"))
            {
                EnsureConfigAssets();
            }

            if (GUILayout.Button("Sync Modules"))
            {
                SyncModulesOnMain();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Generate Quick Start Example"))
            {
                GenerateQuickStartExample(true);
            }

            if (GUILayout.Button("Open Module API Cheat Sheet"))
            {
                PingAsset(ModuleApiCheatSheetPath);
            }

            if (GUILayout.Button("Clean Duplicate Scene Objects"))
            {
                CleanDuplicateSceneObjects();
            }
        }

        // Draws copy-ready generators for the most common first project files.
        private void DrawGenerators()
        {
            EditorGUILayout.Space(12f);
            _showGenerators = EditorGUILayout.Foldout(_showGenerators, "Generators", true);
            if (!_showGenerators)
            {
                return;
            }

            EditorGUILayout.HelpBox("Generate the files people usually need first. Keep names as PascalCase, then move the code into your real gameplay folder if needed.", MessageType.None);

            EditorGUILayout.LabelField("UI", EditorStyles.boldLabel);
            _viewName = EditorGUILayout.TextField("View Class", _viewName);
            _viewType = (EViewType)EditorGUILayout.EnumPopup("View Type", _viewType);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create View Script"))
            {
                CreateViewScript(_viewName);
            }

            if (GUILayout.Button("Open Full View Creator"))
            {
                EditorApplication.ExecuteMenuItem("OSK-Framework/Tools/UI/Create View");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Code", EditorStyles.boldLabel);
            _saveDataName = EditorGUILayout.TextField("Save Data", _saveDataName);
            if (GUILayout.Button("Create Save Data Class"))
            {
                CreateSaveDataScript(_saveDataName);
            }

            _eventName = EditorGUILayout.TextField("Game Event", _eventName);
            if (GUILayout.Button("Create Game Event Class"))
            {
                CreateGameEventScript(_eventName);
            } 
        }

        // Draws shortcuts to existing OSK tools.
        private void DrawOpenTools()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Open Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Package Installer"))
            {
                OSK.Framework.Editor.OSKPackageInstaller.ShowWindow();
            }

            if (GUILayout.Button("Unity Package Manager"))
            {
                EditorApplication.ExecuteMenuItem("Window/Package Manager");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("UI Manager"))
            {
                EditorApplication.ExecuteMenuItem("OSK-Framework/UI/Window");
            }

            if (GUILayout.Button("Sound Manager"))
            {
                EditorApplication.ExecuteMenuItem("OSK-Framework/Sound/Window");
            }

            if (GUILayout.Button("Pool Debug"))
            {
                EditorApplication.ExecuteMenuItem("OSK-Framework/Pool/Debug Window");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Localization"))
            {
                EditorApplication.ExecuteMenuItem("OSK-Framework/Localization/Window");
            }

            if (GUILayout.Button("Data Storage"))
            {
                EditorApplication.ExecuteMenuItem("OSK-Framework/Storage/Window");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("README"))
            {
                PingAsset("Assets/OSK-Framework/README.md");
            }

            if (GUILayout.Button("Quick Use"))
            {
                PingAsset(QuickUseGuidePath);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Docs Folder"))
            {
                UnityEngine.Object docs = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/OSK-Framework/Docs");
                if (docs != null)
                {
                    Selection.activeObject = docs;
                    EditorGUIUtility.PingObject(docs);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // Shows which modules the selected preset will enable.
        private static void DrawPresetPreview(SetupPreset preset)
        {
            EditorGUILayout.HelpBox(
                GetPresetDescription(preset) + "\n\nModules: " + GetModuleSummary(GetModulesForPreset(preset)),
                MessageType.None);
        }

        // Generates the example files used by the dashboard.
        private static void GenerateExamplePack(bool overwriteQuickStart)
        {
            GenerateQuickStartExample(overwriteQuickStart);
            EnsureModuleApiCheatSheet();
            PingAsset(QuickStartPath);
        }

        // Creates a View script template.
        private void CreateViewScript(string className)
        {
            className = NormalizeClassName(className, "NewView");
            string path = UIScriptFolder + "/" + className + ".cs";
            string viewTypeName = "EViewType." + _viewType;
            string content =
$@"using OSK;
using UnityEngine;

public class {className} : View
{{
    // Cache components and register button callbacks here.
    protected override void OnInit()
    {{
        viewType = {viewTypeName};
    }}

    // Called by Main.UI.Open<{className}>() or Main.UI.Open<{className}>(data).
    public override bool Open(object data = null)
    {{
        return base.Open(data);
    }}

    // Called by Main.UI.Hide(Main.UI.Get<{className}>()) or HideAll().
    public override void Hide()
    {{
        base.Hide();
    }}
}}
";
            CreateScriptAsset(path, content);
        }

        // Creates a serializable save-data class template.
        private void CreateSaveDataScript(string className)
        {
            className = NormalizeClassName(className, "PlayerSaveData");
            string path = DataScriptFolder + "/" + className + ".cs";
            string fileName = ToSnakeCase(className) + ".json";
            string content =
$@"using System;

[Serializable]
public class {className}
{{
    public int level = 1;
    public int gold = 0;

    public static {className} Default()
    {{
        return new {className}
        {{
            level = 1,
            gold = 0
        }};
    }}
}}

// Usage:
// {className} data = OSK.Main.Data.Load(OSK.SaveType.Json, ""{fileName}"", {className}.Default());
// data.gold += 10;
// OSK.Main.Data.Save(OSK.SaveType.Json, ""{fileName}"", data);
";
            CreateScriptAsset(path, content);
        }

        // Creates a GameEvent class template.
        private void CreateGameEventScript(string className)
        {
            className = NormalizeClassName(className, "GoldChangedEvent");
            string path = EventScriptFolder + "/" + className + ".cs";
            string content =
$@"using OSK;

public sealed class {className} : GameEvent
{{
    public readonly int value;

    public {className}(int value)
    {{
        this.value = value;
    }}
}}

// Usage:
// Main.Event.Subscribe<{className}>(On{className});
// Main.Event.Publish(new {className}(10));
// Main.Event.Unsubscribe<{className}>(On{className});
";
            CreateScriptAsset(path, content);
        }
 

        // Creates a script asset if it does not exist.
        private static void CreateScriptAsset(string path, string content)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace("\\", "/"));
            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog(WindowTitle, "File already exists:\n" + path, "OK");
                PingAsset(path);
                return;
            }

            File.WriteAllText(path, content);
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();
            PingAsset(path);
        }

        // Normalizes user input into a safe class name.
        private static string NormalizeClassName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            string cleaned = new string(value.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(cleaned))
            {
                return fallback;
            }

            if (char.IsDigit(cleaned[0]))
            {
                cleaned = fallback + cleaned;
            }

            return char.ToUpperInvariant(cleaned[0]) + cleaned.Substring(1);
        }

        // Converts a class name into a simple file name.
        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "data";
            }

            string result = string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsUpper(c) && i > 0)
                {
                    result += "_";
                }

                result += char.ToLowerInvariant(c);
            }

            return result;
        }

        // Returns a human description for each preset.
        private static string GetPresetDescription(SetupPreset preset)
        {
            switch (preset)
            {
                case SetupPreset.CoreOnly:
                    return "CoreOnly is for gameplay prototypes that only need Main, events, data, resources, configs, mono ticks, and procedures.";
                case SetupPreset.FullFramework:
                    return "FullFramework enables every common runtime system, including entity, blackboard, and sheet data.";
                default:
                    return "UIBasedGame is the recommended start for most games: core systems, UI, sound, pool, command, localization, input, web request, director, and game init.";
            }
        }

        // Converts module flags into a readable list.
        private static string GetModuleSummary(ModuleType modules)
        {
            return string.Join(", ", Enum.GetValues(typeof(ModuleType))
                .Cast<ModuleType>()
                .Where(module => module != ModuleType.None && (modules & module) != 0)
                .Select(module => module.ToString()));
        }

        // Creates common folders for an OSK project.
        private static void EnsureStarterFolders()
        {
            EnsureFolder(ProjectFolder);
            EnsureFolder(ConfigFolder);
            EnsureFolder(ExampleFolder);
            EnsureFolder(ScriptFolder);
            EnsureFolder(UIScriptFolder);
            EnsureFolder(DataScriptFolder);
            EnsureFolder(EventScriptFolder);
            EnsureFolder(ProcedureScriptFolder);
            EnsureFolder(SceneFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(UIPrefabFolder);
            EnsureFolder(PoolPrefabFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(AudioFolder);
            EnsureFolder(LocalizationFolder);
            EnsureFolder(SheetFolder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Runs the full setup flow for the selected preset.
        private static void RunSetup(SetupPreset preset)
        {
            CleanDuplicateSceneObjects();
            EnsureStarterFolders();
            Main main = EnsureMain();
            EnsureRootUIIfNeeded(preset);
            CleanDuplicateSceneObjects();
            EnsureConfigAssets();
            AssignConfigAssets(main);
            ApplyModulePreset(main, preset);
            SyncModulesOnMain();
            GenerateExamplePack(false);

            EditorUtility.SetDirty(main);
            EditorSceneManager.MarkSceneDirty(main.gameObject.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = main;
            EditorGUIUtility.PingObject(main);
            EditorUtility.DisplayDialog(WindowTitle, "OSK setup completed. Select Main to review modules and config references.", "OK");
        }

        // Ensures the default framework prefab exists in the scene.
        internal static Main EnsureMain()
        {
            CleanDuplicateMainObjects();
            Main existing = FindMain();
            if (existing != null)
            {
                return existing;
            }

            Main prefab = Resources.LoadAll<Main>(string.Empty).FirstOrDefault();
            if (prefab != null)
            {
                Main instance = InstantiatePrefabComponent(prefab);
                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance.gameObject, "Create OSK Framework");
                    return instance;
                }
            }

            GameObject obj = new GameObject("======== [OSK Framework] ==========");
            Undo.RegisterCreatedObjectUndo(obj, "Create OSK Framework");
            return obj.AddComponent<Main>();
        }

        // Ensures RootUI exists when the selected preset needs UI.
        private static void EnsureRootUIIfNeeded(SetupPreset preset)
        {
            if (preset == SetupPreset.CoreOnly)
            {
                return;
            }

            EnsureRootUI();
            EnsureEventSystemManager();
        }

        // Ensures the default RootUI prefab exists in the scene.
        internal static RootUI EnsureRootUI()
        {
            CleanDuplicateRootUIObjects();
            RootUI existing = FindObjectOfType<RootUI>();
            if (existing != null)
            {
                return existing;
            }

            RootUI prefab = Resources.LoadAll<RootUI>(string.Empty).FirstOrDefault();
            if (prefab != null)
            {
                RootUI instance = InstantiatePrefabComponent(prefab);
                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance.gameObject, "Create OSK RootUI");
                    return instance;
                }
            }

            GameObject obj = new GameObject("RootUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(obj, "Create OSK RootUI");
            Canvas canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return obj.AddComponent<RootUI>();
        }

        // Ensures the scene has the OSK EventSystem helper for UI input.
        private static void EnsureEventSystemManager()
        {
            if (FindObjectOfType<EventSystemManager>() != null)
            {
                return;
            }

            GameObject obj = new GameObject("EventSystemManager");
            Undo.RegisterCreatedObjectUndo(obj, "Create OSK EventSystemManager");
            obj.AddComponent<EventSystemManager>();
        }

        // Removes duplicate generated scene objects while keeping prefab instances first.
        internal static void CleanDuplicateSceneObjects()
        {
            CleanDuplicateMainObjects();
            CleanDuplicateRootUIObjects();
        }

        // Removes duplicate Main objects and keeps the best candidate.
        internal static void CleanDuplicateMainObjects()
        {
            Main[] mains = FindObjectsOfType<Main>(true);
            Main keep = PickBestMain(mains);
            DestroyDuplicates(mains, keep, "OSK Framework");
        }

        // Removes duplicate RootUI objects and keeps the best candidate.
        internal static void CleanDuplicateRootUIObjects()
        {
            RootUI[] roots = FindObjectsOfType<RootUI>(true);
            RootUI keep = PickBestRootUI(roots);
            DestroyDuplicates(roots, keep, "OSK RootUI");
        }

        // Picks a Main prefab instance when possible.
        private static Main PickBestMain(Main[] mains)
        {
            if (mains == null || mains.Length == 0)
            {
                return null;
            }

            return mains.FirstOrDefault(IsPrefabInstance) ?? mains[0];
        }

        // Picks a RootUI prefab instance when possible.
        private static RootUI PickBestRootUI(RootUI[] roots)
        {
            if (roots == null || roots.Length == 0)
            {
                return null;
            }

            return roots.FirstOrDefault(IsPrefabInstance) ?? roots[0];
        }

        // Destroys every duplicate except the kept component.
        private static void DestroyDuplicates<T>(T[] components, T keep, string label) where T : Component
        {
            if (components == null || components.Length <= 1 || keep == null)
            {
                return;
            }

            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null || component == keep)
                {
                    continue;
                }

                string objectName = component.name;
                Undo.DestroyObjectImmediate(component.gameObject);
                Debug.Log("[OSK Setup] Removed duplicate " + label + ": " + objectName);
            }
        }

        // Instantiates a prefab component and returns the component on the instance.
        private static T InstantiatePrefabComponent<T>(T prefabComponent) where T : Component
        {
            UnityEngine.Object instance = PrefabUtility.InstantiatePrefab(prefabComponent);
            if (instance is T component)
            {
                return component;
            }

            if (instance is GameObject gameObject)
            {
                return gameObject.GetComponent<T>();
            }

            return null;
        }

        // Checks whether a component belongs to a prefab instance.
        private static bool IsPrefabInstance(Component component)
        {
            return component != null && PrefabUtility.GetPrefabInstanceStatus(component.gameObject) != PrefabInstanceStatus.NotAPrefab;
        }

        // Creates default OSK config assets under Assets/_OSK.
        private static void EnsureConfigAssets()
        {
            EnsureFolder(ProjectFolder);
            EnsureFolder(ConfigFolder);

            CreateAssetIfMissing<ListViewSO>(ListViewPath);
            CreateAssetIfMissing<ListSoundSO>(ListSoundPath);
            CreateAssetIfMissing<InputConfigSO>(InputConfigPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Assigns default config assets to Main.configInit.
        private static void AssignConfigAssets(Main main)
        {
            if (main == null)
            {
                return;
            }

            if (main.configInit == null)
            {
                main.configInit = new ConfigInit();
            }

            if (main.configInit.data == null)
            {
                main.configInit.data = new DataConfigs();
            }

            main.configInit.data.listViewS0 = FindOrLoadAsset<ListViewSO>(ListViewPath);
            main.configInit.data.listSoundSo = FindOrLoadAsset<ListSoundSO>(ListSoundPath);
            main.configInit.data.inputConfigSO = FindOrLoadAsset<InputConfigSO>(InputConfigPath);
            EditorUtility.SetDirty(main);
        }

        // Applies a recommended module selection preset to Main.
        private static void ApplyModulePreset(Main main, SetupPreset preset)
        {
            if (main == null)
            {
                return;
            }

            if (main.mainModules == null)
            {
                main.mainModules = new MainModules();
            }

            SerializedObject serializedMain = new SerializedObject(main);
            SerializedProperty modules = serializedMain.FindProperty("mainModules._modules");
            if (modules == null)
            {
                Debug.LogWarning("[OSK Setup] Cannot find Main.mainModules._modules serialized field.");
                return;
            }

            modules.intValue = (int)GetModulesForPreset(preset);
            serializedMain.ApplyModifiedProperties();
            EditorUtility.SetDirty(main);
        }

        // Returns module flags for a setup preset.
        private static ModuleType GetModulesForPreset(SetupPreset preset)
        {
            ModuleType core =
                ModuleType.MonoManager |
                ModuleType.ObserverManager |
                ModuleType.EventBusManager |
                ModuleType.ResourceManager |
                ModuleType.DataManager |
                ModuleType.GameConfigsManager |
                ModuleType.ProcedureManager;

            if (preset == SetupPreset.CoreOnly)
            {
                return core;
            }

            ModuleType uiGame =
                core |
                ModuleType.PoolManager |
                ModuleType.CommandManager |
                ModuleType.DirectorManager |
                ModuleType.WebRequestManager |
                ModuleType.UIManager |
                ModuleType.SoundManager |
                ModuleType.LocalizationManager |
                ModuleType.InputDeviceManager |
                ModuleType.GameInit;

            if (preset == SetupPreset.UIBasedGame)
            {
                return uiGame;
            }

            return
                uiGame |
                ModuleType.EntityManager |
                ModuleType.BlackboardManager |
                ModuleType.DataSheetManager;
        }

        // Calls Main.SyncModules for the scene Main.
        private static void SyncModulesOnMain()
        {
            Main main = EnsureMain();
            //main.SyncModules();
            EditorUtility.SetDirty(main);
            EditorSceneManager.MarkSceneDirty(main.gameObject.scene);
        }

        // Generates a complete code example that shows the common OSK access path.
        private static void GenerateQuickStartExample(bool overwrite)
        {
            EnsureFolder(ProjectFolder);
            EnsureFolder(ExampleFolder);

            if (!overwrite && File.Exists(QuickStartPath))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(QuickStartPath);
                EditorGUIUtility.PingObject(Selection.activeObject);
                return;
            }

            File.WriteAllText(QuickStartPath, GetQuickStartExampleContent());
            AssetDatabase.ImportAsset(QuickStartPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<MonoScript>(QuickStartPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        // Creates the module cheat sheet when it has not been generated yet.
        private static void EnsureModuleApiCheatSheet()
        {
            EnsureFolder(ProjectFolder);
            EnsureFolder(ExampleFolder);

            if (File.Exists(ModuleApiCheatSheetPath))
            {
                return;
            }

            File.WriteAllText(ModuleApiCheatSheetPath, GetModuleApiCheatSheetContent());
            AssetDatabase.ImportAsset(ModuleApiCheatSheetPath);
        }

        // Creates a status line.
        private static void DrawStatusRow(string label, bool isOk)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(isOk ? "OK" : "Missing", GUILayout.Width(64f));
            EditorGUILayout.LabelField(label);
            EditorGUILayout.EndHorizontal();
        }

        // Finds Main in the active scene.
        private static Main FindMain()
        {
            return FindObjectOfType<Main>();
        }

        // Finds or loads an asset by path, falling back to project search.
        private static T FindOrLoadAsset<T>(string preferredPath) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(preferredPath);
            if (asset != null)
            {
                return asset;
            }

            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length == 0)
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // Creates a ScriptableObject asset when it does not exist yet.
        private static T CreateAssetIfMissing<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T found = FindOrLoadAsset<T>(path);
            if (found != null)
            {
                return found;
            }

            T asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // Ensures an Assets folder path exists.
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent.Replace("\\", "/"));
                AssetDatabase.CreateFolder(parent.Replace("\\", "/"), folder);
            }
        }

        // Selects and pings an asset in the Project window.
        private static void PingAsset(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
            {
                Debug.LogWarning("[OSK Setup] Asset not found: " + path);
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        // Returns fallback cheat-sheet content when the generated file is missing.
        private static string GetModuleApiCheatSheetContent()
        {
            return
@"using OSK;
using UnityEngine;

// OSK Module API Cheat Sheet
// Open OSK-Framework -> Setup -> Quick Setup, run UIBasedGame or FullFramework, then copy only what you need.
//
// Main:
// Main.Inject(this);
// Main.Mono.AddUpdate(myUpdateable);
// Main.Event.Subscribe<MyEvent>(OnEvent);
// Main.Observer.Add(""Topic.Name"", OnMessage);
// Main.Data.Save(SaveType.Json, ""save.json"", data);
// Main.Data.Load(SaveType.Json, ""save.json"", defaultData);
// Main.UI.Open<MyView>(viewData);
// Main.Sound.Play(soundId);
// Main.Pool.SpawnByKey<GameObject>(""Enemy"");
// Main.Resource.LoadAssetAsync<GameObject>(""Address"");
// Main.Procedure.ChangeProcedure<MyProcedure>();
// Main.Command.Execute(new MyCommand());
// Main.Localization.GetText(""ui.start"");

public class OSKModuleApiCheatSheet : MonoBehaviour
{
    // This file is intentionally comment-first.
    // The full cheat sheet can be expanded by adding module-specific snippets here.
}
";
        }

        // Returns quick-start code content.
        private static string GetQuickStartExampleContent()
        {
            return
@"using OSK;
using System;
using UnityEngine;

// OSK Quick Start Checklist:
// 1. Open OSK-Framework -> Setup -> Quick Setup.
// 2. Use UIBasedGame or FullFramework if you want to run every sample below.
// 3. Add UI prefabs to ListViewSO before calling Main.UI.Open<T>().
// 4. Add sound ids to ListSoundSO before calling Main.Sound.Play(id).
// 5. Add pool keys to PoolManager before calling Main.Pool.SpawnByKey<T>().
// 6. Create BlackboardData assets before calling Main.Blackboard.Create(...).
// 7. Open OSKModuleApiCheatSheet.cs when you need a bigger API map by module.
// 8. Move only the patterns you need into your real gameplay scripts.

public class OSKQuickStartExample : MonoBehaviour
{
    [Header(""Optional scene references"")]
    [SerializeField] private Transform spawnParent;

    private const string SaveFile = ""osk_quick_start_save.json"";
    private OSKQuickTicker ticker;

    [InjectModule] private DataManager injectedData;
    [InjectModule] private EventBusManager injectedEvent;

    private void Awake()
    {
        // Optional: field injection for enabled modules.
        Main.Inject(this);
    }

    private void OnEnable()
    {
        // EventBus: subscribe when enabled, unsubscribe when disabled.
        Main.Event.Subscribe<OSKQuickGoldChangedEvent>(OnGoldChanged, receiveLastIfExists: true);

        // Observer: lightweight topic callbacks for local gameplay messages.
        Main.Observer.Add(""QuickStart.LevelUp"", OnLevelUp);
    }

    private void Start()
    {
        // 1. Data: load default data, modify it, then save it.
        OSKQuickPlayerSaveData save = Main.Data.Load(SaveType.Json, SaveFile, OSKQuickPlayerSaveData.Default());
        save.gold += 10;
        save.lastLoginUtc = DateTime.UtcNow.ToString(""O"");
        Main.Data.Save(SaveType.Json, SaveFile, save);

        // 2. EventBus: notify any system/UI that gold changed.
        Main.Event.Publish(new OSKQuickGoldChangedEvent(save.gold));

        // Observer: notify simple topic listeners.
        Main.Observer.Notify(""QuickStart.LevelUp"", save.level + 1);

        // 3. Command: execute gameplay actions that can be undone.
        Main.Command.Create(""MovePlayer"", new OSKQuickMoveCommand(transform, transform.position + Vector3.right));
        // Main.Command.Undo(""MovePlayer"");

        // 4. Procedure: run high-level flow states after enabling ProcedureManager.
        // Main.Procedure.RunProcedureNode<OSKQuickBootProcedure>();

        // 5. Blackboard: create a BlackboardData asset first, then create/get a runtime blackboard.
        // Blackboard board = Main.Blackboard.Create(""Player"", playerBlackboardData, gameObject);
        // board.SetValue(""HP"", 100);
        // board.Subscribe<int>(""HP"", hp => Debug.Log(""HP: "" + hp));

        // 6. UI: after creating a View prefab and adding it to ListViewSO.
        // Main.UI.Open<MainMenuView>();
        // Main.UI.Open<OSKQuickMenuView>(new OSKQuickMenuData { title = ""Main Menu"" });
        // Main.UI.TryOpen<MainMenuView>();
        // Main.UI.HideAll();

        // 7. Sound: after adding an id to ListSoundSO.
        // Main.Sound.Play(""ButtonClick"");
        // Main.Sound.StopWithFade(SoundType.MUSIC, 0.5f);

        // 8. Pool: after adding a PoolItemData key in PoolManager.
        // GameObject bullet = Main.Pool.SpawnByKey<GameObject>(""Bullet"", spawnParent);
        // Main.Pool.Despawn(bullet, delay: 2f);

        // 9. Centralized tick: one Unity Update in Main drives this object.
        ticker = new OSKQuickTicker();
        Main.Mono.Register(ticker);

        // 10. Common access patterns.
        // Main.Res.Load<GameObject>(""Prefabs/Enemy"");
        // Main.Localization.SwitchLanguage(SystemLanguage.English);
        // Main.DataSheet.GetSheet<YourSheet>();
        // Main.Data.SaveAsync(SaveType.Json, SaveFile, save).Forget();
    }

    private void OnDisable()
    {
        Main.Event.Unsubscribe<OSKQuickGoldChangedEvent>(OnGoldChanged);
        Main.Observer.Remove(""QuickStart.LevelUp"", OnLevelUp);
    }

    private void OnDestroy()
    {
        if (ticker != null)
        {
            Main.Mono.UnRegister(ticker);
            ticker = null;
        }
    }

    private void OnGoldChanged(OSKQuickGoldChangedEvent evt)
    {
        Debug.Log(""Gold changed: "" + evt.gold);
    }

    private void OnLevelUp(object data)
    {
        if (data is int level)
        {
            Debug.Log(""Level up: "" + level);
        }
    }
}

[Serializable]
public class OSKQuickPlayerSaveData
{
    public int level;
    public int gold;
    public string playerName;
    public string lastLoginUtc;

    public static OSKQuickPlayerSaveData Default()
    {
        return new OSKQuickPlayerSaveData
        {
            level = 1,
            gold = 0,
            playerName = ""Player"",
            lastLoginUtc = DateTime.UtcNow.ToString(""O"")
        };
    }
}

public class OSKQuickGoldChangedEvent : GameEvent
{
    public readonly int gold;

    public OSKQuickGoldChangedEvent(int gold)
    {
        this.gold = gold;
    }
}

public class OSKQuickTicker : IUpdateable
{
    private float elapsed;

    public void OnUpdate()
    {
        elapsed += Time.deltaTime;
        if (elapsed < 1f)
        {
            return;
        }

        elapsed = 0f;
        // Put lightweight repeated logic here instead of adding many MonoBehaviour.Update methods.
    }
}

public class OSKQuickMoveCommand : ICommand
{
    private readonly Transform target;
    private readonly Vector3 from;
    private readonly Vector3 to;

    public OSKQuickMoveCommand(Transform target, Vector3 to)
    {
        this.target = target;
        this.from = target != null ? target.position : Vector3.zero;
        this.to = to;
    }

    public void Execute()
    {
        if (target != null)
        {
            target.position = to;
        }
    }

    public void Undo()
    {
        if (target != null)
        {
            target.position = from;
        }
    }
}

public class OSKQuickBootProcedure : ProcedureNode
{
    public override void OnEnter(ProcedureProcessor processor)
    {
        Debug.Log(""Boot procedure: load configs, save data, localization, then go lobby."");
        ChangeState<OSKQuickLobbyProcedure>(processor);
    }
}

public class OSKQuickLobbyProcedure : ProcedureNode
{
    public override void OnEnter(ProcedureProcessor processor)
    {
        Debug.Log(""Lobby procedure: open lobby UI, play lobby BGM, wait for player input."");
        // Main.UI.Open<OSKQuickMenuView>();
        // Main.Sound.Play(""BGM_Lobby"", loop: true);
    }
}

public class OSKQuickPoolable : MonoBehaviour, IPoolable
{
    private TrailRenderer trail;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
    }

    public void OnSpawn()
    {
        gameObject.SetActive(true);
        if (trail != null)
        {
            trail.Clear();
        }
    }

    public void OnDespawn()
    {
        if (trail != null)
        {
            trail.Clear();
        }
    }
}

public class OSKQuickMenuData
{
    public string title;
}

public class OSKQuickMenuView : View
{
    private OSKQuickMenuData menuData;

    protected override void OnInit()
    {
        // Cache buttons/text/images here.
    }

    protected override bool OnValidateData(object data)
    {
        return data == null || data is OSKQuickMenuData;
    }

    protected override void SetData(object data = null)
    {
        base.SetData(data);
        menuData = data as OSKQuickMenuData;
        if (menuData != null)
        {
            Debug.Log(""Open menu: "" + menuData.title);
        }
    }
}
";
        }
    }
}
