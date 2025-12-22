#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OSK
{
    public class LoggerEditor : EditorWindow
    {
        public static LoggerEditor Instance;
        [SerializeField] TreeViewState m_TreeViewState;
        FilterTreeView m_TreeView;
        SearchField m_SearchField;

        [MenuItem("OSK-Framework/Logger Window")]
        public static void ShowWindow() => Instance = GetWindow<LoggerEditor>("Debug Filter");

        private void OnEnable()
        {
            Instance = this;
            MyLogger.OnLog += HandleOnLog;
        }

        private void OnDisable()
        {
            MyLogger.OnLog -= HandleOnLog;
        }

        private void HandleOnLog(string className, string message, Object context)
        {
            // Chỉ repaint để cập nhật số lượng nhảy liên tục, không Reload gây lag
            Repaint();
        }
         

        private void OnGUI()
        {
            if (m_TreeViewState == null) m_TreeViewState = new TreeViewState();
            if (m_TreeView == null) m_TreeView = new FilterTreeView(m_TreeViewState);
            if (m_SearchField == null) m_SearchField = new SearchField();

            // --- 1. TOOLBAR ---
            DrawToolbar();

            // --- 2. TREEVIEW ---
            // Trừ đi 18px toolbar và 30px khu vực nút bấm phía dưới
            Rect treeRect = new Rect(0, 18, position.width, position.height - 18 - 30);
            m_TreeView.OnGUI(treeRect);

            // --- 3. BOTTOM BUTTONS ---
            GUILayout.BeginArea(new Rect(5, position.height - 25, position.width - 10, 20));
            if (GUILayout.Button("Export Log to File", EditorStyles.miniButton))
            {
                WriteLogToFile();
            }
            GUILayout.EndArea();
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton)) m_TreeView.ExpandAll();
            if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton)) m_TreeView.CollapseAll();
            
            GUILayout.Space(5);
            
            EditorGUI.BeginChangeCheck();
            DebugFilterData.IsCollapsed = GUILayout.Toggle(DebugFilterData.IsCollapsed, "Collapse", EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                m_TreeView.Reload();
                RefreshUnityConsole();
            }

            GUILayout.Space(5);
            Rect searchRect = GUILayoutUtility.GetRect(100, 250, 18, 18, EditorStyles.toolbarSearchField);
            m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton))
            {
                DebugFilterData.ClearAll();
                m_TreeView.Reload();
                RefreshUnityConsole();
            }
            GUILayout.EndHorizontal();
        }

        private void WriteLogToFile()
        {
            string path = EditorUtility.SaveFilePanel("Save Log File", "", "Log_Export.txt", "txt");
            if (string.IsNullOrEmpty(path)) return;

            List<string> lines = new List<string>();
            // Duyệt TreeData (Cấu trúc mới: Class -> Method)
            foreach (var classEntry in DebugFilterData.TreeData)
            {
                if (!DebugFilterData.ClassStates.GetValueOrDefault(classEntry.Key, true)) continue;

                foreach (var meth in classEntry.Value)
                {
                    if (!meth.Value.Enabled || meth.Value.Entries.Count == 0) continue;

                    foreach (var entry in meth.Value.Entries)
                    {
                        lines.Add($"[{entry.Level}] [{classEntry.Key}::{meth.Key}] -> {entry.Message}");
                    }
                }
            }

            try
            {
                System.IO.File.WriteAllLines(path, lines);
                // Mở thư mục chứa file đã chọn (Windows)
                Application.OpenURL("file://" + System.IO.Path.GetDirectoryName(path));
                Debug.Log($"Log successfully written to: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to write log: {e.Message}");
            }
        }
        
        public void RefreshUnityConsole()
        {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntries == null) return;
            
            logEntries.GetMethod("Clear").Invoke(null, null);

            foreach (var classEntry in DebugFilterData.TreeData)
            {
                if (!DebugFilterData.ClassStates.GetValueOrDefault(classEntry.Key, true)) continue;

                foreach (var meth in classEntry.Value)
                {
                    if (!meth.Value.Enabled || meth.Value.Entries.Count == 0) continue;
                    
                    if (DebugFilterData.IsCollapsed)
                    {
                        var last = meth.Value.Entries[^1];
                        ReLog(classEntry.Key, $"({meth.Value.Entries.Count}x) {last.Message}", last.Level);
                    }
                    else
                    {
                        // In lại 50 dòng mới nhất để tránh đơ máy
                        int start = Mathf.Max(0, meth.Value.Entries.Count - 50);
                        for (int i = start; i < meth.Value.Entries.Count; i++)
                            ReLog(classEntry.Key, meth.Value.Entries[i].Message, meth.Value.Entries[i].Level);
                    }
                }
            }
        }

        private void ReLog(string cls, string msg, string level)
        {
            string f = $"[{cls}] -> {msg}";
            if (level == "Error" || level == "Fatal") Debug.LogError(f);
            else if (level == "Warning") Debug.LogWarning(f);
            else Debug.Log(f);
        }
    }
}
#endif