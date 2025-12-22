#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using System.Linq;
using Sirenix.Utilities;
using UnityEngine;

namespace OSK
{
    public class DataSheetEditorWindow : OdinMenuEditorWindow
    {
        [MenuItem("OSK-Framework/Data Sheet/Window")]
        public static void OpenWindow()
        {
            var window = GetWindow<DataSheetEditorWindow>();
            window.titleContent = new GUIContent("List Sheet SO");
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1100, 700);
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.Config.DrawSearchToolbar = true;
 
            var sheets = AssetDatabase.FindAssets("t:BaseSheet")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<BaseSheet>)
                .Where(x => x);

            foreach (var sheet in sheets)
            { 
                tree.Add($"{sheet.GetType().Name}/{sheet.name}", sheet);
            }

            tree.SortMenuItemsByName();
            return tree;
        }

        // Vẽ thêm Toolbar phía trên để Save/Refresh nhanh
        protected override void OnBeginDrawEditors()
        {
            SirenixEditorGUI.BeginHorizontalToolbar();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh Assets", EditorStyles.miniButtonLeft))
                {
                    AssetDatabase.Refresh();
                    this.ForceMenuTreeRebuild();
                }

                if (GUILayout.Button("Save Project", EditorStyles.miniButtonRight))
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log("Saved all data sheets.");
                }
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }
    }
}
#endif