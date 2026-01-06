using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace OSK
{
    public enum PreloadMode
    {
        Immediate,      // Load ngay lập tức (Mặc định)
        Spread,         // Load rải rác mỗi frame (Đỡ lag khi start game)
        Lazy            // Khi nào dùng mới load
    }
    
    public enum LimitMode
    {
        RecycleOldest,  // Đủ số lượng -> Xóa cái cũ nhất đi dùng lại (Tối ưu cho Effect/Bullet)
        RejectNew       // Đủ số lượng -> Không cho sinh thêm (Dùng cho Item giới hạn)
    }

    [System.Serializable]
    public class PoolItemData
    {
        [HorizontalGroup("Split", Width = 0.3f)]
        public string GroupName { get; set; }
        
        public string Key;
        public Object Prefab;
        public Transform Parent;
        [Tooltip("Số lượng object khởi tạo ban đầu.")]
        public int Size = 10;
        [Tooltip("Giới hạn tối đa. -1 là vô hạn (tự expand).")]
        public int MaxSize = -1; 
    
        [EnumToggleButtons]
        public PreloadMode LoadMode = PreloadMode.Immediate;
        
        [EnumToggleButtons]
        public LimitMode LimitMode = LimitMode.RecycleOldest; 
    }

    [System.Serializable]
    public class PoolGroupData
    {
        [TitleGroup("$GroupName")]
        public string GroupName;

        [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = false)]
        public List<PoolItemData> Pools = new();
    }
}