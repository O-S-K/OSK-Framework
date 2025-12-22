#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Collections.Generic;

namespace OSK
{
    public class FilterTreeView : TreeView
    {
        public FilterTreeView(TreeViewState state) : base(state) { Reload(); }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            root.children = new List<TreeViewItem>();
            int id = 1;

            // Cấu trúc mới: Class (Depth 0) -> Method (Depth 1)
            foreach (var classEntry in DebugFilterData.TreeData)
            {
                var classItem = new TreeViewItem { id = id++, depth = 0, displayName = classEntry.Key };
                root.AddChild(classItem);
                
                foreach (var method in classEntry.Value)
                {
                    var methodItem = new TreeViewItem { id = id++, depth = 1, displayName = method.Key };
                    classItem.AddChild(methodItem);
                }
            }

            if (!root.hasChildren) 
                root.AddChild(new TreeViewItem { id = -1, depth = 0, displayName = "No Logs Found" });

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item.id <= 0) return;
            float indent = GetContentIndent(args.item);
            Rect r = args.rowRect;

            // 1. Checkbox (Tích vào Class hoặc Method)
            Rect toggleRect = new Rect(r.x + indent, r.y, 16, r.height);
            EditorGUI.BeginChangeCheck();
            bool newState = GUI.Toggle(toggleRect, GetState(args.item), "");
            if (EditorGUI.EndChangeCheck()) SetState(args.item, newState);

            // 2. Stats & Buttons (Căn lề phải)
            float rightX = r.xMax - 5f;
            
            // Nút Thùng rác ngoài cùng
            if (DrawSmallButton(ref rightX, r.y, "TreeEditor.Trash", "Clear")) ClearItemLogs(args.item);

            // Số liệu thống kê Info/Warning/Error
            var agg = GetAggregatedStats(args.item);
            if (agg.e > 0) DrawStatItem(ref rightX, r.y, agg.e, "console.erroricon.sml");
            if (agg.w > 0) DrawStatItem(ref rightX, r.y, agg.w, "console.warnicon.sml");
            if (agg.l > 0) DrawStatItem(ref rightX, r.y, agg.l, "console.infoicon.sml");

            // 3. Label & Icon
            float labelX = r.x + indent + 20f;
            float labelWidth = rightX - labelX - 5f;
            
            // Icon Class (Script) hoặc Method (NextKey)
            string iconName = args.item.depth == 0 ? "cs Script Icon" : "Animation.NextKey";
            GUI.Label(new Rect(labelX, r.y, 16, r.height), EditorGUIUtility.IconContent(iconName));

            // 1. Xác định màu sắc dựa trên Stats
            Color rowColor = Color.white; // Mặc định Trắng
            if (agg.e > 0) rowColor = new Color(1f, 0.4f, 0.4f); // Đỏ nhẹ cho dễ nhìn
            else if (agg.w > 0) rowColor = new Color(1f, 0.9f, 0f); // Vàng

            // 2. Tạo style có RichText và đổi màu chữ
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.richText = true;
            labelStyle.normal.textColor = rowColor; // Đổi màu font toàn dòng
            if (args.item.depth < 1) labelStyle.fontStyle = FontStyle.Bold; // Class/Folder cho đậm lên

            string displayName = args.item.displayName;
    
            if (args.item.depth == 1) // Cấp Method
            {
                var stats = GetStats(args.item);
                if (stats != null && stats.Entries.Count > 0)
                    displayName = $"{args.item.displayName} -> {stats.Entries[^1].Message}";
            }

            Rect textRect = new Rect(labelX + 18, r.y, rightX - labelX - 25, r.height);
            GUI.Label(textRect, new GUIContent(displayName, displayName), labelStyle);
        }  
        private void DrawStatItem(ref float rightX, float y, int count, string iconName)
        {
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            string txt = count.ToString();
            float txtW = style.CalcSize(new GUIContent(txt)).x;
            
            rightX -= (txtW + 20f); // Tổng độ rộng cụm
            GUI.Label(new Rect(rightX, y, txtW, 20), txt, style);
            GUI.Label(new Rect(rightX + txtW + 2, y, 16, 20), EditorGUIUtility.IconContent(iconName));
            rightX -= 4f; 
        }

        private bool DrawSmallButton(ref float rightX, float y, string icon, string tip)
        {
            rightX -= 22f;
            return GUI.Button(new Rect(rightX, y + 2, 20, 16), new GUIContent(EditorGUIUtility.IconContent(icon).image, tip), EditorStyles.miniButton);
        }

        private void SetState(TreeViewItem item, bool state)
        {
            if (item.depth == 0) DebugFilterData.ClassStates[item.displayName] = state;
            else if (item.depth == 1) // Cấp Method
            {
                var s = GetStats(item);
                if (s != null) s.Enabled = state;
            }
            LoggerEditor.Instance.RefreshUnityConsole();
            Reload();
        }

        private void ClearItemLogs(TreeViewItem item)
        {
            if (item.depth == 0) DebugFilterData.ClearClass(item.displayName);
            else if (item.depth == 1) DebugFilterData.ClearMethod(item.parent.displayName, item.displayName);
            LoggerEditor.Instance.RefreshUnityConsole();
            Reload();
        }

        private LogStats GetStats(TreeViewItem item)
        {
            if (item == null) return null;
            string cls = item.depth == 0 ? item.displayName : item.parent.displayName;
            string meth = item.depth == 1 ? item.displayName : "";
            
            if (DebugFilterData.TreeData.TryGetValue(cls, out var methods))
                if (methods.TryGetValue(meth, out var stats)) return stats;
            return null;
        }

        private (int l, int w, int e) GetAggregatedStats(TreeViewItem item)
        {
            if (item.depth == 1) // Cấp Method
            {
                var s = GetStats(item);
                return s != null ? (s.LogCount, s.WarningCount, s.ErrorCount) : (0, 0, 0);
            }

            int tl = 0, tw = 0, te = 0;
            if (item.hasChildren)
                foreach (var c in item.children)
                {
                    var res = GetAggregatedStats(c);
                    tl += res.l; tw += res.w; te += res.e;
                }
            return (tl, tw, te);
        }

        private bool GetState(TreeViewItem item) => item.depth == 0 ? 
            DebugFilterData.ClassStates.GetValueOrDefault(item.displayName, true) : (GetStats(item)?.Enabled ?? true);
    }
}
#endif