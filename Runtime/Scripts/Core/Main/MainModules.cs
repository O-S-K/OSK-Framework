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
}