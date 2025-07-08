using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    [Serializable]
    public class MainModules
    {
        // Cache nội bộ không cần hiển thị
        [HideInInspector]
        private Dictionary<string, Type> componentTypeCache = new();

        [BoxGroup("⚙ Modules Selection")]
        [EnumToggleButtons]
        [SerializeField]
        private ModuleType _modules;
        public ModuleType Modules => _modules;

        [BoxGroup("⚙ Modules Selection")]
        [ReadOnly, InfoBox("Select the modules you want to enable for this project.", InfoMessageType.None)]
        [HideLabel]
        public string title = "";

        [BoxGroup("⚙ Modules Selection/Actions")]
        [HorizontalGroup("⚙ Modules Selection/Actions/Row")]
        [Button(ButtonSizes.Medium)]
        private void EnableAllModule()
        {
            _modules = (ModuleType)~0;
            Debug.Log("✅ All modules have been selected.");
        }

        [HorizontalGroup("⚙ Modules Selection/Actions/Row")]
        [Button(ButtonSizes.Medium)]
        private void DisableAllModule()
        {
            _modules = 0;
            Debug.Log("❌ All modules have been deselected.");
        }

        // Public method for resolving Type
        public Type GetComponentType(string moduleName)
        {
            if (componentTypeCache.TryGetValue(moduleName, out var type))
                return type;

            var fullTypeName = "OSK." + moduleName;
            var componentType = Type.GetType(fullTypeName);
            if (componentType != null)
                componentTypeCache[moduleName] = componentType;

            return componentType;
        }
    }

    [Flags]
    public enum ModuleType
    {
        None = 0,
        MonoManager = 1 << 0,
        ServiceLocatorManager = 1 << 1,
        ObserverManager = 1 << 2,
        EventBusManager = 1 << 3,
        FSMManager = 1 << 4,
        PoolManager = 1 << 5,
        CommandManager = 1 << 6,
        DirectorManager = 1 << 7,
        ResourceManager = 1 << 8,
        StorageManager = 1 << 9,
        DataManager = 1 << 10,
        NetworkManager = 1 << 11,
        WebRequestManager = 1 << 12,
        GameConfigsManager = 1 << 13,
        UIManager = 1 << 14,
        SoundManager = 1 << 15,
        LocalizationManager = 1 << 16,
        EntityManager = 1 << 17,
        TimeManager = 1 << 18,
        NativeManager = 1 << 19,
        BlackboardManager = 1 << 20,
        ProcedureManager = 1 << 21,
        GameInit = 1 << 22
    }
}