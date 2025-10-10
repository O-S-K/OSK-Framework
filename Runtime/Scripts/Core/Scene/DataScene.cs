using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Sirenix.OdinInspector;

namespace OSK
{
    [System.Serializable]
    public class DataScene
    {
#if UNITY_EDITOR
        [HideLabel, LabelWidth(60)]
        public SceneAsset sceneAsset;
#endif

        [HorizontalGroup("Scene"), ReadOnly, LabelText("Scene Name"), LabelWidth(90)]
        [Required]
        public string sceneName;

        [HorizontalGroup("Scene"), Button(ButtonSizes.Small), GUIColor(0.6f, 1f, 0.6f)]
#if UNITY_EDITOR
        private void UpdateSceneName()
        {
            if (sceneAsset != null)
                sceneName = sceneAsset.name;
            else
                sceneName = string.Empty;
        }
#endif

        [Space]
        [EnumToggleButtons]
        public ELoadMode loadMode;

        [Tooltip("Whether to automatically remove the scene when unloading")]
        public bool autoRemove = true;
    }
}