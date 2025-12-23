using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    [CreateAssetMenu(fileName = "InputConfigSO", menuName = "OSK/Input/Input Config")]
    public class InputConfigSO : ScriptableObject
    {
        [Title("General Settings")]
        public bool enableMultiTouch = true;
        public Camera cameraDetection;
        
        [Title("Input Actions")]
        [ListDrawerSettings(DraggableItems = true)]
        [SerializeField] private List<InputActionDefinition> actions = new();
        public IReadOnlyList<InputActionDefinition> Actions => actions;

        private void OnEnable()
        {
            name = "InputConfigSO";
        }
    }
}