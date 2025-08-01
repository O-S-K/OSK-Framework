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
        public static ServiceLocatorManager Service { get; private set; }
        public static ObserverManager Observer { get; private set; }
        public static EventBusManager Event { get; private set; }
        public static FSMManager Fsm { get; private set; }
        public static PoolManager Pool { get; private set; }
        public static CommandManager Command { get; private set; }
        public static DirectorManager Director { get; private set; }
        public static ResourceManager Res { get; private set; }
        public static StorageManager Storage { get; private set; }
        public static DataManager Data { get; private set; }
        public static NetworkManager Network { get; private set; }
        public static WebRequestManager WebRequest { get; private set; }
        public static GameConfigsManager Configs { get; private set; }
        public static UIManager UI { get; private set; }
        public static SoundManager Sound { get; private set; }
        public static LocalizationManager Localization { get; private set; }
        public static EntityManager Entity { get; private set; }
        public static TimeManager Time { get; private set; }
        public static NativeManager Native { get; private set; }
        public static BlackboardManager Blackboard { get; private set; }
        public static ProcedureManager Procedure { get; private set; }
        public static GameInit GameInit { get; private set; }

        [HideLabel, InlineProperty]
        public ConfigInit configInit;
        
        [HideLabel, InlineProperty]
        public MainModules mainModules;

        public bool isDestroyingOnLoad = false;
        public bool isLogInit = false;

        protected void Awake()
        {
            if (isDestroyingOnLoad)
                DontDestroyOnLoad(gameObject);

            InitModules();
            InitDataComponents();
            InitConfigs();
        }

        public void InitModules()
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
                    Logg.Log($"[Main] Module {moduleType} initialized.", Color.green, isLogInit);
                }
                else
                {
                    Logg.LogError($"[Main] Module {moduleType} not found in MainModules.");
                }
            }
        }

        private void AssignModuleInstance(GameFrameworkComponent module)
        {
            if (module is MonoManager manager) Mono = manager;
            else if (module is ServiceLocatorManager locator) Service = locator;
            else if (module is ObserverManager observer) Observer = observer;
            else if (module is EventBusManager eventBus) Event = eventBus;
            else if (module is FSMManager fsm) Fsm = fsm;
            else if (module is PoolManager pool) Pool = pool;
            else if (module is CommandManager command) Command = command;
            else if (module is DirectorManager scene) Director = scene;
            else if (module is ResourceManager res) Res = res;
            else if (module is StorageManager save) Storage = save;
            else if (module is DataManager data) Data = data;
            else if (module is NetworkManager network) Network = network;
            else if (module is WebRequestManager webRequest) WebRequest = webRequest;
            else if (module is GameConfigsManager configs) Configs = configs;
            else if (module is UIManager ui) UI = ui;
            else if (module is SoundManager sound) Sound = sound;
            else if (module is LocalizationManager localization) Localization = localization;
            else if (module is EntityManager entity) Entity = entity;
            else if (module is TimeManager time) Time = time;
            else if (module is NativeManager native) Native = native;
            else if (module is BlackboardManager blackboard) Blackboard = blackboard;
            else if (module is ProcedureManager procedure) Procedure = procedure;
            else if (module is GameInit gameInit) GameInit = gameInit;
            else Logg.LogError($"[AssignModuleToField] Unknown module type: {module}");
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
                        Logg.LogError($"[InitData] Component '{componentName}' is NULL.");
                    }
                    else
                    {
                        Logg.Log($"[InitData] Initializing '{componentName}'...", Color.cyan, isLogInit);
                        current.Value.OnInit();
                    }
                }
                catch (Exception e)
                {
                    Logg.LogError($"[InitData] Failed to initialize component '{componentName}': {e.Message}\n{e.StackTrace}");
                }

                current = current.Next;
            }

            Logg.Log("[InitData] Init Data Components Done!", Color.green, isLogInit);
        }

        private void InitConfigs()
        {
            if (configInit == null)
            {
                Logg.LogError("[InitConfigs] ConfigInit is not set.");
                return;
            }

            Application.targetFrameRate = configInit.TargetFrameRate;
            if(configInit != null) Logg.SetLogEnabled(configInit.IsEnableLogg);
            if(Main.Storage) Main.Storage.isEncrypt = configInit.IsEncryptStorage;
            if(Main.Configs) Main.Configs.CheckVersion(() =>
            {
                Debug.Log("New version");
            });
            Logg.Log("[InitConfigs] Configs initialized successfully.", Color.green, isLogInit);
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