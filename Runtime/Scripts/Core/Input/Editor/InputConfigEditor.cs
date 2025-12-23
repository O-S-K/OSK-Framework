#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Text;
using Sirenix.Utilities;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector.Editor;

namespace OSK
{
    public class InputConfigEditor : OdinMenuEditorWindow
    {
        private InputConfigSO _config;
 
        private string GenPath 
        {
            get => EditorPrefs.GetString("OSK_InputGenPath", "Assets");
            set => EditorPrefs.SetString("OSK_InputGenPath", value);
        }

        [MenuItem("OSK-Framework/Input/Config Input", priority = 200)]
        private static void OpenWindow()
        {
            var window = GetWindow<InputConfigEditor>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(900, 700);
            window.titleContent = new GUIContent("Input Actions", SdfIconType.Joystick.ToString());
        }

        protected override void OnBeginDrawEditors()
        {
            // Toolbar này luôn hiển thị ở trên cùng của phần nội dung bên phải
            SirenixEditorGUI.BeginHorizontalToolbar();
            {
                // 1. Nút chọn Folder
                if (SirenixEditorGUI.ToolbarButton(new GUIContent(" Path: " + GetRelativePath(GenPath), EditorIcons.Folder.Raw)))
                {
                    SelectFolder();
                }

                // 2. Nút Generate
                if (SirenixEditorGUI.ToolbarButton(new GUIContent(" Generate Code", SdfIconType.CodeSquare.ToString())))
                {
                    GenerateActionClass();
                }

                GUILayout.FlexibleSpace();

                // 3. Các nút quản lý Action
                if (SirenixEditorGUI.ToolbarButton(new GUIContent(" Add", EditorIcons.Plus.Raw)))
                {
                    AddNewAction();
                }

                if (SirenixEditorGUI.ToolbarButton(new GUIContent(" Remove", EditorIcons.X.Raw)))
                {
                    RemoveSelectedAction();
                }

                if (SirenixEditorGUI.ToolbarButton(new GUIContent(" Save", EditorIcons.Download.ToString())))
                {
                    AssetDatabase.SaveAssets();
                    EditorUtility.SetDirty(_config);
                    Debug.Log("Saved Input Configuration");
                }
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        private void SelectFolder()
        {
            string absolutePath = EditorUtility.OpenFolderPanel("Select Output Folder", GenPath, "");
            if (!string.IsNullOrEmpty(absolutePath))
            {
                // Chuyển đổi đường dẫn tuyệt đối sang đường dẫn Assets (Relative Path)
                if (absolutePath.StartsWith(Application.dataPath))
                {
                    GenPath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogError("Vui lòng chọn thư mục bên trong dự án (Assets folder)!");
                }
            }
        }

        private string GetRelativePath(string path)
        {
            if (path.Length > 30) return "..." + path.Substring(path.Length - 27);
            return path;
        }

        private void GenerateActionClass()
        {
            if (_config == null || _config.Actions.Count == 0) return;

            string filePath = Path.Combine(GenPath, "EInputEnums.cs");
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("namespace OSK");
            sb.AppendLine("{");
            sb.AppendLine("    public enum EInputEnums"); // Tạo Enum thay vì Class
            sb.AppendLine("    {");
            sb.AppendLine("        None = 0,");

            foreach (var action in _config.Actions)
            {
                if (string.IsNullOrEmpty(action.id)) continue;
                string safeName = action.id.Replace(" ", "_");
                sb.AppendLine($"        {safeName},");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Generated EInputEnums.cs", "OK");
        }

        // --- Giữ nguyên các hàm bổ trợ ---
        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            _config = AssetDatabase.FindAssets("t:InputConfigSO")
                .Select(guid => AssetDatabase.LoadAssetAtPath<InputConfigSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();

            if (_config == null)
            {
                tree.Add("Error", "Cần tạo file InputConfigSO trước!");
                return tree;
            }

            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;
            tree.Config.DrawSearchToolbar = true;
            tree.Add("Settings", _config, SdfIconType.GearFill);

            foreach (var action in _config.Actions)
            {
                string menuPath = "InputActionIDs/" + (string.IsNullOrEmpty(action.id) ? "Unnamed Action" : action.id);
                tree.Add(menuPath, action);
            }

            return tree;
        }

        private void AddNewAction()
        {
            if (_config == null) return;
            SerializedObject so = new SerializedObject(_config);
            SerializedProperty prop = so.FindProperty("actions");
            prop.arraySize++;
            prop.GetArrayElementAtIndex(prop.arraySize - 1).FindPropertyRelative("id").stringValue = "New Action";
            so.ApplyModifiedProperties();
            ForceMenuTreeRebuild();
        }

        private void RemoveSelectedAction()
        {
            var selected = this.MenuTree.Selection.SelectedValue as InputActionDefinition;
            if (selected == null || _config == null) return;

            SerializedObject so = new SerializedObject(_config);
            SerializedProperty prop = so.FindProperty("actions");
            for (int i = 0; i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == selected.id)
                {
                    prop.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
            so.ApplyModifiedProperties();
            ForceMenuTreeRebuild();
        }
    }
}
#endif