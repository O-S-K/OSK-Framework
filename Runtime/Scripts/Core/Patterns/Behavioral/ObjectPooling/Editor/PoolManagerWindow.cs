#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OSK
{
    public class PoolManagerWindow : EditorWindow
    {
      private PoolManager poolManager;
        private string selectedGroupKey = "";
        private string searchString = "";
        private Vector2 scrollPosLeft, scrollPosRight;
        private bool autoRefresh = true;
        
        // --- NEW FEATURES ---
        private bool compactMode = false; // Chế độ thu gọn
        private enum SortOption { Name, MostActive, HighMemory }
        private SortOption currentSort = SortOption.Name;

        // Styles
        private GUIStyle cardStyle, headerStyle, badgeStyleGO, badgeStyleComp;

        [MenuItem("OSK/Ultimate Pool Master")]
        public static void ShowWindow()
        {
            GetWindow<PoolManagerWindow>("Pool Master").minSize = new Vector2(950, 550);
        }

        private void InitStyles()
        {
            if (cardStyle == null)
            {
                cardStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 10, 10), margin = new RectOffset(5, 5, 5, 5) };
                
                headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
                
                // Style cho Note (Badge)
                badgeStyleGO = new GUIStyle(EditorStyles.miniLabel) { 
                    normal = { textColor = new Color(0.4f, 0.8f, 1f) }, // Xanh dương
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight
                };
                
                badgeStyleComp = new GUIStyle(EditorStyles.miniLabel) { 
                    normal = { textColor = new Color(1f, 0.6f, 0.2f) }, // Cam
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight
                };
            }
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying && autoRefresh) Repaint();
        }

        private void OnGUI()
        {
            InitStyles();
            if (poolManager == null) poolManager = FindObjectOfType<PoolManager>();

            DrawToolbar();

            if (poolManager == null || !Application.isPlaying) { DrawEmptyState(); return; }

            EditorGUILayout.BeginHorizontal();
            DrawSidebar();
            DrawContent();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("OSK Pool Master", EditorStyles.boldLabel, GUILayout.Width(110));
            
            // Search
            searchString = EditorGUILayout.TextField(searchString, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            
            // Sort
            GUILayout.Space(10);
            GUILayout.Label("Sort:", GUILayout.Width(30));
            currentSort = (SortOption)EditorGUILayout.EnumPopup(currentSort, EditorStyles.toolbarPopup, GUILayout.Width(90));

            GUILayout.FlexibleSpace();
            
            // Options
            compactMode = GUILayout.Toggle(compactMode, "Compact Mode", EditorStyles.toolbarButton);
            autoRefresh = GUILayout.Toggle(autoRefresh, "Live Monitor", EditorStyles.toolbarButton);
            
            if (GUILayout.Button("GC Collect", EditorStyles.toolbarButton)) System.GC.Collect();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240), GUILayout.ExpandHeight(true));
            DrawBox(new Color(0.2f, 0.2f, 0.2f), () => 
            {
                scrollPosLeft = EditorGUILayout.BeginScrollView(scrollPosLeft);
                foreach (var groupKey in poolManager.GroupPrefabLookup.Keys)
                {
                    if (!string.IsNullOrEmpty(searchString) && !groupKey.ToLower().Contains(searchString.ToLower())) continue;

                    bool isSelected = groupKey == selectedGroupKey;
                    GUI.backgroundColor = isSelected ? new Color(0.3f, 0.6f, 1f) : Color.white;
                    
                    // Show count badge
                    int count = poolManager.GroupPrefabLookup[groupKey].Count;
                    if (GUILayout.Button($"{groupKey} [{count}]", EditorStyles.miniButton, GUILayout.Height(28)))
                    {
                        selectedGroupKey = groupKey;
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndScrollView();
            });
            EditorGUILayout.EndVertical();
        }

        private void DrawContent()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            scrollPosRight = EditorGUILayout.BeginScrollView(scrollPosRight);

            if (string.IsNullOrEmpty(selectedGroupKey) || !poolManager.GroupPrefabLookup.ContainsKey(selectedGroupKey))
            {
                GUILayout.Label("Select a Group to view details", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                DrawGroupHeader(selectedGroupKey);
                
                // --- SORTING LOGIC ---
                var dict = poolManager.GroupPrefabLookup[selectedGroupKey];
                List<KeyValuePair<Object, PoolRuntimeInfo>> sortedList = dict.ToList();

                switch (currentSort)
                {
                    case SortOption.Name:
                        sortedList.Sort((a, b) => a.Key.name.CompareTo(b.Key.name));
                        break;
                    case SortOption.MostActive: // Sắp xếp theo số lượng đang Active
                        sortedList.Sort((a, b) => b.Value.ActiveCount.CompareTo(a.Value.ActiveCount));
                        break;
                    case SortOption.HighMemory: // Sắp xếp theo RAM
                        sortedList.Sort((a, b) => (b.Value.EstimatedMemory * b.Value.TotalCount).CompareTo(a.Value.EstimatedMemory * a.Value.TotalCount));
                        break;
                }

                foreach (var entry in sortedList)
                {
                    DrawPoolCard(entry.Key, entry.Value, selectedGroupKey);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawGroupHeader(string group)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"GROUP: {group.ToUpper()}", headerStyle);
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Despawn All Active", EditorStyles.toolbarButton)) poolManager.DespawnAllInGroup(group);
            if(GUILayout.Button("Destroy Group", EditorStyles.toolbarButton)) poolManager.DestroyAllInGroup(group);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawPoolCard(Object prefab, PoolRuntimeInfo info, string groupName)
        {
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.BeginHorizontal();

            // 1. THUMBNAIL (Ẩn nếu Compact Mode)
            if (!compactMode)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(prefab);
                if (preview == null) preview = AssetPreview.GetMiniThumbnail(prefab);
                GUILayout.Box(preview, GUILayout.Width(50), GUILayout.Height(50));
            }

            // 2. INFO SECTION
            EditorGUILayout.BeginVertical();
            
            // --- TITLE LINE & TYPE BADGE ---
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(prefab.name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            // -> Đây là phần hiển thị Note Type bạn yêu cầu <-
            if (info.IsComponent)
            {
                GUILayout.Label($"[SCRIPT: {info.ObjectType}]", badgeStyleComp);
            }
            else
            {
                GUILayout.Label("[GAME OBJECT]", badgeStyleGO);
            }
            
            if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(40))) EditorGUIUtility.PingObject(prefab);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);
            
            // Progress Bar
            DrawProgressBar(info.ActiveCount, info.TotalCount, info.PeakActiveCount);
            
            // Memory Stat
            long totalMem = info.EstimatedMemory * info.TotalCount;
            GUIStyle memStyle = EditorStyles.miniLabel;
            if (totalMem > 5 * 1024 * 1024) memStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.red } }; // Warn if > 5MB
            
            GUILayout.Label($"RAM Est: {EditorUtility.FormatBytes(totalMem)} | Peak: {info.PeakActiveCount}", memStyle);
            
            EditorGUILayout.EndVertical();

            // 3. ACTIONS
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical(GUILayout.Width(90));
            
            GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
            if (GUILayout.Button("Spawn Test", EditorStyles.miniButton)) SpawnTest(groupName, prefab);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+5", EditorStyles.miniButtonLeft)) poolManager.ExpandPool(groupName, prefab, 5);
            if (GUILayout.Button("Trim", EditorStyles.miniButtonRight)) poolManager.TrimPool(groupName, prefab);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawProgressBar(int active, int total, int peak)
        {
            Rect r = EditorGUILayout.GetControlRect(false, 16);
            EditorGUI.DrawRect(r, new Color(0.1f, 0.1f, 0.1f));
            
            float fillRatio = total > 0 ? (float)active / total : 0;
            Rect rFill = new Rect(r.x, r.y, r.width * fillRatio, r.height);
            
            Color barColor = new Color(0.2f, 0.8f, 0.2f); // Green
            if (fillRatio > 0.8f) barColor = new Color(1f, 0.3f, 0.3f); // Red alert
            else if (fillRatio > 0.5f) barColor = new Color(1f, 0.6f, 0.1f); // Orange warning

            EditorGUI.DrawRect(rFill, barColor);

            string label = $"Active: {active}/{total}";
            EditorGUI.LabelField(r, label, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
        }

        private void SpawnTest(string group, Object prefab)
        {
            Camera cam = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera : Camera.main;
            Vector3 pos = cam ? cam.transform.position + cam.transform.forward * 5 : Vector3.zero;
            poolManager.Spawn(group, prefab, null, pos, Quaternion.identity);
        }

        private void DrawBox(Color color, System.Action content)
        {
            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(r, color);
            content();
            EditorGUILayout.EndVertical();
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Pool Manager - Runtime Monitor", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 18 });
            GUILayout.Label("Enter Play Mode to view data.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }
    }
}
#endif