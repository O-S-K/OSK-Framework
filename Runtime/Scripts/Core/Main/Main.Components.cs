using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    /// <summary>
    /// Main class of the framework, contains all the components of the framework.
    /// </summary>
    public partial class Main
    {
        public static MonoManager Mono { get; private set; }
        public static ObserverManager Observer { get; private set; }
        public static EventBusManager Event { get; private set; }
        public static PoolManager Pool { get; private set; }
        public static CommandManager Command { get; private set; }
        public static DirectorManager Director { get; private set; }
        public static ResourceManager Res { get; private set; }
        public static DataManager Data { get; private set; }
        public static WebRequestManager WebRequest { get; private set; }
        public static GameConfigsManager Configs { get; private set; }
        public static UIManager UI { get; private set; }
        public static SoundManager Sound { get; private set; }
        public static LocalizationManager Localization { get; private set; }
        public static EntityManager Entity { get; private set; }
        public static BlackboardManager Blackboard { get; private set; }
        public static ProcedureManager Procedure { get; private set; }
        public static GameInit GameInit { get; private set; }

        [HideLabel, InlineProperty] public ConfigInit configInit;

        [HideLabel, InlineProperty] public MainModules mainModules;

        public bool isDestroyingOnLoad = false;

        public static Main Instance => SingletonManager.Instance.Get<Main>();

        protected void Awake()
        {
            gameObject.name = " ======= [OSK Framework] ==========";

            SingletonManager.Instance.RegisterGlobal(this);
            if (isDestroyingOnLoad)
                DontDestroyOnLoad(gameObject);

            InitModules();
            InitDataComponents();
            InitConfigs();
        }

        private void InitModules()
        {
            foreach (ModuleType moduleType in Enum.GetValues(typeof(ModuleType)))
            {
                if (moduleType == ModuleType.None || (mainModules.Modules & moduleType) == 0) continue;

                var newObject = new GameObject(moduleType.ToString());
                newObject.transform.SetParent(transform);
                var componentType = mainModules.GetComponentType(moduleType.ToString());
                if (componentType != null)
                {
                    var module = newObject.AddComponent(componentType) as GameFrameworkComponent;
                    AssignModuleInstance(module);
                    OSKLogger.Log("Main", $"Module {moduleType} initialized.");
                }
                else
                {
                    OSKLogger.LogError("Main", $"Module {moduleType} not found in MainModules.");
                }
            }
        }

        private void AssignModuleInstance(GameFrameworkComponent module)
        {
            if (module is MonoManager manager) Mono = manager;
            else if (module is ObserverManager observer) Observer = observer;
            else if (module is EventBusManager eventBus) Event = eventBus;
            else if (module is PoolManager pool) Pool = pool;
            else if (module is CommandManager command) Command = command;
            else if (module is DirectorManager scene) Director = scene;
            else if (module is ResourceManager res) Res = res;
            else if (module is DataManager data) Data = data;
            else if (module is WebRequestManager webRequest) WebRequest = webRequest;
            else if (module is GameConfigsManager configs) Configs = configs;
            else if (module is UIManager ui) UI = ui;
            else if (module is SoundManager sound) Sound = sound;
            else if (module is LocalizationManager localization) Localization = localization;
            else if (module is EntityManager entity) Entity = entity;
            else if (module is BlackboardManager blackboard) Blackboard = blackboard;
            else if (module is ProcedureManager procedure) Procedure = procedure;
            else if (module is GameInit gameInit) GameInit = gameInit;
            else OSKLogger.LogError("Main", $"[AssignModuleToField] Unknown module type: {module}");
        }

        private void InitDataComponents()
        {
            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                var componentName = current.Value?.GetType().Name ?? "Unknown";
                try
                {
                    if (current.Value == null)
                    {
                        OSKLogger.LogError("Main", $"[InitData] Component '{componentName}' is NULL.");
                    }
                    else
                    {
                        current.Value.OnInit();
                    }
                }
                catch (Exception e)
                {
                    OSKLogger.LogError("Main",
                        $"[InitData] Failed to initialize component '{componentName}': {e.Message}\n{e.StackTrace}");
                }

                current = current.Next;
            }

            OSKLogger.Log("[InitData] Init Data Components Done!");
        }

        private void InitConfigs()
        {
            if (configInit == null)
            {
                OSKLogger.LogError("[InitConfigs] ConfigInit is not set.");
                return;
            }

            if (configInit != null)
            {
                Application.targetFrameRate = configInit.TargetFrameRate;
                Application.runInBackground = configInit.RunInBackground;
                Time.timeScale = configInit.GameSpeed;
                QualitySettings.vSyncCount = configInit.VSyncCount;
                Screen.sleepTimeout = configInit.NeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
                OSKLogger.SetLogEnabled(configInit.IsEnableLogg);
                
                if (Data) Data.isEncrypt = configInit.IsEncryptStorage;
                if (Configs) Configs.CheckVersion(() => { Debug.Log("New version"); });
                IOUtility.directorySave = configInit.directoryPathSave;
                IOUtility.customPath = configInit.CustomPathSave;
                OSKLogger.Log("[InitConfigs] Configs initialized successfully.");
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