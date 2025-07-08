#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace OSK
{
    [CustomEditor(typeof(Main))]
    public class MainEditor : OdinEditor
    {
        private Main main;

        public override void OnInspectorGUI()
        {
            main = (Main)target;
            base.OnInspectorGUI();
            if (GUILayout.Button("CreateConfig"))
            {
                CreateConfig();
            }

			if (GUILayout.Button("Update Framewrok"))
            {
				ReimportPackage();
             }
        }


        public void CreateConfig()
        {
            ListViewSO listViewS0 = ScriptableObject.CreateInstance<ListViewSO>();
            ListSoundSO listSoundSo = ScriptableObject.CreateInstance<ListSoundSO>();
            UIParticleSO uiParticleSO = ScriptableObject.CreateInstance<UIParticleSO>();


            string path = "Assets/OSK/Resources/Configs";
            string resourcesPath = "Assets/OSK/Resources";

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(resourcesPath, "Configs");
            }

            IOUtility.CreateDirectory(path);
            if (main == null)
            {
                Debug.LogError("Main instance is null. Please ensure Main is initialized before creating config.");
                return;
            }

            var data = main.configInit.data;
            if (data.listSoundSo == null)
            {
                AssetDatabase.CreateAsset(listSoundSo, $"{path}/ListSoundSO.asset");
                data.listSoundSo = listSoundSo;
            }

            if (data.listViewS0 == null)
            {
                AssetDatabase.CreateAsset(listViewS0, $"{path}/ListViewSO.asset");
                data.listViewS0 = listViewS0;
            }

            if (data.uiParticleSO == null)
            { 
                AssetDatabase.CreateAsset(uiParticleSO, $"{path}/UIParticleSO.asset");
                data.uiParticleSO = uiParticleSO;
            }
            if (PrefabUtility.IsPartOfPrefabInstance(main))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(main);
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/Force Reimport Package")]
            public static void ReimportPackage()
            {
                string path = "com.osk.osk-framework";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log("Package reimported: " + path);
            }
    }
    
}
#endif