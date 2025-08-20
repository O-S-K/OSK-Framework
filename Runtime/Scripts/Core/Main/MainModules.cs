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
        ObserverManager = 1 << 2,
        EventBusManager = 1 << 3,
        PoolManager = 1 << 4,
        CommandManager = 1 << 5,
        DirectorManager = 1 << 6,
        ResourceManager = 1 << 7,
        StorageManager = 1 << 8,
        DataManager = 1 << 9,
        WebRequestManager = 1 << 10,
        GameConfigsManager = 1 << 11,
        UIManager = 1 << 12,
        SoundManager = 1 << 13,
        LocalizationManager = 1 << 14,
        EntityManager = 1 << 15,
        TimeManager = 1 << 16,
        BlackboardManager = 1 << 17,
        ProcedureManager = 1 << 18,
        GameInit = 1 << 19
    }
}