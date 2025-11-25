#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO; 

namespace OSK
{
    [CustomEditor(typeof(DataManager))]
    public class SaveEditorVisual : Editor
    {
        private DataManager manager;

        private void OnEnable()
        {
            manager = (DataManager)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("--- Save System ---", EditorStyles.boldLabel);
            DisplaySavedFiles();
        }

        private void DisplaySavedFiles()
        {
            EditorGUILayout.Space();
            DrawBackground(Color.white);
            DisplayFileSystemFiles();


            EditorGUILayout.Space();
            DrawBackground(Color.white);
            DisplayPlayerPrefsKeys();
        }

        private void DisplayFileSystemFiles()
        {
            EditorGUILayout.LabelField("FileSystem", EditorStyles.boldLabel, GUILayout.Width(400));

            var files = IOUtility.GetAll(Application.persistentDataPath);
            foreach (var file in files)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(file); // Display the filename

                if (GUILayout.Button("Delete"))
                {
                    DeleteFile(Path.Combine(Application.persistentDataPath, file));
                }

                if (GUILayout.Button("Open Path"))
                {
                    OpenFileLocation(Path.Combine(Application.persistentDataPath, file));
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DisplayPlayerPrefsKeys()
        {
            EditorGUILayout.LabelField("PlayerPrefs", GUILayout.Width(400));

            if (GUILayout.Button("Open"))
            {
                
#if CustomPlayerPref
                CustomPlayerPref.PlayerPrefsEditor.PlayerPrefsEditor.Init();
#endif
            }
        }

        private void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                OSKLogger.Log("Storage",$"Deleted file: {filePath}");
            }
            else
            {
                OSKLogger.LogError("Storage",$"File not found: {filePath}");
            }
        }

        private void OpenFileLocation(string filePath)
        {
            string fileDirectory = Path.GetDirectoryName(filePath);
            if (Directory.Exists(fileDirectory))
            {
                EditorUtility.RevealInFinder(fileDirectory); // Opens the directory containing the file
            }
            else
            {
                OSKLogger.LogError("Storage",$"Directory not found: {fileDirectory}");
            }
        }

        private void DrawBackground(Color color)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color);
        }
    }
}
#endif