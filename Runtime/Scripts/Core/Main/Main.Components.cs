using System;
using System.Linq;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    /// <summary>
    /// Main class of the framework, contains all the components of the framework.
    /// </summary>
    public partial class Main
    {
        // Safe Accessors with Guards
        public static MonoManager Mono => EnsureModule<MonoManager>();
        public static ObserverManager Observer => EnsureModule<ObserverManager>();
        public static EventBusManager Event => EnsureModule<EventBusManager>();
        public static PoolManager Pool => EnsureModule<PoolManager>();
        public static CommandManager Command => EnsureModule<CommandManager>();
        public static DirectorManager Director => EnsureModule<DirectorManager>();
        public static ResourceManager Res => EnsureModule<ResourceManager>();
        public static DataManager Data => EnsureModule<DataManager>();
        public static WebRequestManager WebRequest => EnsureModule<WebRequestManager>();
        public static GameConfigsManager Configs => EnsureModule<GameConfigsManager>();
        public static UIManager UI => EnsureModule<UIManager>();
        public static SoundManager Sound => EnsureModule<SoundManager>();
        public static LocalizationManager Localization => EnsureModule<LocalizationManager>();
        public static EntityManager Entity => EnsureModule<EntityManager>();
        public static BlackboardManager Blackboard => EnsureModule<BlackboardManager>();
        public static ProcedureManager Procedure => EnsureModule<ProcedureManager>();
        public static DataSheetManager DataSheet => EnsureModule<DataSheetManager>();
        public static InputDeviceManager InputDevice => EnsureModule<InputDeviceManager>();
        public static GameInit GameInit => EnsureModule<GameInit>();

        private static readonly Dictionary<Type, GameFrameworkComponent> k_Modules = new();

        private static T EnsureModule<T>() where T : GameFrameworkComponent
        {
            if (k_Modules.TryGetValue(typeof(T), out var module))
            {
                return (T)module;
            }

            throw new Exception(
                $"[OSK Framework] Module '{typeof(T).Name}' is not ready. " +
                "Fix: open OSK-Framework -> Setup -> Quick Setup, choose a preset that includes this module, then click Sync Selected Modules or Setup Current Scene.");
        }


        [HideLabel, InlineProperty] public ConfigInit configInit;

        [HideLabel, InlineProperty] public MainModules mainModules;

        [Title("ðŸš€ Entry Point")] [TypeFilter("GetProcedureTypes")] [SerializeField]
        private Type _entranceProcedure;

        public bool isDestroyingOnLoad = false;

        private IEnumerable<Type> GetProcedureTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ProcedureNode).IsAssignableFrom(p) && !p.IsAbstract);
        }

        public static Main Instance => SingletonManager.Instance.Get<Main>();

        protected void Awake()
        {
            gameObject.name = "======== [OSK Framework] ==========";

            if (configInit != null) 
            {
                MyLogger.IsLogEnabled = configInit.IsEnableLogg;
            }

            SingletonManager.Instance.RegisterGlobal(this);
            if (isDestroyingOnLoad)
                DontDestroyOnLoad(gameObject);

            InitModules();
            InitDataComponents();
            InitConfigs();
        }

        
        public void SyncModules()
        {
#if UNITY_EDITOR
            var children = new List<GameObject>();
            foreach (Transform child in transform) children.Add(child.gameObject);
            foreach (var child in children)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(child.name, @"^\d+\..+"))
                {
                    UnityEditor.Undo.DestroyObjectImmediate(child);
                }
            }
            var moduleValues = (ModuleType[])Enum.GetValues(typeof(ModuleType));
            Array.Sort(moduleValues, (a, b) => ((int)a).CompareTo((int)b));

            int index = 0;
            foreach (ModuleType moduleType in moduleValues)
            {
                if (moduleType == ModuleType.None || (mainModules.Modules & moduleType) == 0) continue;

                var moduleName = moduleType.ToString();
                var componentType = mainModules.GetComponentType(moduleName);

                if (componentType != null)
                {
                    index++;
                    var newObject = new GameObject($"{index}.{moduleName}");
                    newObject.transform.SetParent(transform);
                    newObject.AddComponent(componentType);

                    UnityEditor.Undo.RegisterCreatedObjectUndo(newObject, "Sync Module " + moduleName);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[Main] Modules synchronized successfully in hierarchy.");
#endif
        }

        private void InitModules()
        {
            var moduleValues = (ModuleType[])Enum.GetValues(typeof(ModuleType));
            Array.Sort(moduleValues, (a, b) => ((int)a).CompareTo((int)b));
            int index = 0;
            foreach (ModuleType moduleType in moduleValues)
            {
                if (moduleType == ModuleType.None || (mainModules.Modules & moduleType) == 0) continue;

                var moduleName = moduleType.ToString();
                var componentType = mainModules.GetComponentType(moduleName);

                if (componentType != null)
                {
                    index++;
                    var expectedName = $"{index}.{moduleName}";
                    var child = transform.Find(expectedName);
                    GameFrameworkComponent module = null;

                    if (child != null)
                    {
                        module = child.GetComponent(componentType) as GameFrameworkComponent;
                        if (module == null)
                            module = child.gameObject.AddComponent(componentType) as GameFrameworkComponent;
                        MyLogger.Log($"[Main] Reusing existing module object: {expectedName}");
                    }
                    else
                    {
                        var newObject = new GameObject(expectedName);
                        newObject.transform.SetParent(transform);
                        module = newObject.AddComponent(componentType) as GameFrameworkComponent;
                        MyLogger.Log($"[Main] Created new module object: {expectedName}");
                    }

                    if (module != null) AutoAssignModule(module);
                }
                else
                {
                    MyLogger.LogError(
                        $"[Main] Implementation for module '{moduleName}' was not found. " +
                        "Fix: make sure the module enum name matches an OSK component class name, then open OSK-Framework -> Setup -> Quick Setup and click Sync Selected Modules.");
                }
            }
        }

        private void AutoAssignModule(GameFrameworkComponent module)
        {
            var type = module.GetType();
            k_Modules[type] = module;

            foreach (var iface in type.GetInterfaces())
                k_Modules[iface] = module;
        }

        /// <summary>
        /// Main.Inject(this);
        /// </summary>
        public static void Inject(object target)
        {
            if (target == null) return;

            var fields = target.GetType().GetFields(System.Reflection.BindingFlags.Public |
                                                    System.Reflection.BindingFlags.NonPublic |
                                                    System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttributes(typeof(InjectModuleAttribute), true);
                if (attr.Length > 0)
                {
                    var moduleType = field.FieldType;
                    if (k_Modules.TryGetValue(moduleType, out var module))
                    {
                        field.SetValue(target, module);
                    }
                    else
                    {
                        MyLogger.LogError(
                            $"[Inject] Cannot find module '{moduleType.Name}' for {target.GetType().Name}. " +
                            "Fix: enable that module in OSK-Framework -> Setup -> Quick Setup, click Sync Selected Modules, then run the scene again.");
                    }
                }
            }
        }


        private void InitDataComponents()
        {
            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                var component = current.Value;
                if (component != null)
                {
                    var deps = component.GetDependencies();
                    foreach (var dep in deps)
                    {
                    if (GetModule(dep) == null)
                    {
                        MyLogger.LogError(
                                $"[Dependency] Module '{component.GetType().Name}' requires '{dep.Name}' but it is missing. " +
                                "Fix: open OSK-Framework -> Setup -> Quick Setup, choose a preset that includes the dependency, then click Sync Selected Modules.");
                    }
                }
                }

                current = current.Next;
            }

            current = SGameFrameworkComponents.First;
            while (current != null)
            {
                var component = current.Value;
                if (component == null)
                {
                    current = current.Next;
                    continue;
                }

                var componentName = component.GetType().Name;
                try
                {
                    component.OnInit();
                }
                catch (Exception e)
                {
                    MyLogger.LogError($"Failed to initialize component '{componentName}': {e.Message}\n{e.StackTrace}");
                }

                current = current.Next;
            }

            if (Procedure != null && _entranceProcedure != null)
            {
                MyLogger.Log($"[Main] Starting Entrance Procedure: {_entranceProcedure.Name}");
                Procedure.RunProcedureNode(_entranceProcedure);
            }

            MyLogger.Log("Init Data Components Done!");
        }

        private void InitConfigs()
        {
            if (configInit == null)
            {
                MyLogger.LogError("ConfigInit is not set.");
                return;
            }

            if (configInit != null)
            {
                Application.targetFrameRate = configInit.TargetFrameRate;
                Application.runInBackground = configInit.RunInBackground;
                Time.timeScale = configInit.GameSpeed;
                Screen.sleepTimeout = configInit.NeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
                MyLogger.IsLogEnabled = configInit.IsEnableLogg;

                if (Data)
                {
                    Data.isEncrypt = configInit.IsEncryptStorage;
                    PrefData.IsEncrypt = configInit.IsEncryptStorage;
                }

                if (Configs)
                    Configs.CheckVersion(() => { MyLogger.Log("New version " + Application.version + " detected!"); });
                IOUtility.directorySave = configInit.directoryPathSave;
                IOUtility.customPath = configInit.CustomPathSave;
                MyLogger.Log("Configs initialized successfully.");
            }
        }

        private void OnDestroy()
        {
            if (isDestroyingOnLoad) return;

            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                current.Value.OnDestroy();
                current = current.Next;
            }
        }
    }
}
