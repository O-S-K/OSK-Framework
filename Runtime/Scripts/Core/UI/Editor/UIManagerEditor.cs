#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;

namespace OSK
{
    [CustomEditor(typeof(RootUI))]
    public class UIManagerEditor : OdinEditor
    {
        private RootUI uiManager;
        private void OnEnable() => uiManager = (RootUI)target;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Select Data UI SO"))
            {
                FindViewDataSOAssets();
            }
            if (GUILayout.Button("Setup Canvas For Setting"))
            {
                uiManager.SetupCanvas();
            }

            GUILayout.Space(10);

            // Draw background for the views section
            DrawBackground(Color.green);
            EditorGUILayout.LabelField("--- List Views  ---", EditorStyles.boldLabel);
            DisplayViews();

            GUILayout.Space(10);

            EditorGUILayout.LabelField("--- List Views History ---", EditorStyles.boldLabel);
            ShowListViewHistory();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private void ShowListViewHistory()
        {
            var views = uiManager.ListViewHistory.ToList();
            if (views.Count == 0) return;
            for (int i = 0; i < views.Count; i++)
            {
                var _view = views[i];
                if (_view == null)
                    continue;
                try
                {
                    EditorGUILayout.LabelField($"{i}: " + _view.name, GUILayout.Width(400));
                }
                catch (System.Exception ex)
                {
                    MyLogger.LogWarning($"Error displaying view: {ex.Message}");
                }
            }
        }

        private void FindViewDataSOAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:ListViewSO");
            if (guids.Length == 0)
            {
                MyLogger.LogError("No ListViewSO found in the project.");
                return;
            }

            var viewData = new List<ListViewSO>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ListViewSO v = AssetDatabase.LoadAssetAtPath<ListViewSO>(path);
                viewData.Add(v);
            }

            if (viewData.Count == 0)
            {
                MyLogger.LogError("No ListViewSO found in the project.");
            }
            else
            {
                foreach (var v in viewData)
                {
                    MyLogger.Log("ListViewSO found: " + v.name);
                    Selection.activeObject = v;
                    EditorGUIUtility.PingObject(v);
                }
            }
        }

        private void DrawBackground(Color color)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color);
        }

        private void DisplayViews()
        {
            if (!Application.isPlaying)
                return;

            List<View> views = uiManager.GetAll(true).Where(v => v != null && !v.Equals(null)).ToList();

            for (int i = 0; i < views.Count; i++)
            {
                var _view = views[i];
                if (_view == null)
                    continue;

                try
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField($"{i}: " + _view.name, GUILayout.Width(400));

                    bool isVisible = uiManager.Get(_view).IsShowing;

                    GUI.enabled = !isVisible;
                    if (GUILayout.Button("Open"))
                    {
                        uiManager.Open(_view, null, true);
                    }

                    GUI.enabled = isVisible;
                    if (GUILayout.Button("Hide"))
                    {
                        uiManager.Hide(_view);
                    }

                    if (GUILayout.Button("Delete"))
                    {
                        uiManager.Delete(_view);
                    }

                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }
                catch (System.Exception ex)
                {
                    MyLogger.LogWarning($"Error displaying view: {ex.Message}");
                }
            }
        }
    }
}
#endif