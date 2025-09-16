#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace OSK
{
    [CustomEditor(typeof(ListViewSO))]
    public class ListViewSOEditor : OdinEditor
    {
        private Dictionary<EViewType, bool> viewTypeFoldouts = new Dictionary<EViewType, bool>();
        private ListViewSO listViewSO = null;
        private List<View> listViews = null;
        private DataViewUI newViewDraft = null;

        private void OnEnable() => listViewSO = (ListViewSO)target;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Depth", GUILayout.Width(50));
            EditorGUILayout.LabelField("Type", GUILayout.Width(100));
            EditorGUILayout.LabelField("Script Ref", GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("----------------------------------------------------------------------------");


            DisplayGroupedViews(listViewSO);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("----------------------------------------------------------------------------");
            EditorGUILayout.Space();

            // nút Add New View
            if (newViewDraft == null)
            {
                GUI.color = Color.green;
                if (GUILayout.Button("➕ Add New View", GUILayout.Width(200), GUILayout.Height(40)))
                {
                    newViewDraft = new DataViewUI
                    {
                        depth = 0,
                        view = null,
                        path = "",
                        viewType =  EViewType.Popup,
                        isPreloadSpawn = true,
                        isRemoveOnHide = false
                    };
                }

                GUI.color = Color.white;
            }
            else
            {
                DrawNewViewDraft();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("----------------------------------------------------------------------------");

            if (GUILayout.Button("Add All View From Resources"))
            {
                AddAllViewFormResources();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Sort By ViewType and Depth"))
            {
                SortViews(listViewSO);
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.Space(10);
            // if (GUILayout.Button("Add"))
            // {
            //     listViewSO.Views.Add(new DataViewUI { depth = 0, view = null });
            //     EditorUtility.SetDirty(target);
            // }


            if (GUILayout.Button("Set Depth To Prefab"))
            {
                SetDepthSOToRrefab();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Refresh"))
            {
                RefreshListUI();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Clear All"))
            {
                listViewSO.Views.Clear();
                EditorUtility.SetDirty(target);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private void CheckUniqueUI()
        {
            var views = new List<DataViewUI>();
            foreach (var view in listViewSO.Views)
            {
                if (views.Contains(view))
                    OSK.Logg.LogError("UI",$"Popup Type {view} exists in the list. Please remove it.");
                else
                    views.Add(view);
            }
        }

        private void DrawNewViewDraft()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("box");

            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("➕ New View Draft", EditorStyles.boldLabel, GUILayout.Height(20));
            GUI.color = Color.white;
            newViewDraft.depth = EditorGUILayout.IntField("Depth", newViewDraft.depth);
            newViewDraft.viewType = (EViewType)EditorGUILayout.EnumPopup("View Type", newViewDraft.viewType);
            newViewDraft.isPreloadSpawn = EditorGUILayout.Toggle("Is Preload Spawn", newViewDraft.isPreloadSpawn);
            newViewDraft.isRemoveOnHide = EditorGUILayout.Toggle("Is Remove On Hide", newViewDraft.isRemoveOnHide);
            newViewDraft.view =
                (View)EditorGUILayout.ObjectField("View", newViewDraft.view, typeof(View), false);

            if (newViewDraft.view != null)
            {
                newViewDraft.view.depthEdit = newViewDraft.depth;
                newViewDraft.path = IOUtility.GetPathAfterResources(newViewDraft.view);
            }

            EditorGUILayout.BeginHorizontal();
            
            GUI.color = newViewDraft.view != null ? Color.green : Color.gray;
            GUI.enabled = newViewDraft.view != null;
            if (GUILayout.Button("Confirm Add", GUILayout.Width(120)))
            {
                if (newViewDraft.view != null)
                {
                    listViewSO.Views.Add(newViewDraft);
                    newViewDraft = null;
                    EditorUtility.SetDirty(listViewSO);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a View before confirming.", "OK");
                }
            }
            GUI.enabled = true;
            GUI.color = Color.white;

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                newViewDraft = null;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        public void AddAllViewFormResources()
        {
            listViews = Resources.LoadAll<View>("").ToList().FindAll(x => x.isAddToViewManager);
            if (listViews.Count == 0)
            {
                OSK.Logg.LogWarning("No views found in Resources folder");
                return;
            }

            foreach (var popup in listViews)
            {
                if (listViewSO.Views.Any(x => x.view == popup))
                    continue;

                var data = new DataViewUI
                {
                    view = popup,
                    path = IOUtility.GetPathAfterResources(popup)
                };
                data.depth = popup.depthEdit;
                listViewSO.Views.Add(data);
            }

            SortViews(listViewSO);
        }

        private void RefreshListUI()
        {
            SetDepthFromRes();
            SortViews(listViewSO);
        }

        private void SetDepthFromRes()
        {
            foreach (var viewData in listViewSO.Views)
            {
                if (viewData.view == null) continue;

                viewData.depth = viewData.view.depthEdit;

#if UNITY_EDITOR
                viewData.path = IOUtility.GetPathAfterResources(viewData.view);
                UnityEditor.EditorUtility.SetDirty(target);
#endif
            }
        }

        private void SetDepthSOToRrefab()
        {
            foreach (var viewData in listViewSO.Views)
            {
                if (viewData.view == null) continue;

                // Cập nhật depth trong view
                viewData.view.depthEdit = viewData.depth;

                // Đánh dấu prefab là dirty (nếu là prefab)
                EditorUtility.SetDirty(viewData.view);

#if UNITY_2021_1_OR_NEWER
                // Nếu là prefab asset, thì save lại
                var prefabStage = PrefabUtility.GetPrefabInstanceHandle(viewData.view);
                if (prefabStage == null)
                {
                    // Là prefab asset chứ không phải instance trong scene
                    string prefabPath = AssetDatabase.GetAssetPath(viewData.view);
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        AssetDatabase.SaveAssetIfDirty(viewData.view);
                    }
                }
#else
        // Với Unity cũ hơn
        PrefabUtility.RecordPrefabInstancePropertyModifications(viewData.view);
#endif
            }

            AssetDatabase.SaveAssets();
        }


        private void SortViews(ListViewSO listViewSO)
        {
            listViewSO.Views.Sort((x, y) =>
            {
                int depthComparison = x.depth.CompareTo(y.depth);
                if (depthComparison != 0)
                {
                    return depthComparison;
                }

                return x.view.viewType.CompareTo(y.view.viewType);
            });

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(target);
        }

        private void DisplayGroupedViews(ListViewSO listViewSO)
        {
            EViewType? currentViewType = null;

            for (int i = 0; i < listViewSO.Views.Count; i++)
            {
                var dataView = listViewSO.Views[i];

                if (dataView.view != null)
                {
                    if (currentViewType != dataView.view.viewType)
                    {
                        currentViewType = dataView.view.viewType;

                        if (!viewTypeFoldouts.ContainsKey(currentViewType.Value))
                        {
                            viewTypeFoldouts[currentViewType.Value] = true;
                        }

                        EditorGUILayout.Space();
                        viewTypeFoldouts[currentViewType.Value] = EditorGUILayout.Foldout(
                            viewTypeFoldouts[currentViewType.Value], $"{currentViewType}", true, EditorStyles.foldout);
                    }

                    if (currentViewType.HasValue && viewTypeFoldouts[currentViewType.Value])
                    {
                        DisplayViewRow(dataView, listViewSO, i);
                    }
                }
                else
                {
                    currentViewType = null;
                    dataView.view = (View)EditorGUILayout.ObjectField(dataView.view, typeof(View),
                        allowSceneObjects: false, GUILayout.Width(200));

                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        listViewSO.Views.Remove(dataView);
                        EditorUtility.SetDirty(target);
                        return;
                    }
                }
            }
        }

        private void DisplayViewRow(DataViewUI dataView, ListViewSO listViewSO, int index)
        {
            EditorGUILayout.BeginHorizontal();
            dataView.depth = EditorGUILayout.IntField(dataView.depth, GUILayout.Width(50));

            if (dataView.view != null)
            {
                dataView.view.viewType =
                    (EViewType)EditorGUILayout.EnumPopup(dataView.view.viewType, GUILayout.Width(100));
            }
            else
            {
                EditorGUILayout.LabelField("N/A", GUILayout.Width(100));
            }

            dataView.view = (View)EditorGUILayout.ObjectField(dataView.view, typeof(View), allowSceneObjects: false,
                GUILayout.Width(200));

            if (dataView.view != null)
            {
                bool isDuplicate = listViewSO.Views.Count(v => v.view == dataView.view) > 1;

                if (isDuplicate)
                {
                    EditorGUILayout.LabelField("Duplicate Exists", GUILayout.Width(100));
                }
            }

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                listViewSO.Views.RemoveAt(index);
                EditorUtility.SetDirty(target);
                return;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif