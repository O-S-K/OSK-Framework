#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace OSK
{
    public class UIManagerWindow : EditorWindow
    {
        private ListViewSO listViewSO;
        private DataViewUI newViewDraft = null;

        private const float LeftSidebarWidth = 200f;
        private const float RightPanelMinWidth = 1000f;

        private Vector2 leftScroll;
        private Vector2 rightScroll;

        private EViewType? selectedType = null;

        [MenuItem("OSK-Framework/UI/Window")]
        public static void ShowWindow()
        {
            var w = GetWindow<UIManagerWindow>("Window");
            w.minSize = new Vector2(LeftSidebarWidth, 500);
        }

        private void OnEnable()
        {
            if (listViewSO != null) return;
            var guids = AssetDatabase.FindAssets("t:ListViewSO");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                listViewSO = AssetDatabase.LoadAssetAtPath<ListViewSO>(path);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            listViewSO = (ListViewSO)EditorGUILayout.ObjectField("ListViewSO", listViewSO, typeof(ListViewSO), false);

            if (listViewSO == null)
            {
                EditorGUILayout.HelpBox("No ListViewSO assigned. Drag one here.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();

            DrawSidebar();

            float padding = 10f;
            float rightWidth = Mathf.Max(RightPanelMinWidth, position.width - LeftSidebarWidth - padding);

            GUILayout.BeginVertical(GUILayout.Width(rightWidth));
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

            DrawRightPanel();

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
                EditorUtility.SetDirty(listViewSO);
        }

        // ----------------------------------------
        // LEFT SIDEBAR
        // ----------------------------------------
        private void DrawSidebar()
        {
            GUILayout.BeginVertical(GUILayout.Width(LeftSidebarWidth));
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

            EditorGUILayout.LabelField("View Types", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            foreach (EViewType type in System.Enum.GetValues(typeof(EViewType)))
            {
                GUIStyle btn = new GUIStyle(GUI.skin.button);
                btn.alignment = TextAnchor.MiddleLeft;

                if (selectedType == type)
                {
                    GUI.backgroundColor = Color.Lerp(Color.cyan, Color.black, 0.4f);
                    GUILayout.Button(type.ToString(), btn, GUILayout.Height(26));
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    if (GUILayout.Button(type.ToString(), btn, GUILayout.Height(26)))
                        selectedType = type;
                }
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Show All", GUILayout.Height(26)))
                selectedType = null;
            
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Show SO UI", GUILayout.Height(26)))
            {
                Selection.activeObject = listViewSO;
                EditorGUIUtility.PingObject(listViewSO); 
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        
         

        // ----------------------------------------
        // RIGHT PANEL
        // ----------------------------------------
        private void DrawRightPanel()
        {
            EditorGUILayout.Space(10);

            // LIST BY CATEGORY
            IEnumerable<DataViewUI> list = listViewSO.Views;

            if (selectedType != null)
                list = list.Where(v => v.view != null && v.view.viewType == selectedType);

            list = list.OrderBy(v => v.depth).ToList();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Depth", GUILayout.Width(50));
            GUILayout.Label("Type", GUILayout.Width(100));
            GUILayout.Label("View Prefab", GUILayout.Width(240));
            GUILayout.Label("Remove", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            DrawLine();

            int index = 0;
            foreach (var data in list.ToList())
            {
                DrawViewRow(data, index);
                index++;
            }

            EditorGUILayout.Space(20);
            DrawAddViewButton();

            EditorGUILayout.Space(50);
            DrawBottomTools();
        }

        private void DrawViewRow(DataViewUI data, int index)
        {
            EditorGUILayout.BeginHorizontal();

            data.depth = EditorGUILayout.IntField(data.depth, GUILayout.Width(50));

            if (data.view != null)
            {
                data.view.viewType =
                    (EViewType)EditorGUILayout.EnumPopup(data.view.viewType, GUILayout.Width(100));
            }
            else
            {
                GUILayout.Label("N/A", GUILayout.Width(100));
            }

            data.view = (View)EditorGUILayout.ObjectField(
                data.view, typeof(View), false, GUILayout.Width(240));

            if (GUILayout.Button("X", GUILayout.Width(60)))
            {
                listViewSO.Views.Remove(data);
                EditorUtility.SetDirty(listViewSO);
                return;
            }

            EditorGUILayout.EndHorizontal();
        }

        // ----------------------------------------
        // ADD NEW VIEW
        // ----------------------------------------
        private void DrawAddViewButton()
        {
            if (newViewDraft == null)
            {
                GUI.color = Color.green;
                if (GUILayout.Button("➕ Add New View", GUILayout.Width(200), GUILayout.Height(32)))
                    newViewDraft = new DataViewUI();
                GUI.color = Color.white;
            }
            else
            {
                DrawNewViewDraft();
            }
        }

        private void DrawNewViewDraft()
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.Label("New View Draft", EditorStyles.boldLabel);

            newViewDraft.view = (View)EditorGUILayout.ObjectField("View", newViewDraft.view, typeof(View), false);
            newViewDraft.depth = EditorGUILayout.IntField("Depth", newViewDraft.depth);
            newViewDraft.viewType = (EViewType)EditorGUILayout.EnumPopup("View Type", newViewDraft.viewType);
        
            if (newViewDraft.view != null)
            {
                newViewDraft.path = IOUtility.GetPathAfterResources(newViewDraft.view);
                newViewDraft.view.depthEdit = newViewDraft.depth;
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (newViewDraft.view != null)
            {
                GUI.color = Color.green;
                if (GUILayout.Button("Confirm Add", GUILayout.Width(120)))
                {
                    newViewDraft.view.viewType = newViewDraft.viewType;
                    listViewSO.Views.Add(newViewDraft);
                    newViewDraft = null;
                    EditorUtility.SetDirty(listViewSO);
                }
                GUI.color = Color.white;
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
                newViewDraft = null;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ----------------------------------------
        // BOTTOM TOOL BUTTONS
        // ----------------------------------------
        private void DrawBottomTools()
        {
            DrawLine();

            if (GUILayout.Button("Sort By Depth + ViewType", GUILayout.Width(500), GUILayout.Height(25)))
            {
                listViewSO.Views.Sort((a, b) =>
                {
                    int d = a.depth.CompareTo(b.depth);
                    return d != 0 ? d : a.view.viewType.CompareTo(b.view.viewType);
                });
            }

            if (GUILayout.Button("Refresh Depth From Prefab", GUILayout.Width(500), GUILayout.Height(25)))
            {
                foreach (var v in listViewSO.Views)
                {
                    if (v.view != null)
                        v.depth = v.view.depthEdit;
                }
            }

            if (GUILayout.Button("Clear All", GUILayout.Width(500), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear All?", "Remove all views?", "OK", "Cancel"))
                    listViewSO.Views.Clear();
            }
        }

        private void DrawLine()
        {
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, Color.gray);
        }
    }
}
#endif
